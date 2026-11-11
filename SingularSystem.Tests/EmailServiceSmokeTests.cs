using System;
using System.IO;
using SingularSystem.Configuration;
using SingularSystem.Interfaces;
using SingularSystem.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace SingularSystem.Tests;

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

    [Fact]
    public void EmailLayout_Wrap_Produces_Valid_Html_Without_Legacy_Terms()
    {
        string sampleTitle = "Confirmação de Acesso";
        string sampleBody = "<p>Seu acesso ao sistema foi liberado com sucesso.</p>";

        string wrappedHtml = EmailLayout.Wrap(sampleTitle, sampleBody);

        Assert.Contains(sampleTitle, wrappedHtml);
        Assert.Contains(sampleBody, wrappedHtml);
        Assert.Contains(EmailLayout.BrandTitle, wrappedHtml);
        Assert.Contains(EmailLayout.PrimaryColor, wrappedHtml);
        Assert.Contains(EmailLayout.AccentColor, wrappedHtml);

        // Não contém Termos_Legados nem hex legados
        var violations = LegacyScanner.Scan(wrappedHtml, "EmailLayout.html", Array.Empty<BrandException>());
        Assert.Empty(violations);
    }
}
