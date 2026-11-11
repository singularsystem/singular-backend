using SingularSystem.DTOs.Term;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace SingularSystem.Controllers
{
    [ApiController]
    [Route("api/v1/terms")]
    public class TermsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _configuration;

        public TermsController(AppDbContext db, IConfiguration configuration)
        {
            _db = db;
            _configuration = configuration;
        }

        public static string LoadTermContent(IConfiguration config)
        {
            var version = config["Legal:TermVersion"] ?? "1";
            var filename = version == "2" ? "term.v2.txt" : "term.txt";
            var basePath = AppContext.BaseDirectory;
            var filePath = Path.Combine(basePath, "Resources", filename);
            if (!System.IO.File.Exists(filePath))
            {
                filePath = Path.Combine(Directory.GetCurrentDirectory(), "Resources", filename);
            }
            return System.IO.File.Exists(filePath) ? System.IO.File.ReadAllText(filePath) : string.Empty;
        }

        // GET /api/v1/terms/active?key=AEE_TERMO_RESPONSABILIDADE
        [HttpGet("active")]
        public async Task<ActionResult<TermResponse>> GetActive([FromQuery] string key = "AEE_TERMO_RESPONSABILIDADE")
        {
            var isV2 = _configuration["Legal:TermVersion"] == "2";

            var term = await _db.Set<Term>()
                .AsNoTracking()
                .Where(t => t.Key == key && t.IsActive)
                .OrderByDescending(t => t.PublishedAt)
                .FirstOrDefaultAsync();

            if (isV2)
            {
                var v2Content = LoadTermContent(_configuration);
                var contentHash = Hashing.Sha256Hex(v2Content);
                var id = term?.Id ?? Guid.Parse("00000000-0000-0000-0000-000000000002");
                var publishedAt = term?.PublishedAt ?? DateTime.UtcNow;
                return Ok(new TermResponse(
                    id, key, "2", "Termo de Responsabilidade e Uso — Singular System",
                    v2Content, "markdown", publishedAt, contentHash
                ));
            }

            if (term is null)
            {
                var v1Content = LoadTermContent(_configuration);
                if (!string.IsNullOrEmpty(v1Content))
                {
                    return Ok(new TermResponse(
                        Guid.Parse("00000000-0000-0000-0000-000000000001"),
                        key, "1", "Termo de Responsabilidade e Uso — Singular System",
                        v1Content, "markdown", DateTime.UtcNow, Hashing.Sha256Hex(v1Content)
                    ));
                }
                return NotFound();
            }

            return Ok(new TermResponse(
                term.Id, term.Key, term.Version, term.Title,
                term.Content, term.ContentFormat, term.PublishedAt, term.ContentHash
            ));
        }

        // GET /api/v1/terms/active/status?key=...
        [Authorize]
        [HttpGet("active/status")]
        public async Task<ActionResult<ActiveTermStatusResponse>> GetActiveStatus([FromQuery] string key = "AEE_TERMO_RESPONSABILIDADE")
        {
            var userId = GetUserId();

            var term = await _db.Set<Term>()
                .AsNoTracking()
                .Where(t => t.Key == key && t.IsActive)
                .OrderByDescending(t => t.PublishedAt)
                .FirstOrDefaultAsync();

            if (term is null)
                return Ok(new ActiveTermStatusResponse(false, null, null, null));

            var acceptance = await _db.Set<UserTermAcceptance>()
                .AsNoTracking()
                .Where(a => a.UserId == userId && a.TermId == term.Id)
                .FirstOrDefaultAsync();

            return Ok(new ActiveTermStatusResponse(
                HasAccepted: acceptance is not null,
                AcceptedAt: acceptance?.AcceptedAt,
                TermVersion: term.Version,
                TermId: term.Id
            ));
        }

        // POST /api/v1/terms/active/accept?key=...
        [Authorize]
        [HttpPost("active/accept")]
        public async Task<IActionResult> AcceptActive([FromQuery] string key = "AEE_TERMO_RESPONSABILIDADE", [FromBody] AcceptActiveTermRequest req = default!)
        {
            if (req is null || req.Accepted != true)
                return BadRequest(new { message = "Aceite obrigatório (Accepted=true)." });

            var userId = GetUserId();
            var isV2 = _configuration["Legal:TermVersion"] == "2";

            var term = await _db.Set<Term>()
                .Where(t => t.Key == key && t.IsActive)
                .OrderByDescending(t => t.PublishedAt)
                .FirstOrDefaultAsync();

            if (term is null)
            {
                var fallbackContent = LoadTermContent(_configuration);
                if (!string.IsNullOrEmpty(fallbackContent))
                {
                    term = new Term
                    {
                        Id = isV2 ? Guid.Parse("00000000-0000-0000-0000-000000000002") : Guid.Parse("00000000-0000-0000-0000-000000000001"),
                        Key = key,
                        Version = isV2 ? "2" : "1",
                        Title = "Termo de Responsabilidade e Uso — Singular System",
                        Content = fallbackContent,
                        ContentFormat = "markdown",
                        IsActive = true,
                        PublishedAt = DateTime.UtcNow,
                        ContentHash = Hashing.Sha256Hex(fallbackContent)
                    };
                    _db.Add(term);
                    await _db.SaveChangesAsync();
                }
                else
                {
                    return NotFound(new { message = "Termo ativo não encontrado." });
                }
            }

            // evita duplicar
            var exists = await _db.Set<UserTermAcceptance>()
                .AnyAsync(a => a.UserId == userId && a.TermId == term.Id);

            if (exists) return NoContent();

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var ua = Request.Headers.UserAgent.ToString();

            // Hash do aceite: versão + hash do conteúdo (ou conteúdo em si)
            var acceptanceHash = Hashing.Sha256Hex($"{term.Key}|{term.Version}|{term.ContentHash}");

            var acceptance = new UserTermAcceptance
            {
                UserId = userId,
                TermId = term.Id,
                AcceptedAt = DateTime.UtcNow,
                IpAddress = ip,
                UserAgent = string.IsNullOrWhiteSpace(ua) ? null : ua,
                AcceptanceHash = acceptanceHash,
                // Se quiser snapshot congelado preenchido, set aqui (opcional):
                // RenderedContentSnapshot = term.Content
            };

            _db.Add(acceptance);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        private Guid GetUserId()
        {
            // Ajuste conforme seu sistema (ClaimTypes.NameIdentifier, "sub", etc.)
            var idStr = User.FindFirst("sub")?.Value
                        ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(idStr) || !Guid.TryParse(idStr, out var userId))
                throw new UnauthorizedAccessException("UserId não encontrado no token.");

            return userId;
        }
    }

}
