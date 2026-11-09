using SingularSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace SingularSystem.Extensions
{
    public static class EFCoreExtensions
    {
        public static IServiceCollection InjectDbContext(this IServiceCollection services, IConfiguration config)
        {
            services.AddDbContext<AppDbContext>(options =>
                    options.UseSqlServer(
                        config.GetConnectionString("DefaultConnection"),
                        sqlOptions =>
                        {
                            sqlOptions.CommandTimeout(120);
                        }
                    )
            );

            return services;
        }
    }
}
