namespace SingularSystem.Domain.Tenancy
{
    public record TenantResolutionResult(string? TenantId, bool IsPrivatePlan);
}
