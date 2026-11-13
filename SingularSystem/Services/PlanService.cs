using Microsoft.EntityFrameworkCore;
using SingularSystem.Domain.Plans;
using SingularSystem.Models.Entities;

namespace SingularSystem.Services
{
    public class PlanService : IPlanService
    {
        private readonly IPlanSubscriptionRepository _repo;
        private readonly AppDbContext _db;

        public PlanService(IPlanSubscriptionRepository repo, AppDbContext db)
        {
            _repo = repo;
            _db = db;
        }

        public async Task<PlanSubscription?> GetActiveSubscriptionAsync(string tenantId)
            => await _repo.GetActiveAsync(tenantId);

        public async Task<QuotaResult> CheckStudentQuotaAsync(string tenantId)
        {
            var subscription = await _repo.GetActiveAsync(tenantId);

            if (subscription == null)
                return new QuotaResult(true, 0, 0); // plano Público — sem cota

            if (subscription.QuotaDocuments == 0)
                return new QuotaResult(true, 0, 0); // ilimitado

            var used = await _db.Students
                .CountAsync(s => s.TenantId == tenantId && s.IsActive);

            return new QuotaResult(used < subscription.QuotaDocuments, used, subscription.QuotaDocuments);
        }
    }
}
