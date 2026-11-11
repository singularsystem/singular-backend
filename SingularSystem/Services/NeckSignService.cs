using SingularSystem.DTOs;
using SingularSystem.Interfaces;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SingularSystem.Services
{
    /// <summary>
    /// Implementação de <see cref="ISignatureService"/> que integra a Singular System ao microsserviço
    /// próprio <c>NeckSign</c> via API REST. Correlaciona documentos exclusivamente pelo
    /// <c>externalId</c> (GUID da entidade) e nunca expõe tipos específicos de provedor aos
    /// controllers chamadores.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Este arquivo é o <b>esqueleto</b> do serviço (task 4.1). A fiação de DI e o consumo do
    /// <see cref="HttpClient"/> nomeado <c>"NeckSign"</c> estão estabelecidos aqui; a lógica
    /// concreta de cada método é implementada em tasks posteriores
    /// (4.2/4.3 — criação/composição de signatários; 5.x — exclusão, detalhes e sincronização).
    /// </para>
    /// <para>
    /// O <see cref="HttpClient"/> é obtido via <see cref="IHttpClientFactory"/> usando o cliente
    /// nomeado <see cref="NeckSignHttpClientName"/>. A configuração definitiva do cliente nomeado
    /// (<c>AddHttpClient("NeckSign", ...)</c>) é registrada em <c>Program.cs</c> na task 9.4.
    /// Enquanto isso, o construtor aplica, de forma defensiva, <c>BaseAddress</c>, <c>Timeout</c>
    /// de 30s e o header padrão <c>X-Api-Key</c> a partir de <see cref="IConfiguration"/>
    /// (<c>NeckSign:BaseUrl</c>, <c>NeckSign:ApiKey</c>), evitando dependência rígida da classe
    /// de opções tipada (<c>IConfiguration</c>, criada na task 9.1).
    /// </para>
    /// </remarks>
    public class NeckSignService : ISignatureService
    {
        /// <summary>Nome do <see cref="HttpClient"/> nomeado consumido via <see cref="IHttpClientFactory"/>.</summary>
        public const string NeckSignHttpClientName = "NeckSign";

        /// <summary>Timeout padrão das chamadas ao NeckSign (Req 2.4).</summary>
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

        /// <summary>Caminho relativo (sob <c>BaseAddress</c>) do endpoint de criação de envelopes.</summary>
        private const string EnvelopesPath = "api/v1/envelopes";

        /// <summary>Opções de serialização JSON padronizadas (camelCase + enums como string) para o corpo do POST.</summary>
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() },
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly HttpClient _httpClient;
        private readonly IPDFService _pdfService;
        private readonly AppDbContext _appDbContext;
        private readonly ILogger<NeckSignService> _logger;

        /// <summary>
        /// Fábrica de escopos usada por <see cref="SyncPendingDocuments"/> para resolver uma
        /// instância <see cref="ISignatureService"/> (com seu próprio <see cref="AppDbContext"/>)
        /// por token sincronizado, espelhando o padrão de escopo do <c>ZapSignService</c> e evitando
        /// o compartilhamento concorrente do <see cref="DbContext"/> injetado neste serviço.
        /// </summary>
        private readonly IServiceScopeFactory _scopeFactory;

        /// <summary>
        /// URL de webhook da Singular System enviada ao NeckSign na criação do envelope (Req 2.2). Lida de
        /// <c>NeckSign:WebhookUrl</c>; o NeckSign a usa para notificar o <c>NeckSignController</c>
        /// (webhooks de entrada, task 7) sobre mudanças de estado do envelope.
        /// </summary>
        private readonly string? _webhookUrl;

        public NeckSignService(
            IHttpClientFactory httpClientFactory,
            IPDFService pdfService,
            AppDbContext appDbContext,
            ILogger<NeckSignService> logger,
            IConfiguration configuration,
            IServiceScopeFactory scopeFactory)
        {
            _httpClient = httpClientFactory.CreateClient(NeckSignHttpClientName);
            _pdfService = pdfService;
            _appDbContext = appDbContext;
            _logger = logger;
            _scopeFactory = scopeFactory;
            _webhookUrl = configuration["NeckSign:WebhookUrl"];

            // Configuração defensiva do cliente nomeado enquanto a task 9.4 (AddHttpClient)
            // ainda não registra o cliente "NeckSign" completamente. Lê valores diretamente de
            // IConfiguration para não acoplar a IConfiguration (task 9.1).
            ConfigureClientIfNeeded(configuration);
        }

        /// <summary>
        /// Aplica <c>BaseAddress</c>, <c>Timeout</c> e o header padrão <c>X-Api-Key</c> ao
        /// cliente obtido da factory caso ainda não tenham sido configurados pelo registro
        /// nomeado em <c>Program.cs</c> (task 9.4). É idempotente e silencioso: valores ausentes
        /// são apenas registrados em log, sem lançar exceção.
        /// </summary>
        private void ConfigureClientIfNeeded(IConfiguration configuration)
        {
            if (_httpClient.BaseAddress is null)
            {
                var baseUrl = configuration["NeckSign:BaseUrl"];
                if (!string.IsNullOrWhiteSpace(baseUrl) &&
                    Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
                {
                    _httpClient.BaseAddress = baseUri;
                }
                else
                {
                    _logger.LogWarning(
                        "NeckSign BaseUrl ausente ou inválida na configuração; o HttpClient nomeado '{ClientName}' não possui BaseAddress definido.",
                        NeckSignHttpClientName);
                }
            }

            if (_httpClient.Timeout != DefaultTimeout)
            {
                _httpClient.Timeout = DefaultTimeout;
            }

            if (!_httpClient.DefaultRequestHeaders.Contains("X-Api-Key"))
            {
                var apiKey = configuration["NeckSign:ApiKey"];
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    _httpClient.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
                }
                else
                {
                    _logger.LogWarning(
                        "NeckSign ApiKey ausente na configuração; o HttpClient nomeado '{ClientName}' não possui o header padrão X-Api-Key.",
                        NeckSignHttpClientName);
                }
            }
        }

        /// <inheritdoc />
        /// <remarks>
        /// Fluxo (task 4.3): valida <paramref name="type"/> → localiza a entidade por
        /// <paramref name="idDoc"/> <b>antes</b> de gerar o PDF → compõe/filtra signatários
        /// (<see cref="ComposeSignersAsync"/>) → gera o PDF via <see cref="IPDFService.GeneratePdf"/>
        /// → <c>POST /api/v1/envelopes</c> → persiste o estado <b>apenas</b> em um <c>201</c>
        /// completo. Qualquer falha (entidade ausente, lista de signatários vazia, PDF, resposta
        /// malformada/não-201/timeout) registra em log, retorna <c>false</c> e <b>não</b> altera o
        /// estado local (Req 2.1–2.14, 15.3).
        /// </remarks>
        public async Task<bool> CreateDoc(Guid idDoc, string type)
        {
            // Req 1.5/1.6: validação de tipo (case-sensitive); inválido → log + false, sem exceção.
            if (!DocumentTypeParser.TryParse(type, out var documentType))
            {
                _logger.LogInformation("Tipo de documento inválido em CreateDoc (NeckSign): '{Type}'", type);
                return false;
            }

            // Req 2.1/2.13: localiza a entidade ANTES de gerar o PDF. Entidade ausente → false, sem PDF.
            var entity = await LoadDocumentEntityAsync(documentType, idDoc);
            if (entity is null)
            {
                _logger.LogInformation(
                    "CreateDoc (NeckSign) abortado: entidade {Type} {IdDoc} não encontrada; PDF não será gerado.",
                    DocumentTypeParser.ToCanonicalString(documentType), idDoc);
                return false;
            }

            // Req 2.5/2.6/2.7: composição e filtragem de signatários. Lista vazia → false, sem POST.
            var signers = await ComposeSignersAsync(documentType, idDoc);
            if (signers.Count == 0)
            {
                _logger.LogError(
                    "CreateDoc (NeckSign) abortado: lista de signatários vazia para {Type} {IdDoc}; POST não será enviado.",
                    DocumentTypeParser.ToCanonicalString(documentType), idDoc);
                return false;
            }

            // Req 2.14: geração do PDF; falha → false, sem POST.
            byte[] pdfBytes;
            try
            {
                pdfBytes = await _pdfService.GeneratePdf(idDoc);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "CreateDoc (NeckSign) abortado: falha ao gerar PDF do {Type} {IdDoc}; POST não será enviado.",
                    DocumentTypeParser.ToCanonicalString(documentType), idDoc);
                return false;
            }

            if (pdfBytes is null || pdfBytes.Length == 0)
            {
                _logger.LogError(
                    "CreateDoc (NeckSign) abortado: PDF gerado vazio para {Type} {IdDoc}; POST não será enviado.",
                    DocumentTypeParser.ToCanonicalString(documentType), idDoc);
                return false;
            }

            var pdfBase64 = Convert.ToBase64String(pdfBytes);

            // Req 2.2: monta o corpo do POST com externalId (GUID), title, webhookUrl, signers e PDF Base64.
            var requestBody = new NeckSignCreateEnvelopeRequest
            {
                ExternalId = idDoc.ToString(),
                Title = BuildTitle(documentType, entity),
                WebhookUrl = _webhookUrl,
                Base64Pdf = pdfBase64,
                Signers = signers.Select(s => new NeckSignCreateEnvelopeSigner
                {
                    Name = s.Name,
                    Email = s.Email,
                    Role = s.Role,
                    Order = s.Order > 0 ? s.Order : null,
                    ExternalId = string.IsNullOrWhiteSpace(s.ExternalId) ? null : s.ExternalId
                }).ToList()
            };

            var json = JsonSerializer.Serialize(requestBody, JsonOptions);

            // Req 2.2/2.3/2.4: POST /api/v1/envelopes via cliente nomeado "NeckSign"
            // (X-Api-Key já no header padrão; timeout de 30s no HttpClient).
            HttpResponseMessage response;
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, EnvelopesPath)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                response = await _httpClient.SendAsync(request);
            }
            catch (TaskCanceledException ex)
            {
                // Req 2.12: timeout → log + false, sem alterar a entidade.
                _logger.LogError(ex,
                    "CreateDoc (NeckSign): timeout/cancelamento ao criar envelope para {Type} {IdDoc}; estado local preservado.",
                    DocumentTypeParser.ToCanonicalString(documentType), idDoc);
                return false;
            }
            catch (Exception ex)
            {
                // Req 2.12: erro de comunicação → log + false, sem alterar a entidade.
                _logger.LogError(ex,
                    "CreateDoc (NeckSign): falha de comunicação ao criar envelope para {Type} {IdDoc}; estado local preservado.",
                    DocumentTypeParser.ToCanonicalString(documentType), idDoc);
                return false;
            }

            string responseJson;

            // Se o envelope já existir no NeckSign (EXTERNAL_ID_ALREADY_EXISTS), tenta cancelar o envelope anterior
            // (que pode ter ficado ativo por falha prévia de DeleteDoc) e recriar. Se não for possível recriar, tenta recuperar.
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var errorBody = await SafeReadBodyAsync(response);
                if (errorBody.Contains("EXTERNAL_ID_ALREADY_EXISTS"))
                {
                    _logger.LogWarning(
                        "CreateDoc (NeckSign): envelope já existia no NeckSign para {Type} {IdDoc}. Tentando voidar envelope anterior e recriar.",
                        DocumentTypeParser.ToCanonicalString(documentType), idDoc);

                    var existingToken = GetDocumentToken(documentType, entity);
                    var retrySucceeded = false;

                    if (!string.IsNullOrWhiteSpace(existingToken))
                    {
                        try
                        {
                            var voidPayload = JsonSerializer.Serialize(
                                new { reason = "Cancelado para recriação de documento atualizado" }, JsonOptions);
                            using var voidRequest = new HttpRequestMessage(
                                HttpMethod.Post, $"{EnvelopesPath}/{Uri.EscapeDataString(existingToken)}/void")
                            {
                                Content = new StringContent(voidPayload, Encoding.UTF8, "application/json")
                            };
                            var voidResp = await _httpClient.SendAsync(voidRequest);
                            _logger.LogInformation(
                                "CreateDoc (NeckSign): void do envelope anterior {ExistingToken} retornou {StatusCode}.",
                                existingToken, (int)voidResp.StatusCode);

                            // Tenta recriar o envelope com externalId versionado (ex: idDoc:timestamp), pois o NeckSign
                            // bloqueia externalId duplicado mesmo após o envelope anterior ser voidado.
                            var retryRequestBody = new NeckSignCreateEnvelopeRequest
                            {
                                ExternalId = $"{idDoc}:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}",
                                Title = requestBody.Title,
                                WebhookUrl = requestBody.WebhookUrl,
                                Base64Pdf = requestBody.Base64Pdf,
                                Signers = requestBody.Signers
                            };
                            var retryJson = JsonSerializer.Serialize(retryRequestBody, JsonOptions);
                            using var retryRequest = new HttpRequestMessage(HttpMethod.Post, EnvelopesPath)
                            {
                                Content = new StringContent(retryJson, Encoding.UTF8, "application/json")
                            };
                            var retryResponse = await _httpClient.SendAsync(retryRequest);
                            if (retryResponse.StatusCode == HttpStatusCode.Created)
                            {
                                response = retryResponse;
                                retrySucceeded = true;
                            }
                            else
                            {
                                var retryError = await SafeReadBodyAsync(retryResponse);
                                _logger.LogWarning(
                                    "CreateDoc (NeckSign): retry com externalId versionado retornou {StatusCode} para {Type} {IdDoc}. Detalhes: {Error}",
                                    (int)retryResponse.StatusCode, DocumentTypeParser.ToCanonicalString(documentType), idDoc, retryError);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex,
                                "CreateDoc (NeckSign): falha ao voidar envelope anterior {ExistingToken} para {Type} {IdDoc}.",
                                existingToken, DocumentTypeParser.ToCanonicalString(documentType), idDoc);
                        }
                    }

                    if (retrySucceeded)
                    {
                        responseJson = await response.Content.ReadAsStringAsync();
                    }
                    else
                    {
                        var lookupToken = !string.IsNullOrWhiteSpace(existingToken) ? existingToken : idDoc.ToString();
                        try
                        {
                            var lookupResponse = await _httpClient.GetAsync($"{EnvelopesPath}/{Uri.EscapeDataString(lookupToken)}");
                            if (lookupResponse.IsSuccessStatusCode)
                            {
                                responseJson = await lookupResponse.Content.ReadAsStringAsync();
                                response = lookupResponse;
                            }
                            else
                            {
                                _logger.LogError(
                                    "CreateDoc (NeckSign): falha ao recuperar envelope existente ({StatusCode}) para {Type} {IdDoc}.",
                                    (int)lookupResponse.StatusCode, DocumentTypeParser.ToCanonicalString(documentType), idDoc);
                                return false;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex,
                                "CreateDoc (NeckSign): erro ao consultar envelope existente para {Type} {IdDoc}.",
                                DocumentTypeParser.ToCanonicalString(documentType), idDoc);
                            return false;
                        }
                    }
                }
                else
                {
                    _logger.LogError(
                        "CreateDoc (NeckSign): resposta não-201 ({StatusCode}) ao criar envelope para {Type} {IdDoc}. Detalhes: {Error}. Estado local preservado.",
                        (int)response.StatusCode, DocumentTypeParser.ToCanonicalString(documentType), idDoc, errorBody);
                    return false;
                }
            }
            else if (response.StatusCode != HttpStatusCode.Created)
            {
                var errorBody = await SafeReadBodyAsync(response);
                _logger.LogError(
                    "CreateDoc (NeckSign): resposta não-201 ({StatusCode}) ao criar envelope para {Type} {IdDoc}. Detalhes: {Error}. Estado local preservado.",
                    (int)response.StatusCode, DocumentTypeParser.ToCanonicalString(documentType), idDoc, errorBody);
                return false;
            }
            else
            {
                responseJson = await response.Content.ReadAsStringAsync();
            }

            NeckSignCreateEnvelopeResponse? parsed;
            try
            {
                parsed = JsonSerializer.Deserialize<NeckSignCreateEnvelopeResponse>(responseJson, JsonOptions);
            }
            catch (Exception ex)
            {
                // Req 2.11: corpo 201 malformado → log + false, sem alterar a entidade.
                _logger.LogError(ex,
                    "CreateDoc (NeckSign): corpo 201 malformado ao criar envelope para {Type} {IdDoc}; estado local preservado.",
                    DocumentTypeParser.ToCanonicalString(documentType), idDoc);
                return false;
            }

            // Req 2.11: 201 sem envelopeId ou signingUrl → log + false, sem alterar a entidade.
            if (parsed is null ||
                string.IsNullOrWhiteSpace(parsed.EnvelopeId) ||
                string.IsNullOrWhiteSpace(parsed.SigningUrl))
            {
                _logger.LogError(
                    "CreateDoc (NeckSign): 201 sem 'envelopeId'/'signingUrl' para {Type} {IdDoc}; estado local preservado.",
                    DocumentTypeParser.ToCanonicalString(documentType), idDoc);
                return false;
            }

            // Req 2.8/2.9/2.10/15.3: persistência SOMENTE no 201 completo.
            try
            {
                ApplyCreatedEnvelope(documentType, entity, parsed, signers);
                await _appDbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "CreateDoc (NeckSign): falha ao persistir o estado do envelope para {Type} {IdDoc}.",
                    DocumentTypeParser.ToCanonicalString(documentType), idDoc);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Localiza a entidade de documento correspondente a <paramref name="idDoc"/> conforme o tipo,
        /// carregando as navegações necessárias à correlação de <c>signingUrl</c> por papel no
        /// <c>report</c> (Req 2.9). Para <c>pei</c>/<c>paee</c>/<c>caseStudy</c> basta a própria entidade.
        /// </summary>
        private async Task<BaseDocument?> LoadDocumentEntityAsync(DocumentType documentType, Guid idDoc)
        {
            return documentType switch
            {
                DocumentType.Report => await _appDbContext.Reports.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(temp => temp.ID == idDoc),
                DocumentType.Pei => await _appDbContext.PEIs.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(temp => temp.ID == idDoc),
                DocumentType.Paee => await _appDbContext.PAEEs.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(temp => temp.ID == idDoc),
                DocumentType.CaseStudy => await _appDbContext.RegularTeacherEvaluationDocuments.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(temp => temp.ID == idDoc),
                _ => null
            };
        }

        /// <summary>
        /// Deriva um <c>title</c> não vazio para o envelope a partir do tipo e do identificador do
        /// documento (Req 2.2). Usa o <c>StudentID</c> quando disponível, caindo para o GUID da entidade.
        /// </summary>
        internal static string BuildTitle(DocumentType documentType, BaseDocument entity)
        {
            var reference = entity.StudentID?.ToString() ?? entity.ID.ToString();
            var date = DateTime.UtcNow.AddHours(-3).ToString("dd-MM-yyyy");

            var prefix = documentType switch
            {
                DocumentType.Report => "Relatorio",
                DocumentType.Pei => "PEI",
                DocumentType.Paee => "PAEE",
                DocumentType.CaseStudy => "EstudoDeCaso",
                _ => "Documento"
            };

            return $"{prefix}_{reference}_{date}";
        }

        /// <summary>
        /// Aplica à entidade os campos retornados por um <c>201 Created</c> completo:
        /// <c>DocumentToken = envelopeId</c>, <c>SignUrl = signingUrl</c>, <c>DocumentStatus = InProgress</c>
        /// e <c>SignatureProvider = NeckSign</c> (Req 2.8, 2.10, 15.3). Para <c>report</c>, correlaciona
        /// a <c>signingUrl</c> de cada signatário ao papel pelo <c>externalId</c> (Req 2.9).
        /// </summary>
        internal void ApplyCreatedEnvelope(
            DocumentType documentType,
            BaseDocument entity,
            NeckSignCreateEnvelopeResponse response,
            IReadOnlyList<NeckSignSignerInput> composedSigners)
        {
            switch (documentType)
            {
                case DocumentType.Report:
                    // Observação: Report não possui um campo SignUrl único — as URLs de assinatura
                    // são por papel (SocialWorkerSignUrl/PsychologistSignUrl/PsychopedagogueSignUrl).
                    var report = (Report)entity;
                    report.DocumentToken = response.EnvelopeId;
                    report.SignedFile = response.SignedFile;
                    report.SocialWorkerSignUrl = null;
                    report.PsychologistSignUrl = null;
                    report.PsychopedagogueSignUrl = null;
                    report.SocialWorkerSign = false;
                    report.PsychologistSign = false;
                    report.PsychopedagogueSign = false;
                    report.SocialWorkerSignedAt = null;
                    report.PsychologistSignedAt = null;
                    report.PsychopedagogueSignedAt = null;

                    // Req 2.9: correlaciona a signingUrl por papel. O NeckSign retorna signers[] com
                    // { email, signingUrl } (sem externalId), então a correlação é POR E-MAIL: cada
                    // papel do sistema tem um externalId (ID do sub-relatório) e um e-mail conhecidos
                    // (de composedSigners); casamos esse e-mail com a signingUrl retornada pelo
                    // provedor. Mantemos o casamento por externalId como fallback caso o provedor
                    // volte a enviá-lo. A comparação de e-mail é case-insensitive.
                    if (response.Signers is { Count: > 0 })
                    {
                        // email → signingUrl (do provedor)
                        var urlByEmail = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                        // externalId → signingUrl (fallback, se o provedor enviar externalId)
                        var urlByExternalId = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                        foreach (var signer in response.Signers)
                        {
                            var url = string.IsNullOrWhiteSpace(signer.SigningUrl)
                                ? response.SigningUrl
                                : signer.SigningUrl;
                            if (!string.IsNullOrWhiteSpace(signer.Email))
                                urlByEmail[signer.Email!.Trim()] = url;
                            if (!string.IsNullOrWhiteSpace(signer.ExternalId))
                                urlByExternalId[signer.ExternalId!.Trim()] = url;
                        }

                        // Resolve a signingUrl de um papel pelo seu externalId (ID do sub-relatório):
                        // localiza o signatário composto correspondente (que carrega o e-mail do papel)
                        // e busca a URL por e-mail; se não achar, tenta por externalId.
                        string? ResolveRoleUrl(string? roleExternalId)
                        {
                            if (string.IsNullOrWhiteSpace(roleExternalId))
                                return null;

                            var composed = composedSigners
                                .FirstOrDefault(s => string.Equals(s.ExternalId, roleExternalId, StringComparison.OrdinalIgnoreCase));

                            if (composed is not null
                                && !string.IsNullOrWhiteSpace(composed.Email)
                                && urlByEmail.TryGetValue(composed.Email.Trim(), out var byEmail)
                                && !string.IsNullOrWhiteSpace(byEmail))
                                return byEmail;

                            return urlByExternalId.TryGetValue(roleExternalId, out var byExt) ? byExt : null;
                        }

                        report.SocialWorkerSignUrl = ResolveRoleUrl(report.SocialWorkerReportID?.ToString());
                        report.PsychologistSignUrl = ResolveRoleUrl(report.PsychologistReportID?.ToString());
                        report.PsychopedagogueSignUrl = ResolveRoleUrl(report.PsychopedagogueReportID?.ToString());
                    }

                    report.DocumentStatus = Status.InProgress;
                    report.SignatureProvider = SignatureProvider.NeckSign;
                    break;

                case DocumentType.Pei:
                    var pei = (PEI)entity;
                    pei.DocumentToken = response.EnvelopeId;
                    pei.SignedFile = response.SignedFile;
                    pei.SignUrl = response.SigningUrl;
                    pei.SignedAt = null;
                    pei.TeacherSign = false;
                    pei.DocumentStatus = Status.InProgress;
                    pei.SignatureProvider = SignatureProvider.NeckSign;
                    break;

                case DocumentType.Paee:
                    var paee = (PAEE)entity;
                    paee.DocumentToken = response.EnvelopeId;
                    paee.SignedFile = response.SignedFile;
                    paee.SignUrl = response.SigningUrl;
                    paee.SignedAt = null;
                    paee.TeacherSign = false;
                    paee.DocumentStatus = Status.InProgress;
                    paee.SignatureProvider = SignatureProvider.NeckSign;
                    break;

                case DocumentType.CaseStudy:
                    var caseStudy = (RegularTeacherEvaluationDocument)entity;
                    caseStudy.DocumentToken = response.EnvelopeId;
                    caseStudy.SignedFile = response.SignedFile;
                    caseStudy.SignUrl = response.SigningUrl;
                    caseStudy.SignedAt = null;
                    caseStudy.TeacherSign = false;
                    caseStudy.DocumentStatus = Status.InProgress;
                    caseStudy.SignatureProvider = SignatureProvider.NeckSign;
                    break;
            }
        }

        /// <summary>Lê o corpo da resposta de forma segura para fins de log, sem lançar exceção.</summary>
        private static async Task<string> SafeReadBodyAsync(HttpResponseMessage response)
        {
            try
            {
                return await response.Content.ReadAsStringAsync();
            }
            catch
            {
                return "<corpo indisponível>";
            }
        }

        #region Composição e filtragem de signatários (task 4.2)

        /// <summary>
        /// Compõe e filtra a lista de signatários para um documento, aplicando as regras de negócio
        /// de inclusão (nome <b>e</b> e-mail não vazios) e deduplicação por e-mail (case-insensitive).
        /// Este é o ponto de entrada único que <see cref="CreateDoc"/> (task 4.3) consome para montar
        /// o corpo do <c>POST /api/v1/envelopes</c> (Req 2.5, 2.6, 2.7).
        /// </summary>
        /// <param name="documentType">Tipo de documento já validado via <see cref="DocumentTypeParser"/>.</param>
        /// <param name="idDoc">GUID da entidade do documento (usado como <c>externalId</c> do envelope).</param>
        /// <returns>
        /// A lista filtrada de <see cref="NeckSignSignerInput"/>. <b>Uma lista vazia</b> (seja porque a
        /// entidade não foi encontrada, seja porque nenhum signatário qualificou após a filtragem)
        /// sinaliza ao chamador que o envelope <b>não deve</b> ser criado: <see cref="CreateDoc"/>
        /// deve registrar em log e retornar <c>false</c> <b>sem</b> realizar o POST (Req 2.7).
        /// </returns>
        /// <remarks>
        /// A ordem de assinatura (<see cref="NeckSignSignerInput.Order"/>) é atribuída de forma
        /// 1-based conforme a posição na lista resultante, respeitando a ordem de avaliação dos papéis
        /// para <c>report</c> (assistente social → psicólogo → psicopedagogo).
        /// </remarks>
        internal async Task<IReadOnlyList<NeckSignSignerInput>> ComposeSignersAsync(DocumentType documentType, Guid idDoc)
        {
            var signers = documentType switch
            {
                DocumentType.Report => await ComposeReportSignersAsync(idDoc),
                DocumentType.Pei => await ComposePeiSignersAsync(idDoc),
                DocumentType.Paee => await ComposePaeeSignersAsync(idDoc),
                DocumentType.CaseStudy => await ComposeCaseStudySignersAsync(idDoc),
                _ => new List<NeckSignSignerInput>()
            };

            return AssignOrder(signers);
        }

        /// <summary>
        /// Compõe os signatários de um <c>report</c>: até três papéis — Assistente Social,
        /// Psicólogo e Psicopedagogo — incluindo somente papéis com nome <b>e</b> e-mail não vazios
        /// e descartando papel cujo e-mail já tenha sido incluído (dedup case-insensitive). Reutiliza
        /// exatamente as mesmas fontes de dados do <c>ZapSignService</c> (Req 2.5).
        /// </summary>
        private async Task<List<NeckSignSignerInput>> ComposeReportSignersAsync(Guid idDoc)
        {
            var report = await _appDbContext.Reports.IgnoreQueryFilters()
                .Include(p => p.SocialWorkerReport).ThenInclude(u => u != null ? u.User : null)
                .Include(p => p.PsychologistReport).ThenInclude(u => u.User)
                .Include(p => p.PsychologistReport).ThenInclude(p => p.PsychologistQuestionnaire).ThenInclude(u => u != null ? u.User : null)
                .Include(p => p.PsychopedagogueReport).ThenInclude(u => u.User)
                .Include(p => p.PsychopedagogueReport).ThenInclude(p => p.PsychopedagogueQuestionnaire).ThenInclude(u => u != null ? u.User : null)
                .FirstOrDefaultAsync(temp => temp.ID == idDoc);

            var signers = new List<NeckSignSignerInput>();

            if (report is null)
            {
                _logger.LogInformation(
                    "Composição de signatários abortada: Report {IdDoc} não encontrado.", idDoc);
                return signers;
            }

            var addedEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Assistente Social
            var socialWorkerName = report.SocialWorkerReport?.User?.FullName;
            var socialWorkerEmail = report.SocialWorkerReport?.User?.Email;
            AddSignerIfQualified(
                signers, addedEmails,
                socialWorkerName, socialWorkerEmail,
                report.SocialWorkerReport?.ID.ToString());

            // Psicólogo (prioriza o usuário do questionário, com fallback para o do laudo)
            var psychName = report.PsychologistReport?.PsychologistQuestionnaire?.User?.FullName
                            ?? report.PsychologistReport?.User?.FullName;
            var psychEmail = report.PsychologistReport?.PsychologistQuestionnaire?.User?.Email
                             ?? report.PsychologistReport?.User?.Email;
            AddSignerIfQualified(
                signers, addedEmails,
                psychName, psychEmail,
                report.PsychologistReport?.ID.ToString());

            // Psicopedagogo (prioriza o usuário do questionário, com fallback para o do laudo)
            var psychopedName = report.PsychopedagogueReport?.PsychopedagogueQuestionnaire?.User?.FullName
                                ?? report.PsychopedagogueReport?.User?.FullName;
            var psychopedEmail = report.PsychopedagogueReport?.PsychopedagogueQuestionnaire?.User?.Email
                                 ?? report.PsychopedagogueReport?.User?.Email;
            AddSignerIfQualified(
                signers, addedEmails,
                psychopedName, psychopedEmail,
                report.PsychopedagogueReport?.ID.ToString());

            if (signers.Count == 0)
            {
                _logger.LogInformation(
                    "Composição de signatários do Report {IdDoc} resultou em lista vazia após filtragem.", idDoc);
            }

            return signers;
        }

        /// <summary>
        /// Compõe o signatário único de um <c>pei</c>: o responsável (<c>PEI.User</c>), incluído
        /// apenas se nome <b>e</b> e-mail estiverem preenchidos (Req 2.6).
        /// </summary>
        private async Task<List<NeckSignSignerInput>> ComposePeiSignersAsync(Guid idDoc)
        {
            var pei = await _appDbContext.PEIs.IgnoreQueryFilters()
                .Include(u => u.User)
                .FirstOrDefaultAsync(temp => temp.ID == idDoc);

            return ComposeSingleResponsibleSigner("PEI", idDoc, pei?.User?.FullName, pei?.User?.Email, pei?.User?.Id);
        }

        /// <summary>
        /// Compõe o signatário único de um <c>paee</c>: o responsável (<c>PAEE.User</c>), incluído
        /// apenas se nome <b>e</b> e-mail estiverem preenchidos (Req 2.6).
        /// </summary>
        private async Task<List<NeckSignSignerInput>> ComposePaeeSignersAsync(Guid idDoc)
        {
            var paee = await _appDbContext.PAEEs.IgnoreQueryFilters()
                .Include(u => u.User)
                .FirstOrDefaultAsync(temp => temp.ID == idDoc);

            return ComposeSingleResponsibleSigner("PAEE", idDoc, paee?.User?.FullName, paee?.User?.Email, paee?.User?.Id);
        }

        /// <summary>
        /// Compõe o signatário único de um <c>caseStudy</c>: o responsável
        /// (<c>RegularTeacherEvaluationDocument.User</c>), incluído apenas se nome <b>e</b> e-mail
        /// estiverem preenchidos (Req 2.6).
        /// </summary>
        private async Task<List<NeckSignSignerInput>> ComposeCaseStudySignersAsync(Guid idDoc)
        {
            var caseStudy = await _appDbContext.RegularTeacherEvaluationDocuments.IgnoreQueryFilters()
                .Include(u => u.User)
                .FirstOrDefaultAsync(temp => temp.ID == idDoc);

            return ComposeSingleResponsibleSigner("CaseStudy", idDoc, caseStudy?.User?.FullName, caseStudy?.User?.Email, caseStudy?.User?.Id);
        }

        /// <summary>
        /// Monta a lista (com no máximo um elemento) para documentos de signatário único
        /// (<c>pei</c>/<c>paee</c>/<c>caseStudy</c>), aplicando a regra de nome <b>e</b> e-mail não
        /// vazios. Lista vazia sinaliza ao chamador que o envelope não deve ser criado (Req 2.6, 2.7).
        /// </summary>
        private List<NeckSignSignerInput> ComposeSingleResponsibleSigner(
            string entityLabel, Guid idDoc, string? name, string? email, string? externalId)
        {
            var signers = new List<NeckSignSignerInput>();
            var addedEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            AddSignerIfQualified(signers, addedEmails, name, email, externalId);

            if (signers.Count == 0)
            {
                _logger.LogInformation(
                    "Composição de signatários do {Entity} {IdDoc} resultou em lista vazia: responsável sem nome e/ou e-mail.",
                    entityLabel, idDoc);
            }

            return signers;
        }

        /// <summary>
        /// Adiciona um signatário à lista somente se nome <b>e</b> e-mail forem não vazios e o e-mail
        /// (comparado de forma case-insensitive, após <c>Trim</c>) ainda não tiver sido incluído.
        /// Centraliza as regras de inclusão (Req 2.5, 2.6) e dedup por e-mail.
        /// </summary>
        private static void AddSignerIfQualified(
            List<NeckSignSignerInput> signers,
            HashSet<string> addedEmails,
            string? name,
            string? email,
            string? externalId)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
                return;

            var trimmedEmail = email.Trim();
            if (!addedEmails.Add(trimmedEmail))
                return;

            signers.Add(new NeckSignSignerInput
            {
                Name = name,
                Email = email,
                Role = NeckSignSignerRole.SIGNER,
                ExternalId = externalId ?? string.Empty,
            });
        }

        /// <summary>
        /// Atribui a ordem 1-based de assinatura a cada signatário conforme sua posição na lista,
        /// retornando uma lista nova e imutável do ponto de vista do chamador.
        /// </summary>
        private static IReadOnlyList<NeckSignSignerInput> AssignOrder(List<NeckSignSignerInput> signers)
        {
            var ordered = new List<NeckSignSignerInput>(signers.Count);
            for (var i = 0; i < signers.Count; i++)
            {
                var s = signers[i];
                ordered.Add(new NeckSignSignerInput
                {
                    Name = s.Name,
                    Email = s.Email,
                    Role = s.Role,
                    Order = i + 1,
                    ExternalId = s.ExternalId,
                });
            }

            return ordered;
        }

        #endregion

        /// <inheritdoc />
        /// <remarks>
        /// Cancela (void) o envelope no NeckSign (task 5.1): <c>POST /api/v1/envelopes/:id/void</c>
        /// usando o <c>DocumentToken</c> como <c>:id</c>. Retorna <c>true</c> em <c>200</c>/<c>204</c>.
        /// Tipo inválido, entidade inexistente ou <c>DocumentToken</c> vazio/nulo → log + <c>false</c>
        /// <b>sem</b> requisição. O <c>DocumentStatus</c> local é preservado em qualquer falha
        /// (Req 3.1–3.3).
        /// </remarks>
        public async Task<bool> DeleteDoc(Guid idDoc, string type)
        {
            // Req 1.5/1.6: validação de tipo (case-sensitive); inválido → log + false, sem exceção.
            if (!DocumentTypeParser.TryParse(type, out var documentType))
            {
                _logger.LogInformation("Tipo de documento inválido em DeleteDoc (NeckSign): '{Type}'", type);
                return false;
            }

            var entity = await LoadDocumentEntityAsync(documentType, idDoc);

            // Req 3.2: entidade inexistente → log + false, sem request.
            if (entity is null)
            {
                _logger.LogInformation(
                    "DeleteDoc (NeckSign) abortado: entidade {Type} {IdDoc} não encontrada; nenhuma requisição enviada.",
                    DocumentTypeParser.ToCanonicalString(documentType), idDoc);
                return false;
            }

            var documentToken = GetDocumentToken(documentType, entity);

            // Req 3.2: DocumentToken vazio/nulo → log + false, sem request.
            if (string.IsNullOrWhiteSpace(documentToken))
            {
                _logger.LogInformation(
                    "DeleteDoc (NeckSign) abortado: {Type} {IdDoc} sem DocumentToken; nenhuma requisição enviada.",
                    DocumentTypeParser.ToCanonicalString(documentType), idDoc);
                return false;
            }

            HttpResponseMessage response;
            try
            {
                var voidPayload = JsonSerializer.Serialize(new { reason = "Documento cancelado ou reemitido" }, JsonOptions);
                using var request = new HttpRequestMessage(
                    HttpMethod.Post, $"{EnvelopesPath}/{Uri.EscapeDataString(documentToken)}/void")
                {
                    Content = new StringContent(voidPayload, Encoding.UTF8, "application/json")
                };
                response = await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                // Req 3.3: falha de comunicação/timeout → log + false, estado local preservado.
                _logger.LogError(ex,
                    "DeleteDoc (NeckSign): falha ao cancelar envelope {DocumentToken} do {Type} {IdDoc}; estado local preservado.",
                    documentToken, DocumentTypeParser.ToCanonicalString(documentType), idDoc);
                return false;
            }

            // Req 3.1: 200/204 → true.
            if (response.StatusCode is HttpStatusCode.OK or HttpStatusCode.NoContent)
            {
                return true;
            }

            // Req 3.3: qualquer outro status → log + false, DocumentStatus preservado.
            var errorBody = await SafeReadBodyAsync(response);
            _logger.LogError(
                "DeleteDoc (NeckSign): resposta inesperada ({StatusCode}) ao cancelar envelope {DocumentToken} do {Type} {IdDoc}. Detalhes: {Error}. Estado local preservado.",
                (int)response.StatusCode, documentToken, DocumentTypeParser.ToCanonicalString(documentType), idDoc, errorBody);
            return false;
        }

        /// <summary>Lê o <c>DocumentToken</c> da entidade conforme o tipo de documento.</summary>
        private static string? GetDocumentToken(DocumentType documentType, BaseDocument entity) =>
            documentType switch
            {
                DocumentType.Report => ((Report)entity).DocumentToken,
                DocumentType.Pei => ((PEI)entity).DocumentToken,
                DocumentType.Paee => ((PAEE)entity).DocumentToken,
                DocumentType.CaseStudy => ((RegularTeacherEvaluationDocument)entity).DocumentToken,
                _ => null
            };

        /// <inheritdoc />
        /// <remarks>
        /// Consulta o envelope no NeckSign (task 5.2): <c>GET /api/v1/envelopes/:id</c> usando
        /// <paramref name="documentToken"/> como <c>:id</c>, e mapeia para o DTO neutro
        /// <see cref="SignatureDocumentDetails"/> (<c>ExternalId</c>, <c>Status</c>, <c>Signers[]</c>,
        /// <c>SignedFileUrl</c> quando <c>SIGNED</c>). O parâmetro <paramref name="bypassCooldown"/>
        /// é <b>no-op</b> (o NeckSign não aplica cooldown; a consulta é sempre feita a fresco — Req 3.5).
        /// <c>404</c>, <c>&gt;=500</c> ou erro de comunicação → log + <c>null</c> (Req 3.7); os métodos
        /// que persistem tratam <c>null</c> como falha.
        /// </remarks>
        public async Task<SignatureDocumentDetails?> GetDocumentDetails(string documentToken, bool bypassCooldown = false)
        {
            // Req 3.5: bypassCooldown é no-op — nenhuma lógica de cooldown/cache; consulta sempre fresca.
            if (string.IsNullOrWhiteSpace(documentToken))
            {
                _logger.LogInformation("GetDocumentDetails (NeckSign): documentToken vazio/nulo; retornando null.");
                return null;
            }

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.GetAsync($"{EnvelopesPath}/{Uri.EscapeDataString(documentToken)}");
            }
            catch (Exception ex)
            {
                // Req 3.7: erro de comunicação/timeout → log + null.
                _logger.LogWarning(ex,
                    "GetDocumentDetails (NeckSign): falha de comunicação ao consultar envelope {DocumentToken}.",
                    documentToken);
                return null;
            }

            // Req 3.7: 404 ou >=500 → log + null. (demais não-sucesso também retornam null, de forma defensiva)
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "GetDocumentDetails (NeckSign): status {StatusCode} ao consultar envelope {DocumentToken}; retornando null.",
                    (int)response.StatusCode, documentToken);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();

            NeckSignEnvelopeResponse? envelope;
            try
            {
                envelope = JsonSerializer.Deserialize<NeckSignEnvelopeResponse>(json, JsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "GetDocumentDetails (NeckSign): falha ao desserializar o envelope {DocumentToken}; retornando null.",
                    documentToken);
                return null;
            }

            if (envelope is null)
            {
                _logger.LogWarning(
                    "GetDocumentDetails (NeckSign): corpo nulo ao consultar envelope {DocumentToken}; retornando null.",
                    documentToken);
                return null;
            }

            var neutralStatus = MapEnvelopeStatus(envelope.Status);
            var isSigned = neutralStatus == SignatureDocumentStatus.Signed;

            var signers = (envelope.Signers ?? new List<NeckSignEnvelopeSigner>())
                .Select(s => new SignatureSigner
                {
                    ExternalId = s.ExternalId ?? string.Empty,
                    Name = string.IsNullOrWhiteSpace(s.Name) ? null : s.Name,
                    Email = string.IsNullOrWhiteSpace(s.Email) ? null : s.Email,
                    Signed = s.SignedAt != null || string.Equals(s.Status, "SIGNED", StringComparison.OrdinalIgnoreCase),
                    SignedAt = s.SignedAt
                })
                .ToList();

            // A resposta do GET /api/v1/envelopes/:id do NeckSign NÃO inclui a URL do PDF assinado.
            // Quando o envelope está SIGNED, a URL pré-assinada é obtida no endpoint dedicado
            // GET /api/v1/envelopes/:id/download (retorna { downloadUrl, expiresIn }). Buscamos essa
            // URL aqui para preencher SignedFileUrl; qualquer falha mantém SignedFileUrl nulo sem
            // comprometer o status já resolvido.
            string? signedFileUrl = null;
            if (isSigned)
            {
                signedFileUrl = await TryGetSignedDownloadUrlAsync(documentToken)
                    ?? (string.IsNullOrWhiteSpace(envelope.SignedFileUrl) ? null : envelope.SignedFileUrl);
            }

            return new SignatureDocumentDetails
            {
                ExternalId = envelope.ExternalId ?? string.Empty,
                SignedFileUrl = signedFileUrl,
                Status = neutralStatus,
                Signers = signers
            };
        }

        /// <summary>
        /// Obtém a URL pré-assinada do PDF assinado via <c>GET /api/v1/envelopes/:id/download</c> do
        /// NeckSign (resposta <c>{ downloadUrl, expiresIn }</c>). Usado por <see cref="GetDocumentDetails"/>
        /// porque a resposta do GET do envelope não inclui a URL do arquivo assinado. Qualquer falha
        /// (não-sucesso, corpo inesperado, erro de comunicação) retorna <c>null</c> sem lançar, para não
        /// comprometer o status já resolvido do envelope.
        /// </summary>
        private async Task<string?> TryGetSignedDownloadUrlAsync(string documentToken)
        {
            try
            {
                var response = await _httpClient.GetAsync(
                    $"{EnvelopesPath}/{Uri.EscapeDataString(documentToken)}/download");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "GetDocumentDetails (NeckSign): download retornou status {StatusCode} para o envelope {DocumentToken}; SignedFileUrl ficará nulo.",
                        (int)response.StatusCode, documentToken);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var payload = JsonSerializer.Deserialize<NeckSignDownloadResponse>(json, JsonOptions);
                return string.IsNullOrWhiteSpace(payload?.DownloadUrl) ? null : payload.DownloadUrl;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "GetDocumentDetails (NeckSign): falha ao obter a URL de download do envelope {DocumentToken}; SignedFileUrl ficará nulo.",
                    documentToken);
                return null;
            }
        }

        /// <summary>
        /// Mapeia o status textual de envelope do NeckSign
        /// (<c>DRAFT</c>/<c>PENDING</c>/<c>SIGNING</c>/<c>SIGNED</c>/<c>DECLINED</c>/<c>EXPIRED</c>/<c>VOIDED</c>)
        /// para o enum neutro <see cref="SignatureDocumentStatus"/>, sem expor tipos específicos do
        /// NeckSign na assinatura pública. A comparação é case-insensitive; valores desconhecidos ou
        /// <c>DRAFT</c> caem em <see cref="SignatureDocumentStatus.Pending"/>.
        /// </summary>
        /// <remarks>
        /// Correspondência com o estado local do sistema (aplicada por <c>SyncDocument</c>/webhook):
        /// <c>PENDING</c>/<c>SIGNING</c> → <c>InProgress</c>; <c>SIGNED</c> → <c>Complete</c>;
        /// <c>DECLINED</c> → <c>Declined</c>; <c>EXPIRED</c> → <c>Expired</c>; <c>VOIDED</c> → sem
        /// alteração de estado local.
        /// </remarks>
        private static SignatureDocumentStatus MapEnvelopeStatus(string? neckSignStatus)
        {
            if (string.IsNullOrWhiteSpace(neckSignStatus))
                return SignatureDocumentStatus.Pending;

            return neckSignStatus.Trim().ToUpperInvariant() switch
            {
                "PENDING" => SignatureDocumentStatus.Pending,
                "SIGNING" => SignatureDocumentStatus.Signing,
                "SIGNED" => SignatureDocumentStatus.Signed,
                "DECLINED" => SignatureDocumentStatus.Declined,
                "EXPIRED" => SignatureDocumentStatus.Expired,
                "VOIDED" => SignatureDocumentStatus.Voided,
                "DRAFT" => SignatureDocumentStatus.Pending,
                _ => SignatureDocumentStatus.Pending
            };
        }

        /// <inheritdoc />
        /// <remarks>
        /// Reconciliação do estado local de um documento (task 5.3). Consulta o envelope via
        /// <see cref="GetDocumentDetails"/> e, correlacionando pela entidade cujo <c>ID</c> é igual
        /// ao <c>externalId</c> (GUID) do envelope — entre as quatro entidades (PAEE/PEI/CaseStudy/Report),
        /// como em <c>ZapSignService.SyncDocument</c> — ajusta os campos locais pelo status neutro do
        /// envelope:
        /// <list type="bullet">
        /// <item><c>Signed</c> → <c>DocumentStatus = Complete</c> (+ <c>SignedAt</c> + <c>SignedFile</c> = <c>SignedFileUrl</c>);</item>
        /// <item><c>Pending</c>/<c>Signing</c> → <c>DocumentStatus = InProgress</c>;</item>
        /// <item><c>Declined</c> → <c>DocumentStatus = Declined</c>; <c>Expired</c> → <c>DocumentStatus = Expired</c>;</item>
        /// <item><c>Voided</c> → sem alteração de estado local (preserva).</item>
        /// </list>
        /// Para <c>report</c>, os campos por papel (Assistente Social/Psicólogo/Psicopedagogo) são
        /// correlacionados pelo <c>externalId</c> do signatário e o <c>DocumentStatus</c> só vira
        /// <c>Complete</c> quando os três assinaram (senão <c>InProgress</c>). Porém, quando o envelope
        /// resolve em <c>Declined</c>/<c>Expired</c>, o <c>DocumentStatus</c> do <c>report</c> vai para o
        /// terminal correspondente para o documento inteiro, independentemente de quantos papéis
        /// assinaram, e os campos por papel já preenchidos são <b>preservados</b> (não zerados) como fato
        /// histórico. Em qualquer falha (<see cref="GetDocumentDetails"/> retornando <c>null</c>), o
        /// estado local é preservado e o método retorna <c>false</c> (Req 3.6, 3.7).
        /// </remarks>
        public async Task<bool> SyncDocument(string documentToken, bool bypassCooldown = false)
        {
            // Req 3.7: falha do provedor (null) → preserva estado local, retorna false.
            var details = await GetDocumentDetails(documentToken, bypassCooldown);
            if (details is null)
            {
                _logger.LogInformation(
                    "SyncDocument (NeckSign): detalhes indisponíveis para o token informado; estado local preservado.");
                return false;
            }

            // Correlação por externalId (GUID simples ou GUID:timestamp) ou por DocumentToken == documentToken.
            var extIdRaw = details.ExternalId?.Trim() ?? string.Empty;
            var idPart = extIdRaw.Contains(':') ? extIdRaw.Split(':')[0] : (extIdRaw.Contains('_') ? extIdRaw.Split('_')[0] : extIdRaw);
            Guid.TryParse(idPart, out var externalId);

            var paee = await _appDbContext.PAEEs.FirstOrDefaultAsync(x => (externalId != Guid.Empty && x.ID == externalId) || x.DocumentToken == documentToken);
            var pei = await _appDbContext.PEIs.FirstOrDefaultAsync(x => (externalId != Guid.Empty && x.ID == externalId) || x.DocumentToken == documentToken);
            var caseStudy = await _appDbContext.RegularTeacherEvaluationDocuments.FirstOrDefaultAsync(x => (externalId != Guid.Empty && x.ID == externalId) || x.DocumentToken == documentToken);
            var report = await _appDbContext.Reports
                .Include(x => x.SocialWorkerReport).ThenInclude(x => x!.User)
                .Include(x => x.PsychologistReport).ThenInclude(x => x!.User)
                .Include(x => x.PsychologistReport).ThenInclude(x => x!.PsychologistQuestionnaire).ThenInclude(x => x!.User)
                .Include(x => x.PsychopedagogueReport).ThenInclude(x => x!.User)
                .Include(x => x.PsychopedagogueReport).ThenInclude(x => x!.PsychopedagogueQuestionnaire).ThenInclude(x => x!.User)
                .FirstOrDefaultAsync(x => (externalId != Guid.Empty && x.ID == externalId) || x.DocumentToken == documentToken);

            if (paee is null && pei is null && caseStudy is null && report is null)
            {
                _logger.LogWarning(
                    "SyncDocument (NeckSign): nenhuma entidade correlacionada ao externalId {ExternalId}; nada a reconciliar.",
                    externalId);
                return false;
            }

            // Req 4.5/8.2: VOIDED não altera estado local (preserva). Nada a fazer.
            if (details.Status == SignatureDocumentStatus.Voided)
            {
                _logger.LogInformation(
                    "SyncDocument (NeckSign): envelope {ExternalId} em VOIDED; estado local preservado sem alteração.",
                    externalId);
                return true;
            }

            if (paee is not null)
                ReconcilePaee(paee, details);
            if (pei is not null)
                ReconcilePei(pei, details);
            if (caseStudy is not null)
                ReconcileCaseStudy(caseStudy, details);
            if (report is not null)
                ReconcileReport(report, details);

            // Persiste apenas se houver mudanças de fato (idempotência — Req 8.6).
            if (_appDbContext.ChangeTracker.HasChanges())
            {
                await _appDbContext.SaveChangesAsync();
            }

            return true;
        }

        /// <summary>
        /// Resultado da reconciliação de uma entidade de signatário único, calculado uma única vez a
        /// partir do status neutro do envelope e então aplicado à entidade concreta
        /// (<c>pei</c>/<c>paee</c>/<c>caseStudy</c>). Como as três entidades não compartilham uma
        /// interface comum para esses campos, o cálculo é centralizado aqui e a aplicação é feita por
        /// appliers específicos de tipo, evitando duplicação da lógica de decisão.
        /// </summary>
        private readonly record struct SingleSignerReconciliation(
            Status DocumentStatus,
            bool ApplySignedFields,
            bool TeacherSign,
            DateTime? SignedAt,
            string? SignedFile);

        /// <summary>
        /// Calcula a reconciliação de uma entidade de signatário único a partir do status neutro do
        /// envelope. <c>Signed</c> → <c>Complete</c> (+ <c>SignedAt</c> do primeiro signatário assinante
        /// + <c>SignedFile</c> da URL do PDF assinado); <c>Pending</c>/<c>Signing</c> → <c>InProgress</c>
        /// (preserva os demais campos); <c>Declined</c>/<c>Expired</c> → estado terminal correspondente,
        /// preservando os campos de assinatura já existentes. <c>Voided</c> é tratado antes (sem alteração).
        /// </summary>
        private static SingleSignerReconciliation ComputeSingleSignerReconciliation(SignatureDocumentDetails details)
        {
            switch (details.Status)
            {
                case SignatureDocumentStatus.Signed:
                    var signer = details.Signers.FirstOrDefault(s => s.Signed)
                                 ?? details.Signers.FirstOrDefault();
                    // NÃO persistir a URL de download do NeckSign em SignedFile: ela é uma presigned
                    // URL EFÊMERA (TTL ~1h) de um bucket externo (necksign-documents-prod). Persistí-la
                    // (a) a deixaria quebrada após a expiração e (b) faria o DocumentsSignatureController
                    // classificá-la como "S3 próprio" (IsOwnS3Reference casa qualquer host *.s3.amazonaws.com)
                    // e RE-ASSINÁ-LA sobre o bucket próprio, gerando uma URL aninhada inválida (NoSuchKey).
                    // Para documentos NeckSign, a URL do PDF assinado é resolvida SEMPRE na hora, via
                    // GetDocumentDetails → GET /api/v1/envelopes/:id/download. SignedFile fica nulo.
                    return new SingleSignerReconciliation(
                        Status.Complete,
                        ApplySignedFields: true,
                        TeacherSign: true,
                        SignedAt: signer?.SignedAt?.UtcDateTime,
                        SignedFile: null);

                case SignatureDocumentStatus.Pending:
                case SignatureDocumentStatus.Signing:
                    return new SingleSignerReconciliation(Status.InProgress, false, false, null, null);

                case SignatureDocumentStatus.Declined:
                    // Terminal: preserva SignedAt/TeacherSign/SignedFile já preenchidos como fato histórico.
                    return new SingleSignerReconciliation(Status.Declined, false, false, null, null);

                case SignatureDocumentStatus.Expired:
                    return new SingleSignerReconciliation(Status.Expired, false, false, null, null);

                default:
                    // Voided é tratado antes de chegar aqui (SyncDocument retorna sem alterar estado);
                    // defensivamente mapeamos qualquer caso remanescente para InProgress sem tocar nos
                    // campos de assinatura.
                    return new SingleSignerReconciliation(Status.InProgress, false, false, null, null);
            }
        }

        /// <summary>Reconcilia um <c>paee</c> aplicando o resultado de <see cref="ComputeSingleSignerReconciliation"/>.</summary>
        private static void ReconcilePaee(PAEE paee, SignatureDocumentDetails details)
        {
            var r = ComputeSingleSignerReconciliation(details);
            if (r.ApplySignedFields)
            {
                paee.SignedAt = r.SignedAt;
                paee.TeacherSign = r.TeacherSign;
                if (r.SignedFile is not null)
                    paee.SignedFile = r.SignedFile;
            }
            paee.DocumentStatus = r.DocumentStatus;
        }

        /// <summary>Reconcilia um <c>pei</c> aplicando o resultado de <see cref="ComputeSingleSignerReconciliation"/>.</summary>
        private static void ReconcilePei(PEI pei, SignatureDocumentDetails details)
        {
            var r = ComputeSingleSignerReconciliation(details);
            if (r.ApplySignedFields)
            {
                pei.SignedAt = r.SignedAt;
                pei.TeacherSign = r.TeacherSign;
                if (r.SignedFile is not null)
                    pei.SignedFile = r.SignedFile;
            }
            pei.DocumentStatus = r.DocumentStatus;
        }

        /// <summary>Reconcilia um <c>caseStudy</c> aplicando o resultado de <see cref="ComputeSingleSignerReconciliation"/>.</summary>
        private static void ReconcileCaseStudy(RegularTeacherEvaluationDocument caseStudy, SignatureDocumentDetails details)
        {
            var r = ComputeSingleSignerReconciliation(details);
            if (r.ApplySignedFields)
            {
                caseStudy.SignedAt = r.SignedAt;
                caseStudy.TeacherSign = r.TeacherSign;
                if (r.SignedFile is not null)
                    caseStudy.SignedFile = r.SignedFile;
            }
            caseStudy.DocumentStatus = r.DocumentStatus;
        }

        /// <summary>
        /// Reconcilia um <c>report</c> (multi-papel) pelo status neutro do envelope. Para
        /// <c>Signed</c>/<c>Pending</c>/<c>Signing</c>, correlaciona os campos por papel pelo
        /// <c>externalId</c> do signatário e marca <c>Complete</c> somente quando os três papéis
        /// assinaram (senão <c>InProgress</c>). Para <c>Declined</c>/<c>Expired</c>, o
        /// <c>DocumentStatus</c> vai direto ao terminal correspondente para o documento inteiro,
        /// independentemente de quantos papéis assinaram, e os campos por papel já preenchidos são
        /// <b>preservados</b> (não zerados) como fato histórico (Req 8.2/8.3/8.4).
        /// </summary>
        private void ReconcileReport(Report report, SignatureDocumentDetails details)
        {
            // Req 8.2/8.3/8.4: terminal do documento inteiro; preserva campos por papel já preenchidos.
            if (details.Status is SignatureDocumentStatus.Declined or SignatureDocumentStatus.Expired)
            {
                report.DocumentStatus = details.Status == SignatureDocumentStatus.Declined
                    ? Status.Declined
                    : Status.Expired;
                return;
            }

            // NÃO persistir a URL de download do NeckSign em SignedFile (presigned, efêmera):
            // é resolvida na hora via GetDocumentDetails → /download. Ver nota em
            // ComputeSingleSignerReconciliation.

            // Obtém os e-mails dos três profissionais responsáveis pelos papéis no relatório
            var socialEmail = report.SocialWorkerReport?.User?.Email;
            var psychEmail = report.PsychologistReport?.PsychologistQuestionnaire?.User?.Email
                             ?? report.PsychologistReport?.User?.Email;
            var psychopedEmail = report.PsychopedagogueReport?.PsychopedagogueQuestionnaire?.User?.Email
                                 ?? report.PsychopedagogueReport?.User?.Email;

            var socialSigner = FindSignerForRole(details, report.SocialWorkerReportID, socialEmail);
            var psychologistSigner = FindSignerForRole(details, report.PsychologistReportID, psychEmail);
            var psychopedagogueSigner = FindSignerForRole(details, report.PsychopedagogueReportID, psychopedEmail);

            // Status SIGNED do envelope significa, por definição do NeckSign, que TODOS os
            // signatários assinaram ("Everyone has signed").
            if (details.Status == SignatureDocumentStatus.Signed)
            {
                var defaultSignedAt = details.Signers
                    .Select(s => s.SignedAt?.UtcDateTime)
                    .FirstOrDefault(d => d is not null) ?? DateTime.UtcNow;

                report.SocialWorkerSign = true;
                report.PsychologistSign = true;
                report.PsychopedagogueSign = true;
                report.SocialWorkerSignedAt ??= socialSigner?.SignedAt?.UtcDateTime ?? defaultSignedAt;
                report.PsychologistSignedAt ??= psychologistSigner?.SignedAt?.UtcDateTime ?? defaultSignedAt;
                report.PsychopedagogueSignedAt ??= psychopedagogueSigner?.SignedAt?.UtcDateTime ?? defaultSignedAt;
                report.DocumentStatus = Status.Complete;
                return;
            }

            // Para PENDING/SIGNING (parcial): atualiza cada papel que já assinou individualmente.
            if (socialSigner is not null && (socialSigner.Signed || socialSigner.SignedAt is not null))
            {
                report.SocialWorkerSign = true;
                report.SocialWorkerSignedAt = socialSigner.SignedAt?.UtcDateTime ?? DateTime.UtcNow;
            }
            if (psychologistSigner is not null && (psychologistSigner.Signed || psychologistSigner.SignedAt is not null))
            {
                report.PsychologistSign = true;
                report.PsychologistSignedAt = psychologistSigner.SignedAt?.UtcDateTime ?? DateTime.UtcNow;
            }
            if (psychopedagogueSigner is not null && (psychopedagogueSigner.Signed || psychopedagogueSigner.SignedAt is not null))
            {
                report.PsychopedagogueSign = true;
                report.PsychopedagogueSignedAt = psychopedagogueSigner.SignedAt?.UtcDateTime ?? DateTime.UtcNow;
            }

            // Req 3.6: Complete só com os três papéis assinados; senão InProgress.
            report.DocumentStatus =
                report.SocialWorkerSign && report.PsychologistSign && report.PsychopedagogueSign
                    ? Status.Complete
                    : Status.InProgress;
        }

        /// <summary>
        /// Localiza o signatário do envelope correspondente a um papel:
        /// 1) Primeiramente por <c>ExternalId</c> (se fornecido pelo provedor);
        /// 2) Secundariamente por <c>Email</c> (case-insensitive);
        /// Prioriza sempre um signatário que já tenha assinado.
        /// </summary>
        private static SignatureSigner? FindSignerForRole(SignatureDocumentDetails details, Guid? roleExternalId, string? roleEmail)
        {
            if (details.Signers == null || details.Signers.Count == 0)
                return null;

            // 1. Tenta por ExternalId (se o provedor enviar externalId)
            if (roleExternalId is not null)
            {
                var target = roleExternalId.Value.ToString();
                bool MatchesExt(SignatureSigner s) =>
                    !string.IsNullOrWhiteSpace(s.ExternalId) &&
                    string.Equals(s.ExternalId.Trim(), target, StringComparison.OrdinalIgnoreCase);

                var byExt = details.Signers.FirstOrDefault(s => MatchesExt(s) && (s.Signed || s.SignedAt is not null))
                       ?? details.Signers.FirstOrDefault(MatchesExt);

                if (byExt is not null)
                    return byExt;
            }

            // 2. Se não casou por ExternalId (ex.: NeckSign não retorna externalId dos signatários),
            // correlaciona por e-mail (como em ApplyCreatedEnvelope e ZapSignService).
            if (!string.IsNullOrWhiteSpace(roleEmail))
            {
                var targetEmail = roleEmail.Trim();
                bool MatchesEmail(SignatureSigner s) =>
                    !string.IsNullOrWhiteSpace(s.Email) &&
                    string.Equals(s.Email.Trim(), targetEmail, StringComparison.OrdinalIgnoreCase);

                return details.Signers.FirstOrDefault(s => MatchesEmail(s) && (s.Signed || s.SignedAt is not null))
                       ?? details.Signers.FirstOrDefault(MatchesEmail);
            }

            return null;
        }

        private static SignatureSigner? FindSignerByExternalId(SignatureDocumentDetails details, Guid? roleExternalId) =>
            FindSignerForRole(details, roleExternalId, null);

        /// <inheritdoc />
        /// <remarks>
        /// Varredura e sincronização dos documentos pendentes (task 5.4). Seleciona, entre as quatro
        /// entidades, apenas aquelas com <c>DocumentStatus == InProgress</c> <b>e</b> <c>DocumentToken</c>
        /// não vazio — somente estas são consideradas "pendentes". <c>Complete</c>/<c>Declined</c>/<c>Expired</c>
        /// são estados terminais/concluídos e <b>não</b> são varridos (idempotência — Req 8.1, 8.6, 14.4).
        /// Para cada token, resolve um <see cref="ISignatureService"/> de um escopo próprio e chama
        /// <see cref="SyncDocument"/> (espelhando o padrão de escopo/paralelismo do <c>ZapSignService</c>),
        /// aplicando as transições <c>SIGNED</c>/<c>DECLINED</c>/<c>EXPIRED</c>. Uma falha por documento é
        /// isolada com <c>try/catch</c> e registrada em log, <b>não</b> interrompendo o job e preservando o
        /// documento afetado (Req 8.5). Para <c>report</c> que resolve em <c>DECLINED</c>/<c>EXPIRED</c>, o
        /// <c>DocumentStatus</c> terminal é aplicado ao documento inteiro, preservando os campos por papel
        /// já preenchidos (comportamento delegado a <see cref="SyncDocument"/> — Req 8.2/8.3/8.4).
        /// </remarks>
        public async Task SyncPendingDocuments()
        {
            // Req 8.1/14.4: pendentes = InProgress com DocumentToken não vazio, nas quatro entidades.
            // Terminais/concluídos (Complete/Declined/Expired) são ignorados por construção.
            // Filtra também por SignatureProvider = NeckSign: documentos legados do ZapSign (ainda
            // InProgress) não devem ser consultados contra o NeckSign — ele não os conhece, e enviá-los
            // em massa satura o rate limit da API (429), impedindo a sincronização dos documentos NeckSign.
            var tokens = await _appDbContext.PAEEs
                .Where(x => x.DocumentStatus == Status.InProgress && x.SignatureProvider == SignatureProvider.NeckSign && x.DocumentToken != null && x.DocumentToken != "")
                .Select(x => x.DocumentToken)
                .Concat(_appDbContext.PEIs
                    .Where(x => x.DocumentStatus == Status.InProgress && x.SignatureProvider == SignatureProvider.NeckSign && x.DocumentToken != null && x.DocumentToken != "")
                    .Select(x => x.DocumentToken))
                .Concat(_appDbContext.RegularTeacherEvaluationDocuments
                    .Where(x => x.DocumentStatus == Status.InProgress && x.SignatureProvider == SignatureProvider.NeckSign && x.DocumentToken != null && x.DocumentToken != "")
                    .Select(x => x.DocumentToken))
                .Concat(_appDbContext.Reports
                    .Where(x => x.DocumentStatus == Status.InProgress && x.SignatureProvider == SignatureProvider.NeckSign && x.DocumentToken != null && x.DocumentToken != "")
                    .Select(x => x.DocumentToken))
                .Distinct()
                .ToListAsync();

            await Parallel.ForEachAsync(tokens, new ParallelOptions
            {
                MaxDegreeOfParallelism = 2
            },
            async (token, _) =>
            {
                // Req 8.5: isolamento por documento — falha de um não interrompe o job e preserva o afetado.
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<ISignatureService>();
                    await service.SyncDocument(token!);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "SyncPendingDocuments (NeckSign): erro ao sincronizar um documento pendente; job prossegue com os demais.");
                }
            });
        }

        #region Contratos de serialização do NeckSign (internos ao serviço)

        /// <summary>
        /// Corpo da requisição <c>POST /api/v1/envelopes</c> do NeckSign (contrato <c>CreateEnvelope</c>).
        /// Serializado em camelCase. O campo <c>pdfBase64</c> (nome exigido pelo schema estrito do
        /// NeckSign) transporta o PDF gerado pelo sistema (Req 2.2).
        /// </summary>
        private sealed class NeckSignCreateEnvelopeRequest
        {
            public string ExternalId { get; init; } = default!;
            public string Title { get; init; } = default!;
            public string? WebhookUrl { get; init; }

            [JsonPropertyName("pdfBase64")]
            public string Base64Pdf { get; init; } = default!;

            public List<NeckSignCreateEnvelopeSigner> Signers { get; init; } = new();
        }

        /// <summary>Signatário no corpo do <c>POST /api/v1/envelopes</c> (name/email/role/order + externalId de correlação).</summary>
        private sealed class NeckSignCreateEnvelopeSigner
        {
            public string Name { get; init; } = default!;
            public string Email { get; init; } = default!;
            public NeckSignSignerRole Role { get; init; }
            public int? Order { get; init; }
            public string? ExternalId { get; init; }
        }

        /// <summary>
        /// Resposta <c>201 Created</c> de <c>POST /api/v1/envelopes</c>. Contém o <c>envelopeId</c> e a
        /// <c>signingUrl</c> exigidos para persistência (Req 2.8), além da lista opcional de signatários
        /// com <c>signingUrl</c> por papel para correlação em <c>report</c> (Req 2.9).
        /// </summary>
        internal sealed class NeckSignCreateEnvelopeResponse
        {
            public string? EnvelopeId { get; init; }
            public string? ExternalId { get; init; }
            public string? Status { get; init; }
            public string? SigningUrl { get; init; }
            public string? SignedFile { get; init; }
            public List<NeckSignCreateEnvelopeResponseSigner>? Signers { get; init; }
        }

        /// <summary>Signatário retornado no <c>201</c>, usado para correlacionar <c>signingUrl</c> por <c>externalId</c>.</summary>
        internal sealed class NeckSignCreateEnvelopeResponseSigner
        {
            public string? ExternalId { get; init; }
            public string? Email { get; init; }
            public string? Role { get; init; }
            public string? SigningUrl { get; init; }
        }

        /// <summary>
        /// Resposta de <c>GET /api/v1/envelopes/:id</c>. O <c>status</c> é o estado textual do envelope
        /// (<c>PENDING</c>/<c>SIGNING</c>/<c>SIGNED</c>/<c>DECLINED</c>/<c>EXPIRED</c>/<c>VOIDED</c>),
        /// mapeado para o enum neutro por <see cref="MapEnvelopeStatus"/>.
        /// </summary>
        private sealed class NeckSignEnvelopeResponse
        {
            public string? EnvelopeId { get; init; }
            public string? ExternalId { get; init; }
            public string? Status { get; init; }
            public string? SignedFileUrl { get; init; }
            public List<NeckSignEnvelopeSigner>? Signers { get; init; }
        }

        /// <summary>
        /// Resposta de <c>GET /api/v1/envelopes/:id/download</c> do NeckSign: a URL pré-assinada do PDF
        /// assinado (<c>downloadUrl</c>) e o TTL em segundos (<c>expiresIn</c>). Consumida por
        /// <see cref="TryGetSignedDownloadUrlAsync"/> para preencher <c>SignedFileUrl</c>.
        /// </summary>
        private sealed class NeckSignDownloadResponse
        {
            public string? DownloadUrl { get; init; }
            public int ExpiresIn { get; init; }
        }

        /// <summary>Signatário retornado por <c>GET /api/v1/envelopes/:id</c>.</summary>
        private sealed class NeckSignEnvelopeSigner
        {
            public string? ExternalId { get; init; }
            public string? Name { get; init; }
            public string? Email { get; init; }
            public string? Status { get; init; }
            public DateTimeOffset? SignedAt { get; init; }
        }

        #endregion
    }
}
