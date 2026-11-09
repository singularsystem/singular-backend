using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Security;
using Microsoft.EntityFrameworkCore;

namespace SingularSystem.Services
{
    /// <summary>
    /// Implementação de <see cref="IMunicipalityNeckBookKeyResolver"/> que resolve a
    /// <c>NeckBook_Api_Key</c> ativa do município do professor autenticado aplicando exatamente os
    /// critérios de correspondência de <see cref="Helpers.StateMatches"/> e
    /// <see cref="Helpers.CityMatches"/> (Req 5.1–5.6).
    /// </summary>
    public sealed class MunicipalityNeckBookKeyResolver : IMunicipalityNeckBookKeyResolver
    {
        private readonly AppDbContext _db;
        private readonly ApiKeyProtector _protector;

        public MunicipalityNeckBookKeyResolver(AppDbContext db, ApiKeyProtector protector)
        {
            _db = db;
            _protector = protector;
        }

        public async Task<string?> ResolveApiKeyAsync(string? state, string? city, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(state) || string.IsNullOrWhiteSpace(city))
            {
                return null;
            }

            var activeCandidates = await _db.MunicipalityNeckBookConfigs
                .Where(c => c.IsActive)
                .AsNoTracking()
                .ToListAsync(ct);

            var matches = activeCandidates
                .Where(c => Helpers.StateMatches(c.State, state) && Helpers.CityMatches(c.City, city))
                .ToList();

            if (matches.Count != 1)
            {
                return null;
            }

            return _protector.Unprotect(matches[0].EncryptedApiKey);
        }
    }
}
