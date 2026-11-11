using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using SingularSystem.DTOs.NeckBook;
using SingularSystem.Models.Entities;
using SingularSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace SingularSystem.Controllers
{
    /// <summary>
    /// Controller de proxy autenticado entre o Singular System e a API externa de geração de livros
    /// do NeckBook (<c>https://api.neckbook.com.br</c>) (Req 6–17).
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/v1/neckbook/books")]
    public class NeckBookBooksController : BaseController
    {
        /// <summary>
        /// Nome do <see cref="HttpClient"/> registrado em <c>Program.cs</c> via <see cref="IHttpClientFactory"/> (Req 3.1, 3.5).
        /// </summary>
        public const string NeckBookHttpClientName = "NeckBook";

        public const string MunicipalityNotEnabledCode = "MUNICIPALITY_NOT_ENABLED";
        public const string MunicipalityNotEnabledMessage = "Seu município não está habilitado para geração de livros";
        public const string NeckBookUnavailableCode = "NECKBOOK_UNAVAILABLE";
        public const string NeckBookUnavailableMessage = "O serviço de geração de livros (NeckBook) está temporariamente indisponível.";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _db;
        private readonly IMunicipalityNeckBookKeyResolver _keyResolver;
        private readonly NeckBookPayloadAssembler _payloadAssembler;
        private readonly ILogger<NeckBookBooksController> _logger;

        public NeckBookBooksController(
            IHttpClientFactory httpClientFactory,
            UserManager<ApplicationUser> userManager,
            AppDbContext db,
            IMunicipalityNeckBookKeyResolver keyResolver,
            NeckBookPayloadAssembler payloadAssembler,
            ILogger<NeckBookBooksController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _userManager = userManager;
            _db = db;
            _keyResolver = keyResolver;
            _payloadAssembler = payloadAssembler;
            _logger = logger;
        }

        /// <summary>
        /// Submete a geração de um livro no NeckBook para o aluno referenciado (Req 8).
        /// </summary>
        [HttpPost("generate")]
        public async Task<IActionResult> GenerateBook([FromBody] BookForm? form, CancellationToken ct = default)
        {
            var (authError, user, roles, apiKey) = await AuthorizeAndResolveMunicipalityKeyAsync(ct);
            if (authError is not null)
            {
                return authError;
            }

            if (form is null)
            {
                return UnprocessableEntity(new
                {
                    code = "CONTRACT_VALIDATION_ERROR",
                    message = "O corpo da requisição é obrigatório."
                });
            }

            var student = await _db.Students
                .Include(s => s.School)
                .FirstOrDefaultAsync(s => s.ID == form.StudentId, ct);

            if (student is null)
            {
                return NotFound(new
                {
                    code = "STUDENT_NOT_FOUND",
                    message = "Aluno não encontrado."
                });
            }

            // Req 7.1–7.4: reproduz exatamente a semântica de BaseController.HasStudentAccess
            if (!HasStudentAccess(user!, student, roles!))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    code = "STUDENT_ACCESS_DENIED",
                    message = "Você não possui permissão para gerar livros para este aluno."
                });
            }

            GenerationParams generationParams;
            try
            {
                generationParams = await _payloadAssembler.AssembleAsync(student, user!, form, ct);
            }
            catch (NeckBookContractValidationException ex)
            {
                return UnprocessableEntity(new
                {
                    code = ex.Code,
                    message = ex.Message
                });
            }

            var serializedBody = NeckBookPayloadAssembler.Serialize(generationParams);

            using var request = new HttpRequestMessage(HttpMethod.Post, "v1/books/generate")
            {
                Content = new StringContent(serializedBody, Encoding.UTF8, "application/json")
            };

            return await SendToNeckBookAsync(request, apiKey!, "generate", identifier: null, ct);
        }

        /// <summary>
        /// Consulta o status de um job de geração no NeckBook (Req 9).
        /// </summary>
        [HttpGet("jobs/{jobId}")]
        public async Task<IActionResult> GetJobStatus([FromRoute] string jobId, CancellationToken ct = default)
        {
            var (authError, _, _, apiKey) = await AuthorizeAndResolveMunicipalityKeyAsync(ct);
            if (authError is not null)
            {
                return authError;
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, $"v1/books/jobs/{Uri.EscapeDataString(jobId)}");
            return await SendToNeckBookAsync(request, apiKey!, "get_job", jobId, ct);
        }

        /// <summary>
        /// Obtém o rascunho de um livro no NeckBook para revisão (Req 10).
        /// </summary>
        [HttpGet("{bookId}/draft")]
        public async Task<IActionResult> GetDraft([FromRoute] string bookId, CancellationToken ct = default)
        {
            var (authError, _, _, apiKey) = await AuthorizeAndResolveMunicipalityKeyAsync(ct);
            if (authError is not null)
            {
                return authError;
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, $"v1/books/{Uri.EscapeDataString(bookId)}/draft");
            return await SendToNeckBookAsync(request, apiKey!, "get_draft", bookId, ct);
        }

        /// <summary>
        /// Atualiza o rascunho de um livro no NeckBook encaminhando o corpo recebido (Req 11).
        /// </summary>
        [HttpPut("{bookId}/draft")]
        public async Task<IActionResult> UpdateDraft(
            [FromRoute] string bookId,
            [FromBody] JsonElement draftBody,
            CancellationToken ct = default)
        {
            var (authError, _, _, apiKey) = await AuthorizeAndResolveMunicipalityKeyAsync(ct);
            if (authError is not null)
            {
                return authError;
            }

            var rawBody = draftBody.ValueKind == JsonValueKind.Undefined
                ? "{}"
                : draftBody.GetRawText();

            using var request = new HttpRequestMessage(HttpMethod.Put, $"v1/books/{Uri.EscapeDataString(bookId)}/draft")
            {
                Content = new StringContent(rawBody, Encoding.UTF8, "application/json")
            };

            return await SendToNeckBookAsync(request, apiKey!, "put_draft", bookId, ct);
        }

        /// <summary>
        /// Aprova o rascunho de um livro no NeckBook, injetando <c>reviewer_id</c> e <c>reviewer_role</c> (Req 12).
        /// </summary>
        [HttpPost("{bookId}/approve")]
        public async Task<IActionResult> ApproveBook(
            [FromRoute] string bookId,
            [FromBody] JsonElement? approveBody = null,
            CancellationToken ct = default)
        {
            var (authError, user, roles, apiKey) = await AuthorizeAndResolveMunicipalityKeyAsync(ct);
            if (authError is not null)
            {
                return authError;
            }

            JsonObject payloadObj;
            if (approveBody.HasValue && approveBody.Value.ValueKind == JsonValueKind.Object)
            {
                payloadObj = JsonNode.Parse(approveBody.Value.GetRawText()) as JsonObject ?? new JsonObject();
            }
            else
            {
                payloadObj = new JsonObject();
            }

            var reviewerRole = roles!.Contains("Admin") ? "Admin" : "Teacher";
            payloadObj["reviewer_id"] = user!.Id;
            payloadObj["reviewer_role"] = reviewerRole;

            using var request = new HttpRequestMessage(HttpMethod.Post, $"v1/books/{Uri.EscapeDataString(bookId)}/approve")
            {
                Content = new StringContent(payloadObj.ToJsonString(), Encoding.UTF8, "application/json")
            };

            return await SendToNeckBookAsync(request, apiKey!, "approve", bookId, ct);
        }

        private async Task<(IActionResult? ErrorResult, ApplicationUser? User, IList<string>? Roles, string? ApiKey)>
            AuthorizeAndResolveMunicipalityKeyAsync(CancellationToken ct)
        {
            string userId;
            try
            {
                userId = CurrentUserId;
            }
            catch (UnauthorizedAccessException)
            {
                return (Unauthorized(), null, null, null);
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                return (Unauthorized(), null, null, null);
            }

            var user = await _userManager.FindByIdAsync(userId)
                       ?? await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);

            if (user is null)
            {
                return (Unauthorized(), null, null, null);
            }

            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains("Teacher") && !roles.Contains("Admin"))
            {
                return (StatusCode(StatusCodes.Status403Forbidden, new
                {
                    code = "FORBIDDEN_ROLE",
                    message = "Acesso restrito a usuários com papel Teacher ou Admin."
                }), null, null, null);
            }

            var apiKey = await _keyResolver.ResolveApiKeyAsync(user.State, user.City, ct);
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return (StatusCode(StatusCodes.Status403Forbidden, new
                {
                    code = MunicipalityNotEnabledCode,
                    message = MunicipalityNotEnabledMessage
                }), null, null, null);
            }

            return (null, user, roles, apiKey);
        }

        private async Task<IActionResult> SendToNeckBookAsync(
            HttpRequestMessage request,
            string apiKey,
            string operation,
            string? identifier,
            CancellationToken ct)
        {
            request.Headers.Remove("Authorization");
            request.Headers.TryAddWithoutValidation("Authorization", $"ApiKey {apiKey}");

            var client = _httpClientFactory.CreateClient(NeckBookHttpClientName);

            HttpResponseMessage response;
            try
            {
                response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            }
            catch (Exception ex) when (ex is TaskCanceledException or HttpRequestException)
            {
                // Req 16.8 / 16.9 / 17.1–17.3: loga apenas operação e identificador não sensível (sem chave e sem PII)
                _logger.LogWarning(
                    "Falha de comunicação com NeckBook na operação {Operation} (Identifier={Identifier}): indisponibilidade ou timeout.",
                    operation,
                    identifier ?? "n/a");

                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    code = NeckBookUnavailableCode,
                    message = NeckBookUnavailableMessage
                });
            }

            using (response)
            {
                var bodyBytes = await response.Content.ReadAsByteArrayAsync(ct);
                var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/json; charset=utf-8";

                // Req 17.3: registra apenas identificadores não sensíveis (nunca chave de API nem PII)
                _logger.LogInformation(
                    "NeckBook respondeu à operação {Operation} (Identifier={Identifier}) com status {StatusCode}.",
                    operation,
                    identifier ?? "n/a",
                    (int)response.StatusCode);

                if (HttpContext?.Response is not null)
                {
                    HttpContext.Response.StatusCode = (int)response.StatusCode;
                }

                return new NeckBookPassthroughResult(
                    (int)response.StatusCode,
                    bodyBytes,
                    contentType);
            }
        }
    }

    /// <summary>
    /// Resultado HTTP que preserva fielmente o <c>StatusCode</c>, os bytes brutos do corpo
    /// (sem desserialização ou re-serialização) e o <c>Content-Type</c> retornados pelo NeckBook (Req 16.1–16.7).
    /// </summary>
    public sealed class NeckBookPassthroughResult : ContentResult
    {
        public byte[] RawBytes { get; }

        public NeckBookPassthroughResult(int statusCode, byte[] rawBytes, string contentType)
        {
            RawBytes = rawBytes ?? Array.Empty<byte>();
            StatusCode = statusCode;
            ContentType = contentType;
            Content = Encoding.UTF8.GetString(RawBytes);
        }

        public override async Task ExecuteResultAsync(ActionContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            var response = context.HttpContext.Response;
            response.StatusCode = StatusCode ?? StatusCodes.Status200OK;
            if (!string.IsNullOrWhiteSpace(ContentType))
            {
                response.ContentType = ContentType;
            }
            if (RawBytes.Length > 0)
            {
                await response.Body.WriteAsync(RawBytes, context.HttpContext.RequestAborted);
            }
        }
    }
}
