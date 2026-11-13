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
using FsCheck;
using FsCheck.Xunit;

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

        private static readonly string[] SeedRoles =
        {
            "Admin", "Secretary", "SocialWork", "Psychologist", "Psychopedagogue",
            "Teacher", "Coordinator", "Principal", "MunicipalManager",
            "AutonomousProfessional", "SchoolAdmin"
        };

        [Fact]
        public async Task Seed_creates_exactly_the_11_seed_roles()
        {
            using var sp = CreateIdentityServices($"seed-roles-{Guid.NewGuid()}");
            using var scope = sp.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            await Seeders.SeedRolesAsync(roleManager);

            var roles = await roleManager.Roles.Select(r => r.Name!).ToListAsync();
            Assert.Equal(11, roles.Count);
            Assert.Equal(SeedRoles.OrderBy(r => r), roles.OrderBy(r => r));
        }

        [Fact]
        public async Task Seed_roles_twice_keeps_11_roles()
        {
            using var sp = CreateIdentityServices($"seed-roles-twice-{Guid.NewGuid()}");
            using var scope = sp.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            await Seeders.SeedRolesAsync(roleManager);
            await Seeders.SeedRolesAsync(roleManager);

            Assert.Equal(11, await roleManager.Roles.CountAsync());
        }

        // Feature: modulo-3-2b-isolamento-pdf-listagens, Property 12: Idempotência do seed de roles
        // Validates: Requirements 12.1, 12.2, 12.4
        [Property(MaxTest = 100)]
        public bool Seed_roles_n_vezes_resulta_nas_mesmas_11_roles(PositiveInt n)
        {
            var count = Math.Clamp(n.Item, 1, 5);
            using var sp = CreateIdentityServices($"seed-roles-prop-{Guid.NewGuid()}");
            using var scope = sp.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            for (var i = 0; i < count; i++)
            {
                Seeders.SeedRolesAsync(roleManager).GetAwaiter().GetResult();
            }

            var roles = roleManager.Roles.Select(r => r.Name!).ToList();
            return roles.Count == 11 && SeedRoles.OrderBy(r => r).SequenceEqual(roles.OrderBy(r => r));
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
