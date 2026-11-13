using SingularSystem.Authorization;
using SingularSystem.Models.Entities;
using SingularSystem.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace SingularSystem.Extensions
{
    public static class IdentityExtensions
    {
        public static IServiceCollection AddIdentityHandlersAndStores(this IServiceCollection services)
        {
            services.AddIdentity<ApplicationUser, IdentityRole>()
                    .AddEntityFrameworkStores<AppDbContext>()
                    .AddSignInManager<CustomSignInManager>()
                    .AddDefaultTokenProviders();

            services.AddScoped<IAuthorizationHandler, SelfOrRoleHandler>();

            return services;
        }

        public static IServiceCollection ConfigureIdentityOptions(this IServiceCollection services)
        {
            services.Configure<IdentityOptions>(options =>
            {
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = true;
                options.User.RequireUniqueEmail = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Password.RequireDigit = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            });

            return services;
        }

        public static IServiceCollection AddIdentityAuth(this IServiceCollection services, IConfiguration config)
        {
            var issuer = config["JwtIssuer"];
            var audience = config["JwtAudience"] ?? config["JWTAudience"];

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(y =>
            {
                y.SaveToken = false;
                y.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(config["JWTSecret"]!)
                    ),
                    ValidateIssuer = true,
                    ValidateAudience = !string.IsNullOrWhiteSpace(audience),
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                };
                y.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/api/hub/notifications"))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    },

                    OnTokenValidated = async context =>
                    {
                        var userManager = context.HttpContext.RequestServices
                            .GetRequiredService<UserManager<ApplicationUser>>();

                        var userId = context.Principal?
                            .FindFirstValue(ClaimTypes.NameIdentifier);

                        if (string.IsNullOrEmpty(userId))
                        {
                            context.Fail("Token inválido");
                            return;
                        }

                        var user = await userManager.FindByIdAsync(userId);

                        if (user == null || !user.IsActive)
                        {
                            context.Fail("Usuário desativado");
                        }
                    }
                };
            });
            services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder()
                                            .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                                            .RequireAuthenticatedUser()
                                            .Build();
                options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
                options.AddPolicy("SelfOrAdmin", policy =>
                    policy.Requirements.Add(
                        new SelfOrRoleRequirement("Admin")
                    ));
                options.AddPolicy("SelfOrAdminOrMunicipalManager", policy =>
                    policy.Requirements.Add(
                        new SelfOrRoleRequirement("Admin", "MunicipalManager")
                    ));
                options.AddPolicy("AdminOrMunicipalManager", policy =>
                    policy.RequireRole("Admin", "MunicipalManager"));
                options.AddPolicy("AdminOrSecretary", policy =>
                    policy.RequireRole("Admin", "Secretary"));
                options.AddPolicy("AdminOrTeacher", policy =>
                    policy.RequireRole("Admin", "Teacher"));
                options.AddPolicy("AdminOrPsychopedagogue", policy =>
                    policy.RequireRole("Admin", "Psychopedagogue"));
                options.AddPolicy("AdminOrPsychologist", policy =>
                    policy.RequireRole("Admin", "Psychologist"));
                options.AddPolicy("AdminOrSocialWork", policy =>
                    policy.RequireRole("Admin", "SocialWork"));
                options.AddPolicy("AdminOrSchool", policy =>
                    policy.RequireRole("Admin", "Secretary", "Principal", "Coordinator"));
                options.AddPolicy("AdminOrSchoolAdmin", policy =>
                    policy.RequireRole("Admin", "SchoolAdmin"));
                options.AddPolicy("AdminOrAutonomous", policy =>
                    policy.RequireRole("Admin", "AutonomousProfessional"));
                options.AddPolicy("PrivatePlan", policy =>
                    policy.RequireRole("Admin", "AutonomousProfessional", "SchoolAdmin",
                                       "Teacher", "Psychologist", "Psychopedagogue"));
            });

            return services;
        }

        public static WebApplication IdentityAuthMiddlewares(this WebApplication app)
        {
            app.UseAuthentication();
            app.UseAuthorization();

            return app;
        }
    }
}
