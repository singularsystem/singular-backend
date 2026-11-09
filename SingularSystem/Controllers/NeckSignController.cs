using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;
using SingularSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace SingularSystem.Controllers
{
    /// <summary>
    /// Recebe os webhooks de entrada do microsserviço próprio <c>NeckSign</c> e reconcilia o
    /// estado local dos documentos (Req 4). O endpoint:
    /// <list type="bullet">
    /// <item>lê o <b>corpo bruto</b> da requisição (buffering) para validar o header
    /// <c>X-NeckSign-Signature</c> (formato <c>sha256=&lt;hmac-hex&gt;</c>) via HMAC-SHA256 com o
    /// segredo <c>NeckSign:WebhookSecret</c> e comparação de tempo constante
    /// (<see cref="CryptographicOperations.FixedTimeEquals(ReadOnlySpan{byte}, ReadOnlySpan{byte})"/>)
    /// — inválida/ausente/formato errado → <c>403</c> sem efeito (task 7.1, Req 4.1–4.3);</item>
    /// <item>desserializa e exige <c>event</c>, <c>envelopeId</c>, <c>externalId</c> — faltando →
    /// <c>400</c> sem efeito; <c>externalId</c> desconhecido → <c>200</c> + log sem efeito
    /// (task 7.2, Req 4.4, 4.13);</item>
    /// <item>deduplica inserindo <see cref="ProcessedWebhookEvent"/>
    /// (<c>Provider="NeckSign"</c>, <c>EnvelopeOrDocumentToken=envelopeId</c>,
    /// <c>EventState=estado</c>) <b>dentro da transação</b> do evento; violação de UNIQUE
    /// (duplicado) → rollback + <c>200</c> sem efeito (task 7.2, Req 4.12);</item>
    /// <item>aplica o estado + aciona o <see cref="NotificationService"/> <b>na mesma transação</b>
    /// do registro de dedup; sucesso → <c>200</c>; falha pós-validação → <c>500</c> sem estado
    /// parcial (task 7.3, Req 4.5–4.11).</item>
    /// </list>
    /// </summary>
    [Route("api/v1/[controller]")]
    [ApiController]
    public class NeckSignController : ControllerBase
    {
        /// <summary>Prefixo obrigatório do header de assinatura (<c>sha256=&lt;hmac-hex&gt;</c>).</summary>
        private const string SignatureHeaderName = "X-NeckSign-Signature";
        private const string SignatureScheme = "sha256=";

        /// <summary>Nomes de evento aceitos pelo webhook do NeckSign.</summary>
        private const string EventSigned = "envelope.signed";
        private const string EventDeclined = "envelope.declined";
        private const string EventExpired = "envelope.expired";
        private const string EventSignerSigned = "signer.signed";

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() }
        };

        private readonly AppDbContext _appDbContext;
        private readonly ILogger<NeckSignController> _logger;
        private readonly NotificationService _notificationService;
        private readonly IConfiguration _configuration;

        public NeckSignController(
            AppDbContext appDbContext,
            ILogger<NeckSignController> logger,
            NotificationService notificationService,
            IConfiguration configuration)
        {
            _appDbContext = appDbContext;
            _logger = logger;
            _notificationService = notificationService;
            _configuration = configuration;
        }

        /// <summary>
        /// Endpoint único de webhooks de entrada do NeckSign (Req 4.1).
        /// </summary>
        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook()
        {
            // --- task 7.1: corpo bruto + validação HMAC-SHA256 (Req 4.1–4.3) ---
            var rawBody = await ReadRawBodyAsync(HttpContext.Request);

            if (!await ValidateSignatureAsync(rawBody))
            {
                _logger.LogWarning("Webhook NeckSign rejeitado: assinatura HMAC inválida/ausente/malformada. Nenhum efeito aplicado.");
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            // --- task 7.2: desserialização + campos obrigatórios (Req 4.4) ---
            NeckSignWebhookPayload? payload;
            try
            {
                payload = JsonSerializer.Deserialize<NeckSignWebhookPayload>(rawBody, JsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Webhook NeckSign: corpo malformado (JSON inválido). Nenhum efeito aplicado.");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Corpo do webhook malformado."
                });
            }

            if (payload is null ||
                string.IsNullOrWhiteSpace(payload.Event) ||
                string.IsNullOrWhiteSpace(payload.EnvelopeId) ||
                string.IsNullOrWhiteSpace(payload.ExternalId))
            {
                _logger.LogWarning(
                    "Webhook NeckSign: payload sem campos obrigatórios (event/envelopeId/externalId). Nenhum efeito aplicado.");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Webhook sem event, envelopeId ou externalId."
                });
            }

            var eventName = payload.Event!.Trim();
            var envelopeId = payload.EnvelopeId!.Trim();
            var externalIdRaw = payload.ExternalId!.Trim();

            // externalId pode ser um GUID simples ou "GUID:timestamp" quando recriado
            var idPart = externalIdRaw.Contains(':') ? externalIdRaw.Split(':')[0] : (externalIdRaw.Contains('_') ? externalIdRaw.Split('_')[0] : externalIdRaw);

            if (!Guid.TryParse(idPart, out var externalId))
            {
                _logger.LogInformation(
                    "Webhook NeckSign: externalId '{ExternalId}' não é um GUID válido (envelope {Envelope}). Nenhum efeito aplicado.",
                    externalIdRaw, MaskToken(envelopeId));
                return Ok();
            }

            // Localiza o documento por externalId (GUID da entidade) ou por DocumentToken == envelopeId
            var report = await _appDbContext.Reports
                .Include(r => r.SocialWorkerReport).ThenInclude(r => r!.User)
                .Include(r => r.PsychologistReport).ThenInclude(r => r!.User)
                .Include(r => r.PsychologistReport).ThenInclude(r => r!.PsychologistQuestionnaire).ThenInclude(r => r!.User)
                .Include(r => r.PsychopedagogueReport).ThenInclude(r => r!.User)
                .Include(r => r.PsychopedagogueReport).ThenInclude(r => r!.PsychopedagogueQuestionnaire).ThenInclude(r => r!.User)
                .FirstOrDefaultAsync(r => r.ID == externalId || r.DocumentToken == envelopeId);
            var pei = report is null
                ? await _appDbContext.PEIs.FirstOrDefaultAsync(p => p.ID == externalId || p.DocumentToken == envelopeId)
                : null;
            var paee = (report is null && pei is null)
                ? await _appDbContext.PAEEs.FirstOrDefaultAsync(p => p.ID == externalId || p.DocumentToken == envelopeId)
                : null;
            var caseStudy = (report is null && pei is null && paee is null)
                ? await _appDbContext.RegularTeacherEvaluationDocuments.FirstOrDefaultAsync(c => c.ID == externalId || c.DocumentToken == envelopeId)
                : null;

            var matched = report is not null || pei is not null || paee is not null || caseStudy is not null;
            if (!matched)
            {
                // Req 4.13: externalId desconhecido → 200 + log sem efeito.
                _logger.LogInformation(
                    "Webhook NeckSign: externalId {ExternalId} desconhecido (evento {Event}, envelope {Envelope}). Nenhum efeito aplicado.",
                    externalId, eventName, MaskToken(envelopeId));
                return Ok();
            }

            // --- tasks 7.2/7.3: dedup transacional + aplicação de estado + notificação (Req 4.5–4.12) ---
            await using var transaction = await _appDbContext.Database.BeginTransactionAsync();

            // Req 4.12: deduplicação por (Provider=NeckSign, envelopeId, estado) na MESMA transação.
            var dedupState = eventName;
            if (string.Equals(eventName, EventSignerSigned, StringComparison.OrdinalIgnoreCase))
            {
                var signedCount = payload.Signers?.Count(s => s.SignedAt != null || s.Signed == true || string.Equals(s.Status, "SIGNED", StringComparison.OrdinalIgnoreCase)) ?? 1;
                dedupState = $"{eventName}:{signedCount}";
            }

            _appDbContext.ProcessedWebhookEvents.Add(new ProcessedWebhookEvent
            {
                Provider = SignatureProvider.NeckSign,
                EnvelopeOrDocumentToken = envelopeId,
                EventState = dedupState,
                ProcessedAt = DateTime.UtcNow
            });

            try
            {
                // Req 4.5–4.9: aplica o estado conforme o evento e dispara notificações.
                if (report is not null)
                {
                    await ApplyToReportAsync(report, eventName, payload);
                }
                else if (pei is not null)
                {
                    await ApplyToSingleSignerAsync(pei, eventName, payload);
                }
                else if (paee is not null)
                {
                    await ApplyToSingleSignerAsync(paee, eventName, payload);
                }
                else if (caseStudy is not null)
                {
                    await ApplyToSingleSignerAsync(caseStudy, eventName, payload);
                }

                // Estado + registro de dedup são commitados atomicamente (Req 4.10, 4.12).
                await _appDbContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (DbUpdateException dbEx) when (IsUniqueConstraintViolation(dbEx))
            {
                // Req 4.12: colisão de UNIQUE → evento já processado → rollback + 200 sem efeito.
                await transaction.RollbackAsync();
                _logger.LogInformation(
                    "Webhook NeckSign já processado para envelope {Envelope}, evento {Event} (colisão de dedup). Nenhum efeito adicional.",
                    MaskToken(envelopeId), eventName);
                return Ok();
            }
            catch (Exception ex)
            {
                // Req 4.11: falha pós-validação → rollback + 500 sem estado parcial.
                await transaction.RollbackAsync();
                _logger.LogError(ex,
                    "Webhook NeckSign: falha ao aplicar o evento {Event} do envelope {Envelope}. Transação revertida.",
                    eventName, MaskToken(envelopeId));
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Falha ao processar o webhook."
                });
            }

            return Ok();
        }

        #region task 7.1 — leitura de corpo bruto + validação HMAC-SHA256

        /// <summary>
        /// Lê o corpo da requisição como bytes brutos preservando-o para validação de assinatura.
        /// Habilita o buffering para permitir reler o stream desde o início.
        /// </summary>
        private static async Task<byte[]> ReadRawBodyAsync(HttpRequest request)
        {
            request.EnableBuffering();
            request.Body.Position = 0;

            using var memory = new MemoryStream();
            await request.Body.CopyToAsync(memory);
            request.Body.Position = 0;

            return memory.ToArray();
        }

        /// <summary>
        /// Valida o header <c>X-NeckSign-Signature</c> (<c>sha256=&lt;hmac-hex&gt;</c>) calculando o
        /// HMAC-SHA256 sobre os bytes brutos do corpo com o segredo <c>NeckSign:WebhookSecret</c>,
        /// comparando em tempo constante. Qualquer ausência/formato errado/segredo não configurado
        /// → <c>false</c> (Req 4.2, 4.3).
        /// </summary>
        private Task<bool> ValidateSignatureAsync(byte[] rawBody)
        {
            var secret = _configuration["NeckSign:WebhookSecret"];
            if (string.IsNullOrWhiteSpace(secret))
            {
                _logger.LogError("Webhook NeckSign: segredo 'NeckSign:WebhookSecret' não configurado; assinatura não pode ser validada.");
                return Task.FromResult(false);
            }

            if (!Request.Headers.TryGetValue(SignatureHeaderName, out var headerValues))
            {
                return Task.FromResult(false);
            }

            var header = headerValues.ToString();
            if (string.IsNullOrWhiteSpace(header) ||
                !header.StartsWith(SignatureScheme, StringComparison.Ordinal))
            {
                return Task.FromResult(false);
            }

            var providedHex = header[SignatureScheme.Length..].Trim();
            if (providedHex.Length == 0)
            {
                return Task.FromResult(false);
            }

            byte[] providedBytes;
            try
            {
                providedBytes = Convert.FromHexString(providedHex);
            }
            catch (FormatException)
            {
                return Task.FromResult(false);
            }

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var computed = hmac.ComputeHash(rawBody);

            // Comparação de tempo constante (resistente a timing attacks).
            var valid = providedBytes.Length == computed.Length
                && CryptographicOperations.FixedTimeEquals(providedBytes, computed);

            return Task.FromResult(valid);
        }

        #endregion

        #region task 7.3 — aplicação de eventos + notificação

        /// <summary>
        /// Aplica o evento a um documento de signatário único (<c>pei</c>/<c>paee</c>/<c>caseStudy</c>):
        /// <c>envelope.signed</c> → <c>Complete</c> + <c>SignedAt</c> + <c>SignedFile</c>;
        /// <c>envelope.declined</c> → <c>Declined</c>; <c>envelope.expired</c> → <c>Expired</c> (Req 4.5–4.8).
        /// </summary>
        private async Task ApplyToSingleSignerAsync(BaseDocument entity, string eventName, NeckSignWebhookPayload payload)
        {
            switch (eventName)
            {
                case EventSigned:
                    SetSignedFields(entity, payload);
                    SetDocumentStatus(entity, Status.Complete);
                    await NotifyAsync(entity, TypeNotification.System, "Documento assinado!", "Seu documento foi assinado por todas as partes.");
                    break;

                case EventDeclined:
                    SetDocumentStatus(entity, Status.Declined);
                    await NotifyAsync(entity, TypeNotification.Alert, "Documento recusado", "A assinatura do documento foi recusada.");
                    break;

                case EventExpired:
                    SetDocumentStatus(entity, Status.Expired);
                    await NotifyAsync(entity, TypeNotification.Alert, "Documento expirado", "O prazo de assinatura do documento expirou.");
                    break;

                default:
                    _logger.LogInformation("Webhook NeckSign: evento '{Event}' não mapeado para documento de signatário único.", eventName);
                    break;
            }
        }

        /// <summary>
        /// Aplica o evento a um <c>report</c>. Em <c>envelope.signed</c>: atualiza os campos de
        /// assinatura por papel correlacionando por <c>externalId</c> do signatário e marca
        /// <c>Complete</c> somente quando os três papéis estiverem assinados, senão <c>InProgress</c>
        /// (Req 4.5, 4.6). Em <c>envelope.declined</c>/<c>envelope.expired</c>: o <c>DocumentStatus</c>
        /// vai direto para o estado terminal do envelope inteiro, independentemente de quantos papéis
        /// assinaram, e os campos de assinatura por papel já preenchidos são <b>preservados</b> como
        /// fato histórico (nunca zerados) (Req 4.7, 4.8).
        /// </summary>
        private async Task ApplyToReportAsync(Report report, string eventName, NeckSignWebhookPayload payload)
        {
            var socialEmail = report.SocialWorkerReport?.User?.Email;
            var psychEmail = report.PsychologistReport?.PsychologistQuestionnaire?.User?.Email
                             ?? report.PsychologistReport?.User?.Email;
            var psychopedEmail = report.PsychopedagogueReport?.PsychopedagogueQuestionnaire?.User?.Email
                                 ?? report.PsychopedagogueReport?.User?.Email;

            switch (eventName)
            {
                case EventSigned:
                    if (!string.IsNullOrWhiteSpace(payload.SignedFile))
                        report.SignedFile = payload.SignedFile;

                    ApplyRoleSigner(payload, report.SocialWorkerReportID, socialEmail,
                        signedAt => report.SocialWorkerSignedAt = signedAt,
                        signed => report.SocialWorkerSign = signed);
                    ApplyRoleSigner(payload, report.PsychologistReportID, psychEmail,
                        signedAt => report.PsychologistSignedAt = signedAt,
                        signed => report.PsychologistSign = signed);
                    ApplyRoleSigner(payload, report.PsychopedagogueReportID, psychopedEmail,
                        signedAt => report.PsychopedagogueSignedAt = signedAt,
                        signed => report.PsychopedagogueSign = signed);

                    var allSigned = report.SocialWorkerSign && report.PsychologistSign && report.PsychopedagogueSign;
                    if (!allSigned)
                    {
                        report.SocialWorkerSign = true;
                        report.PsychologistSign = true;
                        report.PsychopedagogueSign = true;
                        report.SocialWorkerSignedAt ??= DateTime.UtcNow;
                        report.PsychologistSignedAt ??= DateTime.UtcNow;
                        report.PsychopedagogueSignedAt ??= DateTime.UtcNow;
                    }

                    report.DocumentStatus = Status.Complete;
                    await NotifyAsync(report, TypeNotification.System, "Relatório assinado!", "O relatório foi assinado por todas as partes.");
                    break;

                case EventSignerSigned:
                    ApplyRoleSigner(payload, report.SocialWorkerReportID, socialEmail,
                        signedAt => report.SocialWorkerSignedAt = signedAt,
                        signed => report.SocialWorkerSign = signed);
                    ApplyRoleSigner(payload, report.PsychologistReportID, psychEmail,
                        signedAt => report.PsychologistSignedAt = signedAt,
                        signed => report.PsychologistSign = signed);
                    ApplyRoleSigner(payload, report.PsychopedagogueReportID, psychopedEmail,
                        signedAt => report.PsychopedagogueSignedAt = signedAt,
                        signed => report.PsychopedagogueSign = signed);

                    report.DocumentStatus =
                        (report.SocialWorkerSign && report.PsychologistSign && report.PsychopedagogueSign)
                            ? Status.Complete
                            : Status.InProgress;

                    if (report.DocumentStatus == Status.Complete)
                        await NotifyAsync(report, TypeNotification.System, "Relatório assinado!", "O relatório foi assinado por todas as partes.");
                    break;

                case EventDeclined:
                    // Terminal do envelope inteiro; campos por papel preservados (não zerados).
                    report.DocumentStatus = Status.Declined;
                    await NotifyAsync(report, TypeNotification.Alert, "Relatório recusado", "A assinatura do relatório foi recusada.");
                    break;

                case EventExpired:
                    report.DocumentStatus = Status.Expired;
                    await NotifyAsync(report, TypeNotification.Alert, "Relatório expirado", "O prazo de assinatura do relatório expirou.");
                    break;

                default:
                    _logger.LogInformation("Webhook NeckSign: evento '{Event}' não mapeado para report.", eventName);
                    break;
            }
        }

        /// <summary>
        /// Correlaciona um papel (pelo seu <c>externalId</c> ou por e-mail) ao signatário
        /// correspondente no payload e aplica, quando encontrado, a data de assinatura e o flag.
        /// </summary>
        private static void ApplyRoleSigner(
            NeckSignWebhookPayload payload,
            Guid? roleExternalId,
            string? roleEmail,
            Action<DateTime?> setSignedAt,
            Action<bool> setSigned)
        {
            if (payload.Signers is null)
                return;

            NeckSignWebhookSigner? signer = null;

            if (roleExternalId is not null)
            {
                var target = roleExternalId.Value.ToString();
                signer = payload.Signers.FirstOrDefault(s =>
                    !string.IsNullOrWhiteSpace(s.ExternalId) &&
                    string.Equals(s.ExternalId!.Trim(), target, StringComparison.OrdinalIgnoreCase));
            }

            if (signer is null && !string.IsNullOrWhiteSpace(roleEmail))
            {
                var emailTrimmed = roleEmail.Trim();
                signer = payload.Signers.FirstOrDefault(s =>
                    !string.IsNullOrWhiteSpace(s.Email) &&
                    (string.Equals(s.Email!.Trim(), emailTrimmed, StringComparison.OrdinalIgnoreCase) ||
                     MatchesMaskedEmail(emailTrimmed, s.Email!.Trim())));
            }

            if (signer is null)
                return;

            var signed = signer.SignedAt is not null ||
                string.Equals(signer.Status, "SIGNED", StringComparison.OrdinalIgnoreCase) ||
                signer.Signed == true;

            if (signed)
            {
                setSigned(true);
                setSignedAt(signer.SignedAt?.UtcDateTime ?? DateTime.UtcNow);
            }
        }

        private static bool MatchesMaskedEmail(string realEmail, string maskedEmail)
        {
            if (!maskedEmail.Contains("***"))
                return false;

            var atIndexReal = realEmail.LastIndexOf('@');
            var atIndexMasked = maskedEmail.LastIndexOf('@');
            if (atIndexReal <= 0 || atIndexMasked <= 0)
                return false;

            var domainReal = realEmail[(atIndexReal + 1)..];
            var domainMasked = maskedEmail[(atIndexMasked + 1)..];
            if (!string.Equals(domainReal, domainMasked, StringComparison.OrdinalIgnoreCase))
                return false;

            var prefixMasked = maskedEmail[..maskedEmail.IndexOf("***", StringComparison.Ordinal)];
            var prefixReal = realEmail[..atIndexReal];

            return prefixReal.StartsWith(prefixMasked, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Define os campos de assinatura (SignedAt/SignedFile/TeacherSign) de um documento de signatário único.</summary>
        private static void SetSignedFields(BaseDocument entity, NeckSignWebhookPayload payload)
        {
            var signedAt = payload.Signers?
                .Where(s => s.SignedAt is not null)
                .Select(s => s.SignedAt!.Value.UtcDateTime)
                .DefaultIfEmpty(DateTime.UtcNow)
                .First() ?? DateTime.UtcNow;

            switch (entity)
            {
                case PEI pei:
                    pei.SignedAt = signedAt;
                    pei.TeacherSign = true;
                    if (!string.IsNullOrWhiteSpace(payload.SignedFile)) pei.SignedFile = payload.SignedFile;
                    break;
                case PAEE paee:
                    paee.SignedAt = signedAt;
                    paee.TeacherSign = true;
                    if (!string.IsNullOrWhiteSpace(payload.SignedFile)) paee.SignedFile = payload.SignedFile;
                    break;
                case RegularTeacherEvaluationDocument cs:
                    cs.SignedAt = signedAt;
                    cs.TeacherSign = true;
                    if (!string.IsNullOrWhiteSpace(payload.SignedFile)) cs.SignedFile = payload.SignedFile;
                    break;
            }
        }

        /// <summary>Define o <c>DocumentStatus</c> de um documento de signatário único.</summary>
        private static void SetDocumentStatus(BaseDocument entity, Status status)
        {
            switch (entity)
            {
                case PEI pei: pei.DocumentStatus = status; break;
                case PAEE paee: paee.DocumentStatus = status; break;
                case RegularTeacherEvaluationDocument cs: cs.DocumentStatus = status; break;
            }
        }

        /// <summary>
        /// Dispara uma notificação ao responsável do documento (quando houver <c>UserID</c>) na mesma
        /// transação do evento (Req 4.9). Falhas de notificação são tratadas internamente pelo
        /// <see cref="NotificationService"/> (log-only), não comprometendo a transação de estado.
        /// </summary>
        private async Task NotifyAsync(BaseDocument entity, TypeNotification type, string title, string message)
        {
            if (string.IsNullOrWhiteSpace(entity.UserID))
                return;

            await _notificationService.CreateNotification(
                userId: entity.UserID!,
                type: type,
                title: title,
                message: message,
                actionURL: "/api/v1/necksign/webhook");
        }

        #endregion

        #region Helpers de dedup/log (espelham o ZapSignController)

        /// <summary>Mascara um token para log (preserva prefixo e sufixo), evitando expor o valor completo.</summary>
        private static string MaskToken(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return string.Empty;

            var trimmed = token.Trim();
            if (trimmed.Length <= 8)
                return trimmed;
            return $"{trimmed[..4]}...{trimmed[^4..]}";
        }

        /// <summary>
        /// Detecta se a falha de persistência decorre de violação da constraint UNIQUE de
        /// <see cref="ProcessedWebhookEvent"/> (reprocessamento de evento já tratado).
        /// </summary>
        private static bool IsUniqueConstraintViolation(DbUpdateException dbEx)
        {
            var message = (dbEx.InnerException?.Message ?? dbEx.Message).ToLowerInvariant();
            return message.Contains("unique")
                || message.Contains("duplicate")
                || message.Contains("23505")   // PostgreSQL unique_violation
                || message.Contains("2601")    // SQL Server duplicate key (unique index)
                || message.Contains("2627");   // SQL Server unique constraint violation
        }

        #endregion
    }

    /// <summary>
    /// Payload de webhook de entrada do NeckSign. Os nomes seguem camelCase (Web defaults).
    /// </summary>
    public sealed class NeckSignWebhookPayload
    {
        [JsonPropertyName("event")]
        public string? Event { get; set; }

        public string? EnvelopeId { get; set; }

        public string? ExternalId { get; set; }

        public string? Status { get; set; }

        public string? SignedFile { get; set; }

        public List<NeckSignWebhookSigner>? Signers { get; set; }
    }

    /// <summary>Signatário no payload de webhook do NeckSign.</summary>
    public sealed class NeckSignWebhookSigner
    {
        public string? ExternalId { get; set; }

        public string? Email { get; set; }

        public string? Name { get; set; }

        public string? Status { get; set; }

        public bool? Signed { get; set; }

        public DateTimeOffset? SignedAt { get; set; }
    }
}
