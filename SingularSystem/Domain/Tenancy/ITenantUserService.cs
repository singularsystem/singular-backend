namespace SingularSystem.Domain.Tenancy
{
    public enum DeactivateUserResult
    {
        Deactivated,
        Denied
    }

    public record SchoolUserSummary(int Used, int Max);

    public record SchoolUserItem(
        string Id,
        string FullName,
        string Email,
        string Role,
        bool IsActive,
        DateTime? CreatedAt,
        DateTime? LastLogin
    );

    public record SchoolUsersResult(
        IReadOnlyList<SchoolUserItem> Items,
        SchoolUserSummary Summary,
        int Total
    );

    public interface ITenantUserService
    {
        Task<int> CountActiveAsync(string tenantId);
        Task<SchoolUsersResult> ListAsync(string tenantId, int page, int pageSize, string? searchTerm = null);
        Task<DeactivateUserResult> DeactivateAsync(string callerId, string tenantId, string targetId);
    }
}
