using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SingularSystem.Configuration;
using SingularSystem.Controllers;
using SingularSystem.Interfaces;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;
using SingularSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace SingularSystem.Tests
{
    public sealed class NeckSignIntegrationSmokeTests
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        [Fact]
        public void ISignatureService_resolves_only_to_NeckSignService()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            services.AddHttpClient();
            services.AddScoped<NeckSignService>();
            services.AddScoped<ISignatureService, NeckSignService>();
            services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase($"smoke-di-{Guid.NewGuid()}"));
            services.AddSingleton<IPDFService, MinimalPdfService>();

            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();

            var signatureService = scope.ServiceProvider.GetRequiredService<ISignatureService>();
            Assert.IsType<NeckSignService>(signatureService);
        }

        [Fact]
        public void Structural_absence_of_ZapSign_and_legacy_migration_artifacts()
        {
            var assembly = typeof(NeckSignService).Assembly;

            // Nenhum tipo ZapSign no assembly
            var zapTypes = assembly.GetTypes()
                .Where(t => t.Name.Contains("ZapSign") && t.Name != nameof(SignatureProvider))
                .Select(t => t.FullName)
                .ToList();
            Assert.Empty(zapTypes);

            // Ausência da camada de migração legada
            var migrationTypes = assembly.GetTypes()
                .Where(t => t.Name is "LegacyMediaMigrationService" or "MigrationStatusService" or "CutoverRecreationService" or "MigrationAdminController")
                .Select(t => t.FullName)
                .ToList();
            Assert.Empty(migrationTypes);
        }

        [Fact]
        public async Task NeckSign_webhook_accepts_valid_hmac_and_rejects_invalid_hmac()
        {
            using var harness = new NeckSignControllerHarness();
            var docId = Guid.NewGuid();
            harness.Context.PEIs.Add(new PEI
            {
                ID = docId,
                DocumentToken = "smoke-env-1",
                DocumentStatus = Status.InProgress,
                SignatureProvider = SignatureProvider.NeckSign
            });
            await harness.Context.SaveChangesAsync();

            var payload = new
            {
                @event = "envelope.signed",
                envelopeId = "smoke-env-1",
                externalId = docId.ToString(),
                signedFile = "https://files.test/signed.pdf",
                signers = new[]
                {
                    new
                    {
                        externalId = Guid.NewGuid().ToString(),
                        status = "SIGNED",
                        signedAt = DateTimeOffset.UtcNow
                    }
                }
            };
            var body = JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions);

            // 1. HMAC válida -> 200 OK
            var validHeader = NeckSignControllerHarness.ComputeSignatureHeader(body, NeckSignControllerHarness.WebhookSecret);
            var validResult = await harness.PostWebhookAsync(body, validHeader);
            Assert.True(validResult is OkResult or OkObjectResult);

            // 2. HMAC inválida -> 403 Forbidden
            var invalidHeader = NeckSignControllerHarness.ComputeSignatureHeader(body, "invalid-secret");
            var invalidResult = await harness.PostWebhookAsync(body, invalidHeader);
            Assert.True(invalidResult is StatusCodeResult { StatusCode: 403 });
        }
    }
}
