using SingularSystem.Domain.Access;
using SingularSystem.Domain.Plans;
using SingularSystem.Domain.Tenancy;
using SingularSystem.Infrastructure.Plans;
using SingularSystem.Services;

namespace SingularSystem.Extensions
{
    public static class PlanExtensions
    {
        public static IServiceCollection AddPlanServices(this IServiceCollection services)
        {
            services.AddScoped<IPlanSubscriptionRepository, PlanSubscriptionRepository>();
            services.AddScoped<IPlanService, PlanService>();
            services.AddScoped<ITenantContext, TenantContextService>();
            services.AddScoped<IStudentAccessPolicy, StudentAccessPolicy>();
            services.AddHostedService<SubscriptionExpirationService>();
            return services;
        }
    }
}
