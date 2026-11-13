using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SingularSystem.Domain.Plans;
using SingularSystem.Domain.Tenancy;
using SingularSystem.Models;

namespace SingularSystem.Authorization
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class PlanFeatureAuthorizeAttribute : ActionFilterAttribute
    {
        private readonly string[] _requiredPlans;

        public PlanFeatureAuthorizeAttribute(params string[] requiredPlans)
        {
            _requiredPlans = requiredPlans;
        }

        public override async Task OnActionExecutionAsync(
            ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var tenantContext = context.HttpContext.RequestServices.GetRequiredService<ITenantContext>();
            var planService = context.HttpContext.RequestServices.GetRequiredService<IPlanService>();

            // 1. Sem plano privado = Público legado (IsPrivatePlan == false)
            if (!tenantContext.IsPrivatePlan)
            {
                if (!_requiredPlans.Contains("Publico", StringComparer.OrdinalIgnoreCase))
                {
                    context.Result = new ObjectResult(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Recurso não disponível para o plano atual."
                    })
                    {
                        StatusCode = StatusCodes.Status403Forbidden
                    };
                    return;
                }

                await next();
                return;
            }

            // 2. Assinatura vigente (qualquer status)
            var subscription = tenantContext.Subscription
                ?? await planService.GetCurrentSubscriptionAsync(tenantContext.TenantId!);
            if (subscription == null)
            {
                // Inconsistência (IsPrivatePlan true sem assinatura): trata como Público
                if (!_requiredPlans.Contains("Publico", StringComparer.OrdinalIgnoreCase))
                {
                    context.Result = new ObjectResult(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Recurso não disponível para o plano atual."
                    })
                    {
                        StatusCode = StatusCodes.Status403Forbidden
                    };
                    return;
                }

                await next();
                return;
            }

            // 3. Defesa em profundidade: mesma regra da trava global (helper compartilhado)
            var denial = SubscriptionStatusRules.Evaluate(subscription.Status, context.HttpContext.Request.Method);
            if (denial is not null)
            {
                context.Result = new ObjectResult(new ApiResponse<object>
                {
                    Success = false,
                    Message = denial
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;
            }

            // 4. Responsabilidade principal: PlanType contra a lista
            if (!_requiredPlans.Contains(subscription.PlanType.ToString(), StringComparer.OrdinalIgnoreCase))
            {
                context.Result = new ObjectResult(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Recurso não disponível para o plano atual."
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;
            }

            await next();
        }
    }
}
