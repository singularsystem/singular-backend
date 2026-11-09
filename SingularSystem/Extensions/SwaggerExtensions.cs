using Microsoft.OpenApi.Models;

namespace SingularSystem.Extensions
{
    public static class SwaggerExtensions
    {
        public static IServiceCollection AddSwaggerExplorer(this IServiceCollection services, IWebHostEnvironment env)
        {
            services.AddEndpointsApiExplorer();

            if (env.IsDevelopment())
            {
                services.AddSwaggerGen(options =>
                {
                    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                    {
                        Name = "Authorization",
                        Type = SecuritySchemeType.Http,
                        Scheme = "Bearer",
                        BearerFormat = "JWT",
                        In = ParameterLocation.Header,
                        Description = "Fill in the JWT token",
                    });
                    options.AddSecurityRequirement(new OpenApiSecurityRequirement
                    {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id="Bearer"
                            }
                        },
                        new List<String>()
                    }
                    });
                });
            }

            return services;
        }

        public static WebApplication ConfigureSwaggerExplorer(this WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            return app;
        }
    }
}
