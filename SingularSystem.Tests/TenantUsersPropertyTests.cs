using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SingularSystem.Controllers;
using SingularSystem.Domain.Plans;
using SingularSystem.Domain.Tenancy;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Services;
using Xunit;

namespace SingularSystem.Tests
{
    // Feature: modulo-3-3-frontend-gating, Property 3: Listagem de usuários respeita o tenant, o titular e a busca
    // Feature: modulo-3-3-frontend-gating, Property 4: Desativação é isolada por tenant, uniforme na negação e libera vaga
    // Validates: Requirements 2.1, 2.3, 2.5, 2.6, 2.7, 2.8, 2.9, 2.10, 3.1, 3.3, 3.4, 3.5, 16.11, 16.14
    public sealed class TenantUsersPropertyTests
    {
        private sealed class DummyPlanService : IPlanService
        {
            public int MaxUsers { get; set; } = 10;
            public Task<PlanSubscription?> GetActiveSubscriptionAsync(string tenantId) => Task.FromResult<PlanSubscription?>(null);
            public Task<PlanSubscription?> GetCurrentSubscriptionAsync(string tenantId)
            {
                return Task.FromResult<PlanSubscription?>(new PlanSubscription
                {
                    TenantId = tenantId,
                    MaxUsers = MaxUsers
                });
            }
            public Task<QuotaResult> CheckStudentQuotaAsync(string tenantId) => Task.FromResult(new QuotaResult(true, 0, 10));
            public Task<QuotaResult> CheckStudentQuotaAsync(string tenantId, int additional) => Task.FromResult(new QuotaResult(true, 0, 10));
        }

        private static ServiceProvider CreateIdentityServices(string dbName)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(dbName));
            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();
            return services.BuildServiceProvider();
        }

        [Property(MaxTest = 100)]
        public Property User_listing_respects_tenant_owner_and_search(
            PositiveInt activeSubordinatesCount,
            PositiveInt inactiveSubordinatesCount,
            bool ownerHasTenantIdExplicit,
            PositiveInt searchSeed)
        {
            var dbName = $"prop3-{Guid.NewGuid()}";
            using var sp = CreateIdentityServices(dbName);
            var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
            var planService = new DummyPlanService();
            var tenantUserService = new TenantUserService(userManager, planService);

            var tenantA = $"tenant-a-{Guid.NewGuid()}";
            var tenantB = $"tenant-b-{Guid.NewGuid()}";

            var nActive = (activeSubordinatesCount.Get % 6) + 1; // 1 to 6
            var nInactive = (inactiveSubordinatesCount.Get % 4);  // 0 to 3

            // 1. Titular do Tenant A (TenantId nulo, ou igual ao Id se provisionado explicitamente)
            var ownerA = new ApplicationUser
            {
                Id = tenantA,
                UserName = $"owner-{tenantA}",
                Email = $"owner@{tenantA}.com",
                FullName = "Titular Tenant A",
                TenantId = ownerHasTenantIdExplicit ? tenantA : null,
                IsActive = true
            };
            userManager.CreateAsync(ownerA).GetAwaiter().GetResult();

            // 2. Subordinados ativos Tenant A
            var activeUsersA = new List<ApplicationUser>();
            for (var i = 0; i < nActive; i++)
            {
                var user = new ApplicationUser
                {
                    Id = $"sub-active-{i}-{Guid.NewGuid()}",
                    UserName = $"user-active-{i}-{tenantA}",
                    Email = $"active{i}@{tenantA}.com",
                    FullName = i == 0 ? $"BuscaEspecial {i}" : $"Aluno Regular {i}",
                    TenantId = tenantA,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-i)
                };
                userManager.CreateAsync(user).GetAwaiter().GetResult();
                activeUsersA.Add(user);
            }

            // 3. Subordinados inativos Tenant A
            for (var i = 0; i < nInactive; i++)
            {
                var user = new ApplicationUser
                {
                    Id = $"sub-inactive-{i}-{Guid.NewGuid()}",
                    UserName = $"user-inactive-{i}-{tenantA}",
                    Email = $"inactive{i}@{tenantA}.com",
                    FullName = $"Inativo {i}",
                    TenantId = tenantA,
                    IsActive = false
                };
                userManager.CreateAsync(user).GetAwaiter().GetResult();
            }

            // 4. Usuários do Tenant B
            var ownerB = new ApplicationUser
            {
                Id = tenantB,
                UserName = $"owner-{tenantB}",
                Email = $"owner@{tenantB}.com",
                FullName = "Titular Tenant B",
                TenantId = null,
                IsActive = true
            };
            userManager.CreateAsync(ownerB).GetAwaiter().GetResult();

            var userB = new ApplicationUser
            {
                Id = $"sub-b-{Guid.NewGuid()}",
                UserName = $"user-b-{tenantB}",
                Email = $"user@{tenantB}.com",
                FullName = "Usuario B",
                TenantId = tenantB,
                IsActive = true
            };
            userManager.CreateAsync(userB).GetAwaiter().GetResult();

            // Verificação 1: CountActiveAsync coincide exatamente com os subordinados ativos do Tenant A
            var countActive = tenantUserService.CountActiveAsync(tenantA).GetAwaiter().GetResult();
            var countMatches = countActive == nActive;

            // Verificação 2: ListAsync sem termo
            var listResult = tenantUserService.ListAsync(tenantA, 1, 50, null).GetAwaiter().GetResult();
            var listCountMatches = listResult.Items.Count == nActive && listResult.Total == nActive;
            var summaryUsedMatches = listResult.Summary.Used == nActive;
            var ownerExcluded = listResult.Items.All(u => u.Id != tenantA);
            var tenantBExcluded = listResult.Items.All(u => activeUsersA.Any(a => a.Id == u.Id));

            // Verificação 3: ListAsync com busca
            var searchResult = tenantUserService.ListAsync(tenantA, 1, 50, "BuscaEspecial").GetAwaiter().GetResult();
            var searchItemMatches = searchResult.Items.Count == 1 && searchResult.Items[0].FullName.Contains("BuscaEspecial");
            var searchSummaryUsedMatches = searchResult.Summary.Used == nActive; // used não muda com busca

            return (countMatches &&
                    listCountMatches &&
                    summaryUsedMatches &&
                    ownerExcluded &&
                    tenantBExcluded &&
                    searchItemMatches &&
                    searchSummaryUsedMatches).ToProperty();
        }

        [Property(MaxTest = 100)]
        public Property Deactivation_is_tenant_isolated_uniform_and_frees_slot(
            PositiveInt scenarioIdx)
        {
            var dbName = $"prop4-{Guid.NewGuid()}";
            using var sp = CreateIdentityServices(dbName);
            var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
            var planService = new DummyPlanService();
            var tenantUserService = new TenantUserService(userManager, planService);

            var tenantA = $"tenant-a-{Guid.NewGuid()}";
            var tenantB = $"tenant-b-{Guid.NewGuid()}";
            var callerId = $"caller-{Guid.NewGuid()}";

            // Titular e chamador
            var caller = new ApplicationUser
            {
                Id = callerId,
                UserName = "caller-user",
                Email = "caller@tenant.com",
                FullName = "School Admin Chamador",
                TenantId = tenantA,
                IsActive = true
            };
            userManager.CreateAsync(caller).GetAwaiter().GetResult();

            // Subordinado Tenant A
            var subA = new ApplicationUser
            {
                Id = $"sub-a-{Guid.NewGuid()}",
                UserName = "sub-a-user",
                Email = "suba@tenant.com",
                FullName = "Subordinado A",
                TenantId = tenantA,
                IsActive = true
            };
            userManager.CreateAsync(subA).GetAwaiter().GetResult();

            // Subordinado Tenant B
            var subB = new ApplicationUser
            {
                Id = $"sub-b-{Guid.NewGuid()}",
                UserName = "sub-b-user",
                Email = "subb@tenant.com",
                FullName = "Subordinado B",
                TenantId = tenantB,
                IsActive = true
            };
            userManager.CreateAsync(subB).GetAwaiter().GetResult();

            var scenario = scenarioIdx.Get % 5;
            switch (scenario)
            {
                case 0:
                    // Desativar a si próprio: negado
                    var resSelf = tenantUserService.DeactivateAsync(callerId, tenantA, callerId).GetAwaiter().GetResult();
                    var callerStillActive = userManager.FindByIdAsync(callerId).GetAwaiter().GetResult()!.IsActive;
                    return (resSelf == DeactivateUserResult.Denied && callerStillActive).ToProperty();

                case 1:
                    // Desativar usuário de outro tenant: negado
                    var resOther = tenantUserService.DeactivateAsync(callerId, tenantA, subB.Id).GetAwaiter().GetResult();
                    var subBStillActive = userManager.FindByIdAsync(subB.Id).GetAwaiter().GetResult()!.IsActive;
                    return (resOther == DeactivateUserResult.Denied && subBStillActive).ToProperty();

                case 2:
                    // Desativar usuário inexistente: negado
                    var resNonExistent = tenantUserService.DeactivateAsync(callerId, tenantA, "non-existent-id").GetAwaiter().GetResult();
                    return (resNonExistent == DeactivateUserResult.Denied).ToProperty();

                case 3:
                    // Desativar o titular (targetId == tenantId): negado
                    var resOwner = tenantUserService.DeactivateAsync(callerId, tenantA, tenantA).GetAwaiter().GetResult();
                    return (resOwner == DeactivateUserResult.Denied).ToProperty();

                default:
                    // Desativação bem-sucedida: desativa e libera vaga
                    var countBefore = tenantUserService.CountActiveAsync(tenantA).GetAwaiter().GetResult();
                    var resSuccess = tenantUserService.DeactivateAsync(callerId, tenantA, subA.Id).GetAwaiter().GetResult();
                    var countAfter = tenantUserService.CountActiveAsync(tenantA).GetAwaiter().GetResult();
                    var subANowInactive = !userManager.FindByIdAsync(subA.Id).GetAwaiter().GetResult()!.IsActive;

                    // Desativar novamente deve ser negado (já está inativo)
                    var resAgain = tenantUserService.DeactivateAsync(callerId, tenantA, subA.Id).GetAwaiter().GetResult();

                    return (resSuccess == DeactivateUserResult.Deactivated &&
                            subANowInactive &&
                            countAfter == countBefore - 1 &&
                            resAgain == DeactivateUserResult.Denied).ToProperty();
            }
        }

        [Fact]
        public async Task UsersController_ListSchoolUsers_AdminWithoutPrivateTenant_Returns_400()
        {
            var tenantContext = new TenantContextService
            {
                IsPrivatePlan = false,
                TenantId = null
            };

            var controller = new UsersController(
                null!, null!, null!,
                NullLogger<UsersController>.Instance,
                null!, null!, null!, null!,
                new DummyPlanService(),
                tenantContext,
                null!,
                null!);

            var result = await controller.ListSchoolUsers();

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var apiResp = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
            Assert.False(apiResp.Success);
            Assert.Equal("O usuário atual não possui escola associada.", apiResp.Message);
            Assert.Null(apiResp.ErrorCode);
        }

        [Fact]
        public async Task UsersController_DeactivateSchoolUser_Denied_Returns_403_Uniform()
        {
            var tenantContext = new TenantContextService
            {
                IsPrivatePlan = true,
                TenantId = "tenant-1"
            };

            var dbName = $"controller-denied-{Guid.NewGuid()}";
            using var sp = CreateIdentityServices(dbName);
            var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
            var tenantUserService = new TenantUserService(userManager, new DummyPlanService());

            var controller = new UsersController(
                userManager, null!, null!,
                NullLogger<UsersController>.Instance,
                null!, null!, null!, null!,
                new DummyPlanService(),
                tenantContext,
                tenantUserService,
                null!);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "caller-id")
                    }))
                }
            };

            // Alvo inexistente -> 403 uniforme
            var result = await controller.DeactivateSchoolUser("non-existent-id");

            var objResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status403Forbidden, objResult.StatusCode);
            var apiResp = Assert.IsType<ApiResponse<object>>(objResult.Value);
            Assert.False(apiResp.Success);
            Assert.Equal("Acesso negado.", apiResp.Message);
        }
    }
}
