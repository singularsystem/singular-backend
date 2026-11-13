using SingularSystem.DTOs;
using SingularSystem.Interfaces;
using SingularSystem.Mappers;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace SingularSystem.Controllers
{
    /// <summary>
    /// Endpoint agnóstico ao provedor de assinatura para visualização/download do PDF assinado.
    /// Expõe <c>GET /api/v1/documents/{documentToken}/signed-url</c>, retornando um contrato estável
    /// (<see cref="SignedUrlResponse"/>) com um único campo de URL (<c>signedFileUrl</c>),
    /// independentemente de o documento ter sido originado no ZapSign ou no NeckSign.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Classe dedicada (separada do <c>PDFController</c>, que também usa a rota base
    /// <c>api/v1/documents</c>) para isolar o recurso de assinatura. A rota final
    /// <c>api/v1/documents/{documentToken}/signed-url</c> não colide com as rotas de PDF existentes
    /// (<c>{documentId}/pdf</c>, <c>{documentId}/download</c>).
    /// </para>
    /// <para>
    /// Thin controller: não conhece provedores. Resolve <see cref="ISignatureService"/> por DI
    /// (a feature flag <c>SignatureProvider</c> da spec 012 decide a implementação injetada) e aplica
    /// a precedência do fallback S3 próprio via <see cref="ISignedMediaResolver"/>.
    /// </para>
    /// </remarks>
    [ApiController]
    [Route("api/v1/documents")]
    [Authorize]
    public sealed class DocumentsSignatureController : BaseController
    {
        private readonly ISignatureService _signatureService;
        private readonly ISignedMediaResolver _signedMedia;
        private readonly AppDbContext _appDbContext;
        private readonly ILogger<DocumentsSignatureController> _logger;

        public DocumentsSignatureController(
            ISignatureService signatureService,
            ISignedMediaResolver signedMedia,
            AppDbContext appDbContext,
            ILogger<DocumentsSignatureController> logger)
        {
            _signatureService = signatureService;
            _signedMedia = signedMedia;
            _appDbContext = appDbContext;
            _logger = logger;
        }

        /// <summary>
        /// Retorna a URL do PDF assinado de forma agnóstica ao provedor.
        /// </summary>
        /// <param name="documentToken">
        /// Token/identificador do documento no provedor (envelopeId no NeckSign; token no ZapSign).
        /// </param>
        /// <returns>
        /// <c>400</c> quando o token é nulo/vazio/apenas espaços; caso contrário, a URL do documento
        /// assinado no envelope <see cref="ApiResponse{T}"/> (ramos completos em tasks 3.2–3.5).
        /// </returns>
        [HttpGet("{documentToken}/signed-url")]
        public async Task<IActionResult> GetSignedUrl(string documentToken)
        {
            // Req 1.5: token nulo/vazio/apenas espaços → 400 neutro, sem invocar o provedor.
            if (string.IsNullOrWhiteSpace(documentToken))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "O identificador do documento é obrigatório."
                });
            }

            // task 3.5: Req 5.1 — toda a resolução (lookup + presign S3 próprio + consulta ao
            //   provedor) roda dentro de um try/catch. Em qualquer exceção (falha de banco no
            //   lookup, falha na geração da presigned URL do S3 próprio, exceção em
            //   GetDocumentDetails), logamos o erro COMPLETO via _logger.LogError (com o token
            //   mascarado) e respondemos uma mensagem neutra, SEM vazar mensagem interna da
            //   exceção, stack trace, token, segredo, endpoint do provedor, bucket ou chave bruta
            //   no corpo HTTP. A validação de entrada 400 (acima) permanece FORA do try para não
            //   ser engolida. Note que GetDocumentDetails retorna null (não lança) quando os
            //   detalhes estão indisponíveis — esse caso é o 404 abaixo, não o catch.
            try
            {
                // task 3.2: lookup da entidade por DocumentToken nas quatro DbSet
                //   (Reports, PEIs, PAEEs, RegularTeacherEvaluationDocuments) via _appDbContext,
                //   retornando tipo, SignedFile e DocumentStatus do primeiro match (null se nenhuma).
                //   As tasks 3.3/3.4 consomem este resultado para o fallback S3 próprio e o 404.
                DocumentLookupResult? lookup = await FindDocumentByTokenAsync(documentToken);

                var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId != null && lookup is not null)
                {
                    var userManager = HttpContext?.RequestServices?.GetService<UserManager<ApplicationUser>>();
                    var user = userManager != null ? await userManager.FindByIdAsync(userId) : null;
                    if (user == null) return Unauthorized();
                    var roles = await userManager.GetRolesAsync(user);

                    if (!roles.Contains("Admin"))
                    {
                        if (lookup.StudentId is null)
                        {
                            return StatusCode(StatusCodes.Status403Forbidden, DocumentAccessDenied.Body());
                        }

                        var student = await _appDbContext.Students.IgnoreQueryFilters().AsNoTracking()
                            .Include(s => s.School)
                            .FirstOrDefaultAsync(s => s.ID == lookup.StudentId.Value);

                        if (student == null || !HasStudentAccess(user, student, roles))
                        {
                            return StatusCode(StatusCodes.Status403Forbidden, DocumentAccessDenied.Body());
                        }
                    }
                }

                // task 3.3: fallback S3 próprio (precedência sobre o provedor, Req 4.1/4.2/4.4).
                //   Avaliado ANTES de qualquer consulta ao provedor: documentos legados migrados ao S3
                //   próprio devem ser servidos mesmo com o provedor indisponível. Quando a entidade
                //   existe, está completa (DocumentStatus == Complete) e seu SignedFile aponta para o
                //   S3 próprio (IsOwnS3Reference), geramos uma presigned URL (TTL 60 min) e respondemos
                //   200 SEM chamar GetDocumentDetails.
                if (lookup is not null
                    && lookup.Status == Status.Complete
                    && _signedMedia.IsOwnS3Reference(lookup.SignedFile))
                {
                    // SignedFile não é nulo/vazio aqui: IsOwnS3Reference só retorna true para uma
                    // referência de S3 próprio válida. O '!' documenta essa garantia ao compilador.
                    string presignedUrl = _signedMedia.GeneratePreSignedUrl(lookup.SignedFile!, 60);

                    return Ok(new ApiResponse<SignedUrlResponse>
                    {
                        Success = true,
                        Data = new SignedUrlResponse
                        {
                            // Documento completo e migrado → status neutro "Signed" (design.md:
                            // o ramo S3 próprio não consulta o provedor, logo não há
                            // SignatureDocumentStatus para mapear via SignedUrlStatusMapper).
                            Status = SignedUrlStatusMapper.ToNeutralStatus(SignatureDocumentStatus.Signed),
                            SignedFileUrl = presignedUrl
                        }
                    });
                }

                // task 3.4: caminho do provedor (quando não há fallback S3 próprio aplicável).
                //   Para o legado-ZapSign ainda não migrado (IsZapSignUrl(SignedFile) e provedor
                //   responde), a URL também vem daqui — preservando o comportamento atual.
                SignatureDocumentDetails? details =
                    await _signatureService.GetDocumentDetails(documentToken, bypassCooldown: true);

                // Req 3.2/4.6: provedor sem detalhes E sem entidade S3-própria servível → 404 neutro.
                //   (O ramo S3 próprio já teria respondido 200 antes de chegar aqui; se chegamos aqui,
                //   não há fallback migrado disponível para este token.)
                if (details is null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Documento não encontrado."
                    });
                }

                // Req 2.1: documento assinado → 200 com signedFileUrl (= SignedFileUrl do provedor) e
                //   signers mascarados (SignedUrlSignerMapper.ToResponseList, task 2.2).
                if (details.Status == SignatureDocumentStatus.Signed && !string.IsNullOrEmpty(details.SignedFileUrl))
                {
                    return Ok(new ApiResponse<SignedUrlResponse>
                    {
                        Success = true,
                        Data = new SignedUrlResponse
                        {
                            Status = SignedUrlStatusMapper.ToNeutralStatus(details.Status),
                            SignedFileUrl = details.SignedFileUrl,
                            Signers = SignedUrlSignerMapper.ToResponseList(details.Signers)
                        }
                    });
                }

                // Req 3.1/3.3: documento ainda não assinado (status != Signed ou sem URL) → responder o
                //   status atual SEM signedFileUrl, com código distinto do 200 de assinado. Usamos
                //   409 Conflict: o recurso existe, mas não está no estado "assinado" — alinhado à
                //   recomendação do design (o legado usava 400, mas 409 expressa melhor o conflito de
                //   estado e é a convenção adotada para o novo endpoint). Req 3.1/3.4: registrar a
                //   ocorrência via ILogger com o documentToken mascarado (nunca o token completo).
                _logger.LogInformation(
                    "signed-url: documento ainda não assinado para o token {Token} (status {Status}). Nenhuma URL disponível.",
                    MaskToken(documentToken),
                    SignedUrlStatusMapper.ToNeutralStatus(details.Status));

                return Conflict(new ApiResponse<SignedUrlResponse>
                {
                    Success = false,
                    Message = "O documento ainda não foi assinado.",
                    Data = new SignedUrlResponse
                    {
                        Status = SignedUrlStatusMapper.ToNeutralStatus(details.Status),
                        Signers = SignedUrlSignerMapper.ToResponseList(details.Signers)
                    }
                });
            }
            catch (Exception ex)
            {
                // Req 5.1/5.3: logar o erro COMPLETO (ex contém mensagem + stack trace) com o token
                //   MASCARADO — nunca o token completo, nunca dados pessoais completos. O template
                //   estruturado só interpola o token mascarado; nenhum dado sensível é adicionado.
                _logger.LogError(
                    ex,
                    "signed-url: falha ao resolver a URL do documento assinado para o token {Token}.",
                    MaskToken(documentToken));

                // Req 5.1/5.2: resposta NEUTRA. Diferente de alguns handlers legados do sistema que
                //   anexam ex.Message, aqui NÃO expomos a mensagem interna, stack trace, token,
                //   segredo, endpoint do provedor, nome de bucket, chave de objeto, nem qual provedor
                //   (ZapSign/NeckSign) originou o documento. 500 segue a convenção predominante do
                //   projeto (StatusCode(500, ApiResponse { Success = false })).
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Ocorreu um erro ao obter o documento assinado. Tente novamente mais tarde."
                });
            }
        }

        /// <summary>
        /// Mascara um token para log (preserva prefixo e sufixo), evitando expor o valor completo
        /// (Req 3.4/5.3). Segue a mesma convenção do <c>ZapSignController.MaskToken</c>.
        /// </summary>
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
        /// Tipo de entidade de documento assinável correspondente ao <c>DocumentToken</c>.
        /// </summary>
        private enum DocumentType
        {
            Report,
            PEI,
            PAEE,
            RegularTeacherEvaluationDocument,
        }

        /// <summary>
        /// Resultado leve do lookup por <c>DocumentToken</c>: o tipo da entidade localizada,
        /// o campo <c>SignedFile</c> (URL/chave do PDF assinado, pode ser nulo) e o
        /// <c>DocumentStatus</c>. As tasks 3.3/3.4 usam <c>SignedFile</c> e <c>Status</c> para
        /// decidir entre o fallback S3 próprio e a consulta ao provedor.
        /// </summary>
        private sealed record DocumentLookupResult(DocumentType Type, string? SignedFile, Status Status, Guid? StudentId = null);

        /// <summary>
        /// Localiza a entidade de documento correspondente ao <paramref name="documentToken"/>
        /// consultando as quatro <c>DbSet</c> assináveis (<c>Reports</c>, <c>PEIs</c>, <c>PAEEs</c>,
        /// <c>RegularTeacherEvaluationDocuments</c>) por <c>DocumentToken == documentToken</c>,
        /// retornando o primeiro match (Req 4.5). Acesso direto via <see cref="AppDbContext"/>,
        /// sem camada de repositório, no padrão de dados da aplicação.
        /// </summary>
        /// <returns>
        /// O <see cref="DocumentLookupResult"/> do primeiro match encontrado, ou <c>null</c> se
        /// nenhuma das quatro entidades possuir o <c>DocumentToken</c> informado.
        /// </returns>
        private async Task<DocumentLookupResult?> FindDocumentByTokenAsync(string documentToken)
        {
            Report? report = await _appDbContext.Reports
                .FirstOrDefaultAsync(r => r.DocumentToken == documentToken);
            if (report is not null)
            {
                return new DocumentLookupResult(DocumentType.Report, report.SignedFile, report.DocumentStatus, report.StudentID);
            }

            PEI? pei = await _appDbContext.PEIs
                .FirstOrDefaultAsync(p => p.DocumentToken == documentToken);
            if (pei is not null)
            {
                return new DocumentLookupResult(DocumentType.PEI, pei.SignedFile, pei.DocumentStatus, pei.StudentID);
            }

            PAEE? paee = await _appDbContext.PAEEs
                .FirstOrDefaultAsync(p => p.DocumentToken == documentToken);
            if (paee is not null)
            {
                return new DocumentLookupResult(DocumentType.PAEE, paee.SignedFile, paee.DocumentStatus, paee.StudentID);
            }

            RegularTeacherEvaluationDocument? evaluation = await _appDbContext.RegularTeacherEvaluationDocuments
                .FirstOrDefaultAsync(d => d.DocumentToken == documentToken);
            if (evaluation is not null)
            {
                return new DocumentLookupResult(
                    DocumentType.RegularTeacherEvaluationDocument,
                    evaluation.SignedFile,
                    evaluation.DocumentStatus,
                    evaluation.StudentID);
            }

            return null;
        }
    }
}
