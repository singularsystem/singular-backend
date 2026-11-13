namespace SingularSystem.Domain.Plans
{
    public interface IPlanService
    {
        Task<PlanSubscription?> GetActiveSubscriptionAsync(string tenantId);
        Task<QuotaResult> CheckStudentQuotaAsync(string tenantId);
    }
}
