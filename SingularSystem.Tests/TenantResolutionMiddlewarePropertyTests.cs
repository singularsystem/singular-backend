using System.Security.Claims;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SingularSystem.Authorization;
using SingularSystem.Domain.Plans;
using SingularSystem.Domain.Plans.Enums;
using SingularSystem.Infrastructure.Plans;
using SingularSystem.Middleware;
using SingularSystem.Models.Entities;
using SingularSystem.Services;
using Xunit;

namespace SingularSystem.Tests
{
    public sealed class TenantResolutionMiddlewarePropertyTests
    {
        private static (AppDbContext Db, UserManager<ApplicationUser> UserManager) CreateIdentityContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            var db = new AppDbContext(options);
            var store = new UserStore<ApplicationUser, IdentityRole, AppDbContext>(db);
            var userManager = new UserManager<ApplicationUser>(
                store,
                Options.Create(new IdentityOptions()),
                new PasswordHasher<ApplicationUser>(),
                Array.Empty<IUserValidator<ApplicationUser>>(),
                Array.Empty<IPasswordValidator<ApplicationUser>>(),
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                null,
                NullLogger<UserManager<ApplicationUser>>.Instance);

            return (db, userManager);
        }

        // Feature: modulo-3-2-travas-backend, Property 9: Resolução de tenant (titular e subordinado)
        // Validates: Requirements 11.2, 11.4
        [Property(MaxTest = 100)]
        public Property Resolucao_de_tenant_titular_e_subordinado(PositiveInt scenarioGen, PositiveInt planTypeGen)
        {
            var dbName = $"p9-{Guid.NewGuid()}";
            var (db, userManager) = CreateIdentityContext(dbName);
            var repo = new PlanSubscriptionRepository(db);

            var userId = $"user-{Guid.NewGuid()}";
            var schoolTenantId = $"school-{Guid.NewGuid()}";

            var scenario = scenarioGen.Get % 4;
            // 0: claim presente com valor (subordinado novo token)
            // 1: claim presente vazio (titular novo token)
            // 2: claim ausente com user.TenantId no banco (subordinado token legado)
            // 3: claim ausente sem user.TenantId no banco (titular token legado)

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId)
            };

            string expectedResolvedTenantId;
            string? dbUserTenantId = null;

            switch (scenario)
            {
                case 0:
                    claims.Add(new(TenantClaimTypes.TenantId, schoolTenantId));
                    expectedResolvedTenantId = schoolTenantId;
                    break;
                case 1:
                    claims.Add(new(TenantClaimTypes.TenantId, string.Empty));
                    expectedResolvedTenantId = userId;
                    break;
                case 2:
                    // claim ausente; user tem TenantId gravado no banco
                    dbUserTenantId = schoolTenantId;
                    expectedResolvedTenantId = schoolTenantId;
                    break;
                default:
                    // claim ausente; user com TenantId nulo no banco
                    dbUserTenantId = null;
                    expectedResolvedTenantId = userId;
                    break;
            }

            // Persistir usuário no banco
            var appUser = new ApplicationUser
            {
                Id = userId,
                UserName = $"user_{userId}@test.com",
                Email = $"user_{userId}@test.com",
                TenantId = dbUserTenantId,
                City = "City",
                State = "ST"
            };
            db.Users.Add(appUser);

            // Cenário de plano/assinatura
            var planScenario = planTypeGen.Get % 3;
            // 0: sem assinatura (Público legado) => IsPrivatePlan == false, Subscription == null
            // 1: assinatura do tipo Público => IsPrivatePlan == false, Subscription != null
            // 2: assinatura do tipo Privado (Standard/Golden/Empresa) => IsPrivatePlan == true, Subscription != null

            bool expectedIsPrivatePlan = false;
            if (planScenario == 1)
            {
                db.PlanSubscriptions.Add(new PlanSubscription
                {
                    ID = Guid.NewGuid(),
                    TenantId = expectedResolvedTenantId,
                    PlanType = PlanType.Publico,
                    Status = SubscriptionStatus.Active,
                    StartDate = DateTime.UtcNow
                });
                expectedIsPrivatePlan = false;
            }
            else if (planScenario == 2)
            {
                db.PlanSubscriptions.Add(new PlanSubscription
                {
                    ID = Guid.NewGuid(),
                    TenantId = expectedResolvedTenantId,
                    PlanType = PlanType.Empresa,
                    Status = SubscriptionStatus.Active,
                    StartDate = DateTime.UtcNow
                });
                expectedIsPrivatePlan = true;
            }
            db.SaveChanges();

            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

            var tenantContext = new TenantContextService();
            bool nextInvoked = false;
            var middleware = new TenantResolutionMiddleware(ctx =>
            {
                nextInvoked = true;
                return Task.CompletedTask;
            });

            middleware.InvokeAsync(httpContext, tenantContext, repo, userManager).GetAwaiter().GetResult();

            bool tenantOk = tenantContext.TenantId == expectedResolvedTenantId;
            bool isPrivateOk = tenantContext.IsPrivatePlan == expectedIsPrivatePlan;
            bool subOk = planScenario == 0 ? tenantContext.Subscription == null : tenantContext.Subscription != null;

            return (nextInvoked && tenantOk && isPrivateOk && subOk).ToProperty()
                .Label($"scenario={scenario}, planScenario={planScenario}, resolvedTenant={tenantContext.TenantId}, expected={expectedResolvedTenantId}, isPrivate={tenantContext.IsPrivatePlan}");
        }
    }
}
