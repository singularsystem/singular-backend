using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace SingularSystem.Authorization
{
    public class SelfOrRoleHandler
    : AuthorizationHandler<SelfOrRoleRequirement, string>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            SelfOrRoleRequirement requirement,
            string resourceUserId)
        {
            var currentUserId =
                context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(currentUserId))
                return Task.CompletedTask;

            // Self
            if (currentUserId == resourceUserId)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // Role-based
            if (requirement.AllowedRoles.Any(context.User.IsInRole))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
