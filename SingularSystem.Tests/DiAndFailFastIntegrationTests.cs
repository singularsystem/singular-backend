using System;
using System.Collections.Generic;
using System.Net.Http;
using SingularSystem.Configuration;
using SingularSystem.Interfaces;
using SingularSystem.Models.Entities;
using SingularSystem.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace SingularSystem.Tests
{
    public sealed class DiAndFailFastIntegrationTests
    {
        private static ServiceProvider BuildProvider(string dbName)
        {
            var configDict = new Dictionary<string, string?>
            {
                ["NeckSign:BaseUrl"] = "https://necksign.test/",
                ["NeckSign:ApiKey"] = "api-key",
                ["NeckSign:WebhookSecret"] = "secret",
                ["NeckSign:WebhookUrl"] = "https://singularsystem.com.br/api/v1/necksign/webhook"
            };

            var configuration = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddHttpClient();
            services.AddHttpClient(NeckSignService.NeckSignHttpClientName, (sp, client) =>
            {
                client.BaseAddress = new Uri("https://necksign.test/");
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("X-Api-Key", "api-key");
            });
            services.AddSingleton<IPDFService, MinimalPdfService>();
            services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(dbName), ServiceLifetime.Scoped);

            services.AddScoped<NeckSignService>();
            services.AddScoped<ISignatureService, NeckSignService>();

            return services.BuildServiceProvider();
        }

        [Fact]
        public void Di_resolves_NeckSignService_as_ISignatureService()
        {
            using var provider = BuildProvider($"di-neck-{Guid.NewGuid()}");
            using var scope = provider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ISignatureService>();
            Assert.IsType<NeckSignService>(service);
        }

        [Fact]
        public void SignatureService_is_registered_scoped()
        {
            using var provider = BuildProvider($"di-scope-{Guid.NewGuid()}");

            ISignatureService first, second, other;
            using (var scope = provider.CreateScope())
            {
                first = scope.ServiceProvider.GetRequiredService<ISignatureService>();
                second = scope.ServiceProvider.GetRequiredService<ISignatureService>();
            }
            using (var scope2 = provider.CreateScope())
            {
                other = scope2.ServiceProvider.GetRequiredService<ISignatureService>();
            }

            Assert.Same(first, second);
            Assert.NotSame(first, other);
        }
    }
}
