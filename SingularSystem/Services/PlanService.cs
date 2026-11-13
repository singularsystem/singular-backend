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

        public async Task<PlanSubscription?> GetCurrentSubscriptionAsync(string tenantId)
            => await _repo.GetCurrentAsync(tenantId);

        public Task<QuotaResult> CheckStudentQuotaAsync(string tenantId) => CheckStudentQuotaAsync(tenantId, 1);

        public async Task<QuotaResult> CheckStudentQuotaAsync(string tenantId, int additional)
        {
            var subscription = await _repo.GetCurrentAsync(tenantId);

            if (subscription == null || subscription.QuotaDocuments == 0)
                return new QuotaResult(true, 0, 0); // plano Público ou ilimitado

            var used = await _db.Students
                .CountAsync(s => s.TenantId == tenantId && s.IsActive);

            var allowed = used + additional <= subscription.QuotaDocuments;
            return new QuotaResult(allowed, used, subscription.QuotaDocuments);
        }
    }
}
