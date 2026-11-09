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

        public TermsController(AppDbContext db)
        {
            _db = db;
        }

        // GET /api/v1/terms/active?key=AEE_TERMO_RESPONSABILIDADE
        [HttpGet("active")]
        public async Task<ActionResult<TermResponse>> GetActive([FromQuery] string key = "AEE_TERMO_RESPONSABILIDADE")
        {
            var term = await _db.Set<Term>()
                .AsNoTracking()
                .Where(t => t.Key == key && t.IsActive)
                .OrderByDescending(t => t.PublishedAt)
                .FirstOrDefaultAsync();

            if (term is null) return NotFound();

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

            var term = await _db.Set<Term>()
                .Where(t => t.Key == key && t.IsActive)
                .OrderByDescending(t => t.PublishedAt)
                .FirstOrDefaultAsync();

            if (term is null) return NotFound(new { message = "Termo ativo não encontrado." });

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
