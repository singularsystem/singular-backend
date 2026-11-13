using SingularSystem.Domain.Plans;
using SingularSystem.Domain.Tenancy;

namespace SingularSystem.Services
{
    public class TenantContextService : ITenantContext
    {
        public string? TenantId { get; set; }
        public bool IsPrivatePlan { get; set; }
        public PlanSubscription? Subscription { get; set; }
    }
}
