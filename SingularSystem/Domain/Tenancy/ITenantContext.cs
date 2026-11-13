using SingularSystem.Domain.Plans;

namespace SingularSystem.Domain.Tenancy
{
    public interface ITenantContext
    {
        string? TenantId { get; }
        bool IsPrivatePlan { get; }
        PlanSubscription? Subscription { get; }
    }
}
