namespace SingularSystem.Domain.Plans
{
    public interface IPlanSubscriptionRepository
    {
        Task<PlanSubscription?> GetActiveAsync(string tenantId);
    }
}
