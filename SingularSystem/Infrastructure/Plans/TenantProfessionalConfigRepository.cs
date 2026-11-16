using Microsoft.EntityFrameworkCore;
using SingularSystem.Domain.Plans;
using SingularSystem.Models.Entities;

namespace SingularSystem.Infrastructure.Plans
{
    public class TenantProfessionalConfigRepository : ITenantProfessionalConfigRepository
    {
        private readonly AppDbContext _db;

        public TenantProfessionalConfigRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task<TenantProfessionalConfig?> GetByTenantIdAsync(string tenantId)
        {
            return await _db.TenantProfessionalConfigs
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.TenantId == tenantId);
        }

        public async Task UpsertAsync(TenantProfessionalConfig config)
        {
            var existing = await _db.TenantProfessionalConfigs.FirstOrDefaultAsync(c => c.TenantId == config.TenantId);
            if (existing == null)
            {
                if (config.Id == Guid.Empty) config.Id = Guid.NewGuid();
                await _db.TenantProfessionalConfigs.AddAsync(config);
            }
            else
            {
                existing.PsychologistEnabled = config.PsychologistEnabled;
                existing.PsychopedagogueEnabled = config.PsychopedagogueEnabled;
                existing.SocialWorkEnabled = config.SocialWorkEnabled;
                existing.UpdatedAt = config.UpdatedAt;
                existing.UpdatedBy = config.UpdatedBy;
            }
            await _db.SaveChangesAsync();
        }
    }
}
