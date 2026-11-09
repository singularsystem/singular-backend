using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using SingularSystem.Controllers;
using SingularSystem.DTOs.NeckBook;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace SingularSystem.Tests
{
    // Feature: neckbook-books-proxy — Task 13.4: testes unitários de NeckBookAdminController
    // Validates: Requirements 17.1, 18.1, 18.2, 18.3
    public sealed class NeckBookAdminControllerTests
    {
        [Fact]
        public void NeckBookAdminController_requires_Admin_role()
        {
            var authorizeAttr = typeof(NeckBookAdminController)
                .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
                .Cast<AuthorizeAttribute>()
                .FirstOrDefault();

            Assert.NotNull(authorizeAttr);
            Assert.Equal("Admin", authorizeAttr!.Roles);

            var methods = typeof(NeckBookAdminController)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            foreach (var method in methods)
            {
                Assert.Empty(method.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true));
            }
        }

        [Fact]
        public async Task UpsertConfig_encrypts_key_in_db_and_never_exposes_key_in_response_or_logs()
        {
            using var db = NeckBookControllerTestInfra.CreateContext();
            var logger = new CapturingLogger<NeckBookAdminController>();
            var controller = new NeckBookAdminController(db, NeckBookControllerTestInfra.Protector, logger);

            const string plaintextKey = "nk_live_admin_super_secret_key_999";

            var createResult = await controller.UpsertConfig(new UpsertMunicipalityNeckBookConfigRequest
            {
                State = "PI",
                City = "Piripiri",
                ApiKey = plaintextKey,
                IsActive = true
            });

            var createdResult = Assert.IsType<CreatedAtActionResult>(createResult);
            var responseJson = JsonSerializer.Serialize(createdResult.Value);
            var allLogs = string.Join("\n", logger.Entries);

            // A chave nunca aparece na resposta nem nos logs
            Assert.DoesNotContain(plaintextKey, responseJson, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(plaintextKey, allLogs, StringComparison.OrdinalIgnoreCase);

            // No banco a chave está cifrada (diferente do texto em claro) e decifra para o original
            var stored = await db.MunicipalityNeckBookConfigs.SingleAsync();
            Assert.NotEqual(plaintextKey, stored.EncryptedApiKey);
            Assert.Equal(plaintextKey, NeckBookControllerTestInfra.Protector.Unprotect(stored.EncryptedApiKey));
            Assert.DoesNotContain(stored.EncryptedApiKey, responseJson, StringComparison.OrdinalIgnoreCase);

            // Atualização do mesmo município atualiza in-place e também nunca expõe a nova chave
            const string updatedKey = "nk_live_admin_updated_secret_key_888";
            var updateResult = await controller.UpsertConfig(new UpsertMunicipalityNeckBookConfigRequest
            {
                State = "Piauí",
                City = "Piripiri",
                ApiKey = updatedKey,
                IsActive = false
            });

            var okResult = Assert.IsType<OkObjectResult>(updateResult);
            var updateJson = JsonSerializer.Serialize(okResult.Value);
            var updatedLogs = string.Join("\n", logger.Entries);

            Assert.DoesNotContain(updatedKey, updateJson, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(updatedKey, updatedLogs, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(1, await db.MunicipalityNeckBookConfigs.CountAsync());
        }
    }
}
