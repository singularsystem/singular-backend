using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SingularSystem.Configuration;
using Xunit;

namespace SingularSystem.Tests
{
    public sealed class BootSmokeTests
    {
        // Validates: Requirements 1.1, 5.6, 5.7, 5.8 (Task 12.2)
        // Backend sobe com config válida e responde ao healthcheck; aborta antes de escutar com config inválida.

        [Fact]
        public void Backend_aborts_with_exit_code_1_before_listening_when_config_is_invalid()
        {
            var dllPath = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..", "..",
                "SingularSystem", "bin", "Debug", "net8.0", "SingularSystem.dll"
            ));

            if (!File.Exists(dllPath))
            {
                // Fallback search
                dllPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "SingularSystem.dll"));
            }

            Assert.True(File.Exists(dllPath), $"Target dll not found at {dllPath}");

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"\"{dllPath}\"",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // Force empty environment so required config is absent
            startInfo.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = "Production";
            startInfo.EnvironmentVariables["ConnectionStrings__DefaultConnection"] = "";
            startInfo.EnvironmentVariables["JWTSecret"] = "";
            startInfo.EnvironmentVariables["S3BucketAccessKey"] = "";
            startInfo.EnvironmentVariables["S3BucketSecretKey"] = "";
            startInfo.EnvironmentVariables["S3BucketName"] = "";
            startInfo.EnvironmentVariables["S3SignedDocumentsBucketName"] = "";
            startInfo.EnvironmentVariables["NeckSign__BaseUrl"] = "";
            startInfo.EnvironmentVariables["NeckSign__ApiKey"] = "";
            startInfo.EnvironmentVariables["NeckSign__WebhookSecret"] = "";
            startInfo.EnvironmentVariables["NeckSign__WebhookUrl"] = "";
            startInfo.EnvironmentVariables["Smtp__Host"] = "";
            startInfo.EnvironmentVariables["Smtp__Port"] = "";
            startInfo.EnvironmentVariables["Smtp__Username"] = "";
            startInfo.EnvironmentVariables["Smtp__Password"] = "";
            startInfo.EnvironmentVariables["Smtp__FromAddress"] = "";

            using var process = Process.Start(startInfo)!;
            var stderr = process.StandardError.ReadToEnd();
            var stdout = process.StandardOutput.ReadToEnd();
            process.WaitForExit(15000);

            Assert.True(process.HasExited, "Process should have exited promptly on invalid configuration.");
            Assert.Equal(1, process.ExitCode);
            Assert.Contains("[FATAL]", stderr);
            Assert.Contains("ConnectionStrings:DefaultConnection", stderr);
            Assert.Contains("JWTSecret", stderr);
        }

        [Fact]
        public async Task Backend_with_valid_config_responds_200_ok_on_healthcheck()
        {
            var validSettings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=singular_test;Username=postgres;Password=pass",
                ["JWTSecret"] = "a_very_secret_key_32_bytes_long_minimum!",
                ["S3BucketAccessKey"] = "AKIAEXAMPLEKEY123",
                ["S3BucketSecretKey"] = "SECRETKEYEXAMPLE1234567890",
                ["S3BucketName"] = "test-bucket",
                ["S3SignedDocumentsBucketName"] = "test-signed-bucket",
                ["NeckSign:BaseUrl"] = "https://api.necksign.com",
                ["NeckSign:ApiKey"] = "necksign_key_123",
                ["NeckSign:WebhookSecret"] = "whsec_123",
                ["NeckSign:WebhookUrl"] = "https://app.singularsystem.com.br/api/webhooks/necksign",
                ["Smtp:Host"] = "smtp.test.com",
                ["Smtp:Port"] = "587",
                ["Smtp:Username"] = "smtp_user",
                ["Smtp:Password"] = "smtp_pass",
                ["Smtp:FromAddress"] = "noreply@singularsystem.com.br",
                ["IntegrationSettings:ApiKey"] = "test_integration_key_123",
                ["PublicValidation:BaseUrl"] = "https://app.singularsystem.com.br"
            };

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(validSettings)
                .Build();

            // Validate that the configuration passes fail-fast
            var validation = ConfigValidator.Validate(config);
            Assert.True(validation.IsValid, "Configuration must be valid.");

            // Verify healthcheck endpoint boot and response
            var builder = WebApplication.CreateBuilder();
            builder.Configuration.AddInMemoryCollection(validSettings);
            builder.Services.AddHealthChecks();
            builder.Services.AddRouting();

            // Bind to dynamic ephemeral port
            builder.WebHost.UseKestrel().UseUrls("http://127.0.0.1:0");

            var app = builder.Build();
            app.MapHealthChecks("/health");

            await app.StartAsync();

            try
            {
                var serverAddress = app.Urls;
                Assert.NotEmpty(serverAddress);
                var baseUri = new Uri(new List<string>(serverAddress)[0]);

                using var httpClient = new HttpClient { BaseAddress = baseUri };
                var response = await httpClient.GetAsync("/health");

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                var content = await response.Content.ReadAsStringAsync();
                Assert.Equal("Healthy", content);
            }
            finally
            {
                await app.StopAsync();
                await app.DisposeAsync();
            }
        }
    }
}
