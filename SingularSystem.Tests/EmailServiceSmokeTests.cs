using System;
using SingularSystem.Configuration;
using SingularSystem.Interfaces;
using SingularSystem.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace SingularSystem.Tests
{
    public sealed class EmailServiceSmokeTests
    {
        [Fact]
        public void IEmailService_is_registered_in_di_container()
        {
            var services = new ServiceCollection();
            services.AddLogging();

            var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
            {
                new System.Collections.Generic.KeyValuePair<string, string?>("Smtp:Host", "smtp.test.com"),
                new System.Collections.Generic.KeyValuePair<string, string?>("Smtp:Port", "587"),
                new System.Collections.Generic.KeyValuePair<string, string?>("Smtp:Username", "user"),
                new System.Collections.Generic.KeyValuePair<string, string?>("Smtp:Password", "pass"),
                new System.Collections.Generic.KeyValuePair<string, string?>("Smtp:FromAddress", "noreply@singularsystem.com.br")
            }).Build();

            services.Configure<SmtpOptions>(config.GetSection(SmtpOptions.SectionName));
            services.AddScoped<IEmailService, SmtpEmailService>();

            using var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();

            var emailService = scope.ServiceProvider.GetService<IEmailService>();
            Assert.NotNull(emailService);
            Assert.IsType<SmtpEmailService>(emailService);
        }

        [Theory]
        [InlineData("Bem-vindo ao sistema IPAAGS-AEE", "Bem-vindo ao sistema Singular System")]
        [InlineData("Notificação da Prefeitura de Teresina para você", "Notificação da Singular System para você")]
        [InlineData("Comunicado Oficial SEMEC", "Comunicado Oficial Singular System")]
        [InlineData("Relatório do iPAAGS gerado com sucesso", "Relatório do Singular System gerado com sucesso")]
        public void Email_content_is_sanitized_without_municipal_branding(string input, string expected)
        {
            var sanitized = SmtpEmailService.SanitizeNeutralContent(input);
            Assert.Equal(expected, sanitized);

            // Nenhuma menção a termos municipais
            Assert.DoesNotContain("IPAAGS", sanitized, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Prefeitura", sanitized, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("SEMEC", sanitized, StringComparison.OrdinalIgnoreCase);
        }
    }
}
