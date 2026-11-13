using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SingularSystem.Authorization;
using SingularSystem.Domain.Plans;
using SingularSystem.Domain.Plans.Enums;
using SingularSystem.Domain.Tenancy;
using SingularSystem.Models.Entities;
using SingularSystem.Services;

namespace SingularSystem.Middleware
{
    public class TenantResolutionMiddleware
    {
        private readonly RequestDelegate _next;

        public TenantResolutionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext,
            IPlanSubscriptionRepository repo, UserManager<ApplicationUser> userManager)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!string.IsNullOrEmpty(userId) && tenantContext is TenantContextService svc)
            {
                var claimTenant = context.User.FindFirstValue(TenantClaimTypes.TenantId);
                string? userTenantId = claimTenant is not null
                    ? (claimTenant.Length > 0 ? claimTenant : null)
                    : await userManager.Users.AsNoTracking()
                          .Where(u => u.Id == userId).Select(u => u.TenantId).FirstOrDefaultAsync();

                svc.TenantId = userTenantId ?? userId;

                var subscription = await repo.GetCurrentAsync(svc.TenantId);
                svc.Subscription = subscription;
                svc.IsPrivatePlan = subscription != null &&
                                    subscription.PlanType != PlanType.Publico;
            }

            await _next(context);
        }
    }
}
