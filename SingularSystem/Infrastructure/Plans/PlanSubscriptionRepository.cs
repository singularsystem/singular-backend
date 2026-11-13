using Microsoft.EntityFrameworkCore;
using SingularSystem.Domain.Plans;
using SingularSystem.Domain.Plans.Enums;
using SingularSystem.Models.Entities;

namespace SingularSystem.Infrastructure.Plans
{
    public class PlanSubscriptionRepository : IPlanSubscriptionRepository
    {
        private readonly AppDbContext _db;

        public PlanSubscriptionRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task<PlanSubscription?> GetActiveAsync(string tenantId)
        {
            var entity = await _db.PlanSubscriptions
                .FirstOrDefaultAsync(s =>
                    s.TenantId == tenantId &&
                    s.Status == SubscriptionStatus.Active);

            return entity; // AppDbContext.PlanSubscriptions retorna a mesma classe de domínio
        }

        public async Task<PlanSubscription?> GetCurrentAsync(string tenantId)
        {
            // Prioriza Active; senão a mais recente em qualquer status (PastDue, Suspended, Cancelled).
            return await _db.PlanSubscriptions
                .AsNoTracking()
                .Where(s => s.TenantId == tenantId)
                .OrderBy(s => s.Status == SubscriptionStatus.Active ? 0 : 1)
                .ThenByDescending(s => s.StartDate)
                .FirstOrDefaultAsync();
        }
    }
}
