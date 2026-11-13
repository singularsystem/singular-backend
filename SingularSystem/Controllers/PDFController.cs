using SingularSystem.Authorization;
using SingularSystem.DTOs.Students;
using SingularSystem.Interfaces;
using SingularSystem.Models.Entities;
using SingularSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using SingularSystem.Models;

namespace SingularSystem.Controllers
{
    [Authorize]
    [Route("api/v1/documents")]
    [ApiController]
    public class PDFController : BaseController
    {
        private readonly ILogger<PDFController> _logger;
        private readonly AppDbContext _appDbContext;
        private readonly NotificationService _notificationService;
        private readonly IPDFService _pdfService;
        private readonly UserManager<ApplicationUser> _userManager;

        public PDFController(ILogger<PDFController> logger, AppDbContext appDbContext, NotificationService notificationService, IPDFService pDFService, UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _appDbContext = appDbContext;
            _notificationService = notificationService;
            _pdfService = pDFService;
            _userManager = userManager;
        }

        private static string SanitizeFileName(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '-');

            return name.Replace(' ', '-');
        }

        [HttpGet("{documentId}/pdf")]
        public async Task<IActionResult> ViewDocumentPdf(Guid documentId, [FromQuery] string? profile = null)
        {
            try
            {
                var parseResult = PdfProfileParser.Parse(profile);
                if (!parseResult.IsSuccess)
                {
                    return BadRequest(parseResult.ErrorMessage);
                }

                var bytes = await _pdfService.GeneratePdf(documentId, parseResult.Profile);

                Response.Headers["Content-Disposition"] = $"inline; filename=\"documento-{documentId}.pdf\"";

                return File(bytes, "application/pdf");
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (NotSupportedException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar PDF do documento {DocumentId}", documentId);
                return StatusCode(500, "Erro interno ao gerar PDF.");
            }
        }

        [HttpGet("{documentId}/download")]
        public async Task<IActionResult> DownloadDocument(Guid documentId, [FromQuery] string? profile = null)
        {
            try
            {
                var parseResult = PdfProfileParser.Parse(profile);
                if (!parseResult.IsSuccess)
                {
                    return BadRequest(parseResult.ErrorMessage);
                }

                var bytes = await _pdfService.GeneratePdf(documentId, parseResult.Profile);
                var type = await _pdfService.GetDocumentType(documentId);

                var safeType = SanitizeFileName(type ?? "documento");
                var filename = $"{safeType}-{documentId}.pdf";

                return File(bytes, "application/pdf", filename);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (NotSupportedException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar PDF do documento {DocumentId}", documentId);
                return StatusCode(500, "Erro interno ao gerar PDF.");
            }
        }

        [HttpGet("students/pdf")]
        public async Task<IActionResult> ViewStudentsList([FromQuery] string state = "", [FromQuery] string city = "", [FromQuery] string schoolID = "", [FromQuery] string serie = "")
        {
            try
            {
                var bytes = await _pdfService.GenerateStudentsPdf(state, city, schoolID, serie);

                Response.Headers["Content-Disposition"] = $"inline; filename=\"Lista_Estudantes_Singular.pdf\"";

                return File(bytes, "application/pdf");
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (NotSupportedException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar PDF de estudantes");
                return StatusCode(500, "Erro interno ao gerar PDF.");
            }
        }

        [HttpGet("students/download")]
        public async Task<IActionResult> DownloadStudentsList([FromQuery] string state = "", [FromQuery] string city = "", [FromQuery] string schoolID = "", [FromQuery] string serie = "")
        {
            try
            {
                var bytes = await _pdfService.GenerateStudentsPdf(state, city, schoolID, serie);

                var filename = $"Lista_Estudantes_Singular.pdf";

                return File(bytes, "application/pdf", filename);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (NotSupportedException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao baixar PDF de estudantes");
                return StatusCode(500, "Erro interno ao gerar PDF.");
            }
        }

        [HttpGet("reports-aee/pdf")]
        public async Task<IActionResult> ViewReportsAEEList([FromQuery] string state = "", [FromQuery] string city = "", [FromQuery] string schoolID = "", [FromQuery] string disability = "", [FromQuery] string disorder = "")
        {
            try
            {
                var bytes = await _pdfService.GenerateReportsAEEPdf(state, city, schoolID, disability, disorder);

                Response.Headers["Content-Disposition"] = $"inline; filename=\"Relatorio_Geral_Singular.pdf\"";

                return File(bytes, "application/pdf");
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (NotSupportedException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar PDF de relatórios AEE");
                return StatusCode(500, "Erro interno ao gerar PDF.");
            }
        }

        [HttpGet("reports-aee/download")]
        public async Task<IActionResult> DownloadReportsAEEList([FromQuery] string state = "", [FromQuery] string city = "", [FromQuery] string schoolID = "", [FromQuery] string disability = "", [FromQuery] string disorder = "")
        {
            try
            {
                var bytes = await _pdfService.GenerateReportsAEEPdf(state, city, schoolID, disability, disorder);

                var filename = $"Relatorio_Geral_Singular.pdf";

                return File(bytes, "application/pdf", filename);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (NotSupportedException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao baixar PDF de relatórios AEE");
                return StatusCode(500, "Erro interno ao gerar PDF.");
            }
        }

        [HttpGet("case-studies-tracking/pdf")]
        public async Task<IActionResult> ViewCaseStudiesTrackingList([FromQuery] string state = "", [FromQuery] string city = "", [FromQuery] string schoolID = "", [FromQuery] string searchTerm = "", [FromQuery] string status = "", [FromQuery] int? year = null, [FromQuery] int? month = null)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(CurrentUserId);
                if (user == null) return Unauthorized();

                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("Secretary"))
                {
                    state = user.State ?? "";
                    city = user.City ?? "";
                }

                var bytes = await _pdfService.GenerateCaseStudiesTrackingPdf(state, city, schoolID, searchTerm, status, year, month);

                Response.Headers["Content-Disposition"] = "inline; filename=\"Acompanhamento_Estudos_Caso.pdf\"";

                return File(bytes, "application/pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar PDF de acompanhamento de estudos de caso");
                return StatusCode(500, "Erro interno ao gerar PDF.");
            }
        }

        [HttpGet("case-studies-tracking/download")]
        public async Task<IActionResult> DownloadCaseStudiesTrackingList([FromQuery] string state = "", [FromQuery] string city = "", [FromQuery] string schoolID = "", [FromQuery] string searchTerm = "", [FromQuery] string status = "", [FromQuery] int? year = null, [FromQuery] int? month = null)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(CurrentUserId);
                if (user == null) return Unauthorized();

                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("Secretary"))
                {
                    state = user.State ?? "";
                    city = user.City ?? "";
                }

                var bytes = await _pdfService.GenerateCaseStudiesTrackingPdf(state, city, schoolID, searchTerm, status, year, month);

                var filename = "Acompanhamento_Estudos_Caso.pdf";

                return File(bytes, "application/pdf", filename);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao baixar PDF de acompanhamento de estudos de caso");
                return StatusCode(500, "Erro interno ao baixar PDF.");
            }
        }

        [PlanFeatureAuthorize("Empresa", "Publico")]
        [HttpGet("biopsychosocial-tracking/pdf")]
        public async Task<IActionResult> ViewBiopsychosocialTrackingList([FromQuery] string state = "", [FromQuery] string city = "", [FromQuery] string schoolID = "", [FromQuery] string searchTerm = "", [FromQuery] string status = "", [FromQuery] string pendingProfessional = "", [FromQuery] int? year = null, [FromQuery] int? month = null)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(CurrentUserId);
                if (user == null) return Unauthorized();

                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("Secretary"))
                {
                    state = user.State ?? "";
                    city = user.City ?? "";
                }

                var bytes = await _pdfService.GenerateBiopsychosocialTrackingPdf(state, city, schoolID, searchTerm, status, pendingProfessional, year, month);

                Response.Headers["Content-Disposition"] = "inline; filename=\"Acompanhamento_Biopsicossocial.pdf\"";

                return File(bytes, "application/pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar PDF de acompanhamento biopsicossocial");
                return StatusCode(500, "Erro interno ao gerar PDF.");
            }
        }

        [PlanFeatureAuthorize("Empresa", "Publico")]
        [HttpGet("biopsychosocial-tracking/download")]
        public async Task<IActionResult> DownloadBiopsychosocialTrackingList([FromQuery] string state = "", [FromQuery] string city = "", [FromQuery] string schoolID = "", [FromQuery] string searchTerm = "", [FromQuery] string status = "", [FromQuery] string pendingProfessional = "", [FromQuery] int? year = null, [FromQuery] int? month = null)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(CurrentUserId);
                if (user == null) return Unauthorized();

                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("Secretary"))
                {
                    state = user.State ?? "";
                    city = user.City ?? "";
                }

                var bytes = await _pdfService.GenerateBiopsychosocialTrackingPdf(state, city, schoolID, searchTerm, status, pendingProfessional, year, month);

                var filename = "Acompanhamento_Biopsicossocial.pdf";

                return File(bytes, "application/pdf", filename);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao baixar PDF de acompanhamento biopsicossocial");
                return StatusCode(500, "Erro interno ao baixar PDF.");
            }
        }
    }
}
