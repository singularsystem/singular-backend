using SingularSystem.DTOs.NeckBook;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace SingularSystem.Controllers
{
    /// <summary>
    /// Controller administrativo para gerenciar as configurações de chave de API do NeckBook
    /// por município (<see cref="MunicipalityNeckBookConfig"/>) (Req 18.1–18.3).
    /// Exige o papel <c>Admin</c>, criptografa a chave via <see cref="ApiKeyProtector"/> antes de
    /// persistir e nunca retorna ou registra em log o valor da chave.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/v1/neckbook/admin/municipalities")]
    public class NeckBookAdminController : BaseController
    {
        private readonly AppDbContext _db;
        private readonly ApiKeyProtector _protector;
        private readonly ILogger<NeckBookAdminController> _logger;

        public NeckBookAdminController(
            AppDbContext db,
            ApiKeyProtector protector,
            ILogger<NeckBookAdminController> logger)
        {
            _db = db;
            _protector = protector;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> ListConfigs(CancellationToken ct = default)
        {
            var items = await _db.MunicipalityNeckBookConfigs
                .AsNoTracking()
                .OrderBy(c => c.State)
                .ThenBy(c => c.City)
                .Select(c => new MunicipalityNeckBookConfigResponse
                {
                    Id = c.Id,
                    State = c.State,
                    City = c.City,
                    IsActive = c.IsActive,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                })
                .ToListAsync(ct);

            return Ok(items);
        }

        [HttpPost]
        public async Task<IActionResult> UpsertConfig(
            [FromBody] UpsertMunicipalityNeckBookConfigRequest request,
            CancellationToken ct = default)
        {
            if (request is null ||
                string.IsNullOrWhiteSpace(request.State) ||
                string.IsNullOrWhiteSpace(request.City) ||
                string.IsNullOrWhiteSpace(request.ApiKey))
            {
                return BadRequest(new
                {
                    code = "INVALID_MUNICIPALITY_CONFIG",
                    message = "Os campos State, City e ApiKey são obrigatórios."
                });
            }

            var normalizedState = Helpers.NormalizeState(request.State);
            var normalizedCity = Helpers.NormalizeCity(request.City);
            var encryptedKey = _protector.Protect(request.ApiKey.Trim());

            var allConfigs = await _db.MunicipalityNeckBookConfigs.ToListAsync(ct);
            var existing = allConfigs.FirstOrDefault(c =>
                Helpers.StateMatches(c.State, normalizedState) &&
                Helpers.CityMatches(c.City, normalizedCity));

            if (existing is not null)
            {
                existing.State = normalizedState;
                existing.City = normalizedCity;
                existing.EncryptedApiKey = encryptedKey;
                existing.IsActive = request.IsActive;
                existing.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync(ct);

                _logger.LogInformation(
                    "Configuração NeckBook atualizada para o município {City}/{State} (Id={ConfigId}, IsActive={IsActive}).",
                    existing.City,
                    existing.State,
                    existing.Id,
                    existing.IsActive);

                return Ok(ToSafeResponse(existing));
            }

            var created = new MunicipalityNeckBookConfig
            {
                Id = Guid.NewGuid(),
                State = normalizedState,
                City = normalizedCity,
                EncryptedApiKey = encryptedKey,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _db.MunicipalityNeckBookConfigs.Add(created);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Configuração NeckBook criada para o município {City}/{State} (Id={ConfigId}, IsActive={IsActive}).",
                created.City,
                created.State,
                created.Id,
                created.IsActive);

            return CreatedAtAction(nameof(ListConfigs), new { id = created.Id }, ToSafeResponse(created));
        }

        private static MunicipalityNeckBookConfigResponse ToSafeResponse(MunicipalityNeckBookConfig entity) =>
            new()
            {
                Id = entity.Id,
                State = entity.State,
                City = entity.City,
                IsActive = entity.IsActive,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
    }
}
