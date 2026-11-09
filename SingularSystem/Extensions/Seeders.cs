using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace SingularSystem.Extensions
{
    public static class Seeders
    {
        public static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            // Req 2.5, 2.6: Semeia exclusivamente a role Admin necessária para boot.
            // Nenhuma role municipal (Secretary, SocialWork, Psychologist, etc.) e nenhuma
            // role do Módulo 3 (AutonomousProfessional, ClinicAdmin) são semeadas aqui.
            var roles = new[] { "Admin" };

            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
        }

        public static async Task SeedAdminUserAsync(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            // Req 2.7: ipaags.pi@gmail.com removido. Se credenciais forem fornecidas por
            // variáveis de ambiente de provisionamento, cria o admin operacional da Singular.
            var adminEmail = Environment.GetEnvironmentVariable("ADMIN_SEED_EMAIL");
            var adminPassword = Environment.GetEnvironmentVariable("ADMIN_SEED_PASSWORD");

            if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
            {
                var adminUser = await userManager.FindByEmailAsync(adminEmail);
                if (adminUser == null)
                {
                    adminUser = new ApplicationUser
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        EmailConfirmed = true,
                        FullName = "Admin Singular",
                        Sex = Helpers.GetEnumValueFromDisplayName<Sex>("Outro"),
                        DateOfBirth = DateTime.Parse("1980-01-01"),
                        Address = "",
                        City = "",
                        State = ""
                    };

                    var result = await userManager.CreateAsync(adminUser, adminPassword);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(adminUser, "Admin");
                    }
                }
            }
        }

        public static Task SeedAsync(AppDbContext db)
        {
            // Req 2.7: termo AEE_TERMO_RESPONSABILIDADE removido (ADR-007).
            return Task.CompletedTask;
        }

        public static async Task<WebApplication> SeedIdentityData(this WebApplication app)
        {
            using (var scope = app.Services.CreateScope())
            {
                var serviceProvider = scope.ServiceProvider;

                var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                await SeedRolesAsync(roleManager);
                await SeedAdminUserAsync(userManager, roleManager);
                await SeedAsync(db);
            }

            return app;
        }
    }
}
