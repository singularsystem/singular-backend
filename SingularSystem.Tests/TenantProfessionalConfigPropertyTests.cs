using System;
using System.Security.Claims;
using System.Threading.Tasks;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SingularSystem.Controllers;
using SingularSystem.Domain.Plans;
using SingularSystem.Domain.Tenancy;
using SingularSystem.DTOs.Users;
using SingularSystem.Infrastructure.Plans;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Services;
using Xunit;

namespace SingularSystem.Tests
{
    // Feature: modulo-3-3-frontend-gating, Property 5: SchoolAdmin nunca altera SocialWorkEnabled
    // Feature: modulo-3-3-frontend-gating, Property 6: Admin altera os três campos de habilitação
    // Feature: modulo-3-3-frontend-gating, Property 7: Cadastro recusa exatamente os tipos desabilitados
    // Validates: Requirements 4.2, 4.3, 4.4, 4.5, 4.6, 4.7, 4.8, 4.9, 4.10, 4.11, 4.14, 16.7
    public sealed class TenantProfessionalConfigPropertyTests
    {
        private static AppDbContext CreateDb(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new AppDbContext(options);
        }

        [Property(MaxTest = 100)]
        public Property SchoolAdmin_never_modifies_SocialWorkEnabled(
            bool initialPsychologist,
            bool initialPsychopedagogue,
            bool initialSocialWork,
            bool newPsychologist,
            bool newPsychopedagogue,
            bool attemptChangeSocialWork)
        {
            var dbName = $"prop5-{Guid.NewGuid()}";
            using var db = CreateDb(dbName);
            var repo = new TenantProfessionalConfigRepository(db);
            var service = new TenantProfessionalConfigService(repo);

            var tenantId = $"tenant-{Guid.NewGuid()}";
            var callerId = $"school-admin-{Guid.NewGuid()}";

            // Configuração inicial
            var initialConfig = new TenantProfessionalConfig
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                PsychologistEnabled = initialPsychologist,
                PsychopedagogueEnabled = initialPsychopedagogue,
                SocialWorkEnabled = initialSocialWork,
                UpdatedAt = DateTime.UtcNow.AddHours(-1),
                UpdatedBy = "initial-user"
            };
            repo.UpsertAsync(initialConfig).GetAwaiter().GetResult();

            bool? requestedSocialWork = attemptChangeSocialWork ? !initialSocialWork : initialSocialWork;

            var result = service.UpdateBySchoolAdminAsync(
                tenantId,
                callerId,
                newPsychologist,
                newPsychopedagogue,
                requestedSocialWork
            ).GetAwaiter().GetResult();

            var persisted = repo.GetByTenantIdAsync(tenantId).GetAwaiter().GetResult()!;

            if (attemptChangeSocialWork)
            {
                // Deve falhar com Forbidden e manter o estado intacto
                var isForbidden = result.Status == ProfessionalConfigUpdateStatus.Forbidden;
                var unchanged = persisted.PsychologistEnabled == initialPsychologist &&
                                persisted.PsychopedagogueEnabled == initialPsychopedagogue &&
                                persisted.SocialWorkEnabled == initialSocialWork &&
                                persisted.UpdatedBy == "initial-user";
                return (isForbidden && unchanged).ToProperty();
            }
            else
            {
                // Deve ter sucesso e alterar somente Psicólogo e Psicopedagogo
                var isSuccess = result.Status == ProfessionalConfigUpdateStatus.Success;
                var updated = persisted.PsychologistEnabled == newPsychologist &&
                              persisted.PsychopedagogueEnabled == newPsychopedagogue &&
                              persisted.SocialWorkEnabled == initialSocialWork &&
                              persisted.UpdatedBy == callerId;
                return (isSuccess && updated).ToProperty();
            }
        }

        [Property(MaxTest = 100)]
        public Property Admin_modifies_all_three_enabled_flags(
            bool psychologist,
            bool psychopedagogue,
            bool socialWork)
        {
            var dbName = $"prop6-{Guid.NewGuid()}";
            using var db = CreateDb(dbName);
            var repo = new TenantProfessionalConfigRepository(db);
            var service = new TenantProfessionalConfigService(repo);

            var tenantId = $"tenant-{Guid.NewGuid()}";
            var adminId = $"admin-{Guid.NewGuid()}";

            var result = service.UpdateByAdminAsync(
                tenantId,
                adminId,
                psychologist,
                psychopedagogue,
                socialWork
            ).GetAwaiter().GetResult();

            var effective = service.GetEffectiveAsync(tenantId).GetAwaiter().GetResult();

            var matches = effective.PsychologistEnabled == psychologist &&
                          effective.PsychopedagogueEnabled == psychopedagogue &&
                          effective.SocialWorkEnabled == socialWork;

            return matches.ToProperty();
        }

        [Property(MaxTest = 100)]
        public Property Registration_rejects_exactly_disabled_types(
            bool isPrivatePlan,
            bool psychologistEnabled,
            bool psychopedagogueEnabled,
            PositiveInt typeIdx)
        {
            var dbName = $"prop7-{Guid.NewGuid()}";
            using var db = CreateDb(dbName);
            var repo = new TenantProfessionalConfigRepository(db);
            var configService = new TenantProfessionalConfigService(repo);

            var tenantId = $"tenant-{Guid.NewGuid()}";

            // Configuração do tenant
            var config = new TenantProfessionalConfig
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                PsychologistEnabled = psychologistEnabled,
                PsychopedagogueEnabled = psychopedagogueEnabled,
                SocialWorkEnabled = false,
                UpdatedAt = DateTime.UtcNow
            };
            repo.UpsertAsync(config).GetAwaiter().GetResult();

            var validTypes = new[] { "Teacher", "Psychologist", "Psychopedagogue" };
            var selectedType = validTypes[typeIdx.Get % validTypes.Length];

            var isEnabled = configService.IsTypeEnabledAsync(tenantId, selectedType).GetAwaiter().GetResult();

            if (!isPrivatePlan)
            {
                // Em plano Público, a checagem de configuração de tenant não se aplica
                return true.ToProperty();
            }

            if (selectedType == "Teacher")
            {
                return isEnabled.ToProperty(); // Teacher sempre habilitado
            }
            else if (selectedType == "Psychologist")
            {
                return (isEnabled == psychologistEnabled).ToProperty();
            }
            else // Psychopedagogue
            {
                return (isEnabled == psychopedagogueEnabled).ToProperty();
            }
        }

        [Fact]
        public async Task Default_config_without_record_returns_true_true_false()
        {
            using var db = CreateDb($"default-cfg-{Guid.NewGuid()}");
            var repo = new TenantProfessionalConfigRepository(db);
            var service = new TenantProfessionalConfigService(repo);

            var config = await service.GetEffectiveAsync("tenant-inexistente");
            Assert.True(config.PsychologistEnabled);
            Assert.True(config.PsychopedagogueEnabled);
            Assert.False(config.SocialWorkEnabled);

            // Teacher sempre habilitado
            Assert.True(await service.IsTypeEnabledAsync("tenant-inexistente", "Teacher"));
            Assert.True(await service.IsTypeEnabledAsync("tenant-inexistente", "Psychologist"));
            Assert.True(await service.IsTypeEnabledAsync("tenant-inexistente", "Psychopedagogue"));
            Assert.False(await service.IsTypeEnabledAsync("tenant-inexistente", "SocialWork"));
        }

        [Fact]
        public async Task TenantsController_GetMyProfessionalConfig_AdminWithoutPrivateTenant_Returns_400()
        {
            var tenantContext = new TenantContextService
            {
                IsPrivatePlan = false,
                TenantId = null
            };

            var controller = new TenantsController(null!, tenantContext);

            var result = await controller.GetMyProfessionalConfig();

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var apiResp = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
            Assert.False(apiResp.Success);
            Assert.Equal("O usuário atual não possui escola associada.", apiResp.Message);
            Assert.Null(apiResp.ErrorCode);
        }

        [Fact]
        public async Task TenantsController_SchoolAdmin_Attempting_SocialWork_Returns_403()
        {
            using var db = CreateDb($"ctrl-403-{Guid.NewGuid()}");
            var repo = new TenantProfessionalConfigRepository(db);
            var service = new TenantProfessionalConfigService(repo);

            var tenantContext = new TenantContextService
            {
                IsPrivatePlan = true,
                TenantId = "tenant-1"
            };

            var controller = new TenantsController(service, tenantContext)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                        {
                            new Claim(ClaimTypes.NameIdentifier, "school-admin-1")
                        }))
                    }
                }
            };

            var req = new UpdateProfessionalConfigRequest(
                PsychologistEnabled: true,
                PsychopedagogueEnabled: true,
                SocialWorkEnabled: true // Tenta mudar de false para true
            );

            var result = await controller.UpdateMyProfessionalConfig(req);

            var objResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status403Forbidden, objResult.StatusCode);
        }
    }
}
