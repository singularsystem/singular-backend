using SingularSystem.Models.Entities;

namespace SingularSystem.Extensions
{
    public static class AppConfigExtensions
    {
        public static IServiceCollection AddCORSOrigins(this IServiceCollection services, IConfiguration config, string name)
        {
            var allowedOrigins = config.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
            var sanitizedOrigins = allowedOrigins
                .Where(o => !string.IsNullOrWhiteSpace(o))
                .Select(o => o.Trim().TrimEnd('/'))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            services.AddCors(options =>
            {
                options.AddPolicy(name: name,
                    policy =>
                    {
                        var originsToUse = sanitizedOrigins.Length > 0
                            ? sanitizedOrigins
                            : new[] { "http://localhost:3000" };

                        policy.WithOrigins(originsToUse)
                              .AllowAnyHeader()
                              .AllowAnyMethod()
                              .AllowCredentials()
                              .WithExposedHeaders("Content-Disposition");
                    }
                );
                //options.AddPolicy(name: name,
                //    policy =>
                //    {
                //        policy.WithOrigins("http://localhost:*",
                //                           "https://localhost:*",
                //                           "http://localhost:3000",
                //                           "http://172.25.96.1:3000",
                //                           "https://localhost:3000")
                //              .AllowAnyMethod();
                //    }
                //);
            });
            return services;
        }

        public static WebApplication ConfigureCORS(this WebApplication app, string name)
        {
            app.UseCors(name);

            return app;
        }

        public static IServiceCollection AddAppConfig(this IServiceCollection services, IConfiguration config)
        {
            services.Configure<AppSettings>(config.GetSection("AppSettings"));

            return services;
        }

        public static IServiceCollection LowercaseURLs(this IServiceCollection services)
        {
            services.Configure<RouteOptions>(options =>
            {
                options.LowercaseUrls = true;
            });

            return services;
        }
    }
}
