using System.Security.Claims;
using SingularSystem.Domain.Plans;
using SingularSystem.Domain.Plans.Enums;
using SingularSystem.Domain.Tenancy;
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
            IPlanSubscriptionRepository repo)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!string.IsNullOrEmpty(userId) && tenantContext is TenantContextService svc)
            {
                svc.TenantId = userId;
                var subscription = await repo.GetActiveAsync(userId);
                svc.IsPrivatePlan = subscription != null &&
                                    subscription.PlanType != PlanType.Publico;
            }

            await _next(context);
        }
    }
}
