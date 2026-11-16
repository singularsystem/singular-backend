using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SingularSystem.Domain.Plans;
using SingularSystem.Domain.Tenancy;
using SingularSystem.Models.Entities;

namespace SingularSystem.Services
{
    public class TenantUserService : ITenantUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPlanService _planService;

        public TenantUserService(
            UserManager<ApplicationUser> userManager,
            IPlanService planService)
        {
            _userManager = userManager;
            _planService = planService;
        }

        public async Task<int> CountActiveAsync(string tenantId)
        {
            return await _userManager.Users.CountAsync(u => u.TenantId == tenantId && u.IsActive && u.Id != tenantId);
        }

        public async Task<SchoolUsersResult> ListAsync(string tenantId, int page, int pageSize, string? searchTerm = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var used = await CountActiveAsync(tenantId);
            var subscription = await _planService.GetCurrentSubscriptionAsync(tenantId);
            var max = subscription?.MaxUsers ?? 0;

            var query = _userManager.Users.Where(u => u.TenantId == tenantId && u.IsActive && u.Id != tenantId);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                query = query.Where(u =>
                    (u.FullName != null && EF.Functions.Like(u.FullName, $"%{term}%")) ||
                    (u.Email != null && EF.Functions.Like(u.Email, $"%{term}%")));
            }

            var total = await query.CountAsync();
            var pagedUsers = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = new List<SchoolUserItem>(pagedUsers.Count);
            foreach (var user in pagedUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var role = roles.FirstOrDefault() ?? string.Empty;
                items.Add(new SchoolUserItem(
                    user.Id,
                    user.FullName,
                    user.Email ?? string.Empty,
                    role,
                    user.IsActive,
                    user.CreatedAt,
                    user.LastLogin
                ));
            }

            return new SchoolUsersResult(
                items,
                new SchoolUserSummary(used, max),
                total
            );
        }

        public async Task<DeactivateUserResult> DeactivateAsync(string callerId, string tenantId, string targetId)
        {
            if (callerId == targetId)
                return DeactivateUserResult.Denied;

            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == targetId && u.TenantId == tenantId && u.Id != tenantId);
            if (user == null || !user.IsActive)
                return DeactivateUserResult.Denied;

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            var result = await _userManager.UpdateAsync(user);

            return result.Succeeded ? DeactivateUserResult.Deactivated : DeactivateUserResult.Denied;
        }
    }
}
