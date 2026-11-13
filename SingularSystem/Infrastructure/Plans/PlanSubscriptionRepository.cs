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
    }
}
