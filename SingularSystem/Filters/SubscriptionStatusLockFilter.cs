using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SingularSystem.Authorization;
using SingularSystem.Domain.Tenancy;
using SingularSystem.Models;

namespace SingularSystem.Filters
{
    public class SubscriptionStatusLockFilter : IAsyncActionFilter
    {
        private readonly ITenantContext _tenantContext;

        public SubscriptionStatusLockFilter(ITenantContext tenantContext)
        {
            _tenantContext = tenantContext;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // Sem assinatura (Público legado ou anônimo): trava não se aplica (Req 10.3).
            // O critério é a existência de assinatura, e não IsPrivatePlan: assinaturas do tipo Publico
            // provisionadas via webhook também são travadas quando PastDue/Suspended/Cancelled.
            if (_tenantContext.Subscription is null)
            {
                await next();
                return;
            }

            var template = context.ActionDescriptor.AttributeRouteInfo?.Template?.Trim('/');
            if (template is not null && SubscriptionLockOptions.ExemptRouteTemplates.Contains(template))
            {
                await next();
                return;
            }

            var denial = SubscriptionStatusRules.Evaluate(
                _tenantContext.Subscription.Status, context.HttpContext.Request.Method);

            if (denial is not null)
            {
                context.Result = new ObjectResult(new ApiResponse<object> { Success = false, Message = denial })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;
            }

            await next();
        }
    }
}
