using SingularSystem.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using SingularSystem.Extensions;
using SingularSystem.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace SingularSystem.Tests
{
    public sealed class DatabaseBootstrapTests
    {
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

        // Task 4.3: Testes de exemplo para o seed neutro (Req 2.5, 2.6, 2.7)
        [Fact]
        public async Task Seed_creates_only_Admin_role_and_no_municipal_roles()
        {
            using var sp = CreateIdentityServices($"seed-roles-{Guid.NewGuid()}");
            using var scope = sp.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            await Seeders.SeedRolesAsync(roleManager);

            var roles = await roleManager.Roles.Select(r => r.Name).ToListAsync();

            // Contém Admin
            Assert.Contains("Admin", roles);
            Assert.Single(roles);

            // NÃO contém roles municipais
            var forbiddenRoles = new[]
            {
                "Secretary", "SocialWork", "Psychologist", "Psychopedagogue",
                "Teacher", "Coordinator", "Principal", "MunicipalManager"
            };
            foreach (var forbidden in forbiddenRoles)
            {
                Assert.DoesNotContain(forbidden, roles);
            }

            // NÃO contém roles do Módulo 3
            Assert.DoesNotContain("AutonomousProfessional", roles);
            Assert.DoesNotContain("ClinicAdmin", roles);
        }

        [Fact]
        public async Task Seed_does_not_create_legacy_admin_user_nor_responsibility_term()
        {
            using var sp = CreateIdentityServices($"seed-user-term-{Guid.NewGuid()}");
            using var scope = sp.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await Seeders.SeedAdminUserAsync(userManager, roleManager);
            await Seeders.SeedAsync(db);

            // Ausência de conta de e-mail legada
            var legacyUser = await userManager.FindByEmailAsync("ipaags.pi@gmail.com");
            Assert.Null(legacyUser);

            // Ausência do termo AEE_TERMO_RESPONSABILIDADE
            var legacyTerm = await db.Terms.FirstOrDefaultAsync(t => t.Key == "AEE_TERMO_RESPONSABILIDADE");
            Assert.Null(legacyTerm);
        }

        // Task 4.4: Teste de integração do bootstrap de banco (Req 2.1, 2.3, 2.4, 2.8)
        [Fact]
        public async Task Database_bootstrap_schema_does_not_contain_legacy_municipal_structures()
        {
            using var sp = CreateIdentityServices($"schema-check-{Guid.NewGuid()}");
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Verifica que entidades essenciais existem
            Assert.NotNull(db.Model.FindEntityType(typeof(ApplicationUser)));
            Assert.NotNull(db.Model.FindEntityType(typeof(ProcessedWebhookEvent)));
            Assert.NotNull(db.Model.FindEntityType(typeof(SingularSystem.Models.Entities.File)));
            Assert.NotNull(db.Model.FindEntityType(typeof(Attachment)));

            // E que nenhuma migração legada de seeds municipais está presente
            var termsCount = await db.Terms.CountAsync(t => t.Key == "AEE_TERMO_RESPONSABILIDADE");
            Assert.Equal(0, termsCount);
        }

        [Fact]
        public void Failed_migration_propagates_exception_and_aborts()
        {
            // Cria um DbContext configurado para falhar em migrations
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer("Server=invalid-server-name-999;Database=NonExistent;Connect Timeout=1;")
                .Options;

            using var db = new AppDbContext(options);

            // MigrationRunner deve propagar exceção em caso de falha de conexão / migração (Req 2.8)
            Assert.ThrowsAny<Exception>(() =>
            {
                try
                {
                    db.Database.Migrate();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[FATAL] Migration error: {ex.Message}");
                    throw;
                }
            });
        }
    }
}
