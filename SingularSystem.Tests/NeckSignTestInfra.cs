using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SingularSystem.Interfaces;
using SingularSystem.Models.Entities;
using SingularSystem.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace SingularSystem.Tests
{
    /// <summary>
    /// Infraestrutura compartilhada pelos testes de propriedade/integração do NeckSignService:
    /// fábricas de contexto InMemory, HttpMessageHandler programável e um IServiceScopeFactory
    /// que resolve um NeckSignService ligado ao mesmo contexto (necessário por SyncPendingDocuments).
    /// Colaboradores externos (HTTP, PDF) são mockados; o DbContext é EF Core InMemory.
    /// </summary>
    internal static class NeckSignTestInfra
    {
        public static AppDbContext CreateContext(string? name = null) =>
            new(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(name ?? $"necksign-{Guid.NewGuid()}")
                .EnableSensitiveDataLogging()
                .Options);

        public static IConfiguration Configuration() =>
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["NeckSign:BaseUrl"] = "https://necksign.test/",
                    ["NeckSign:ApiKey"] = "test-api-key",
                    ["NeckSign:WebhookUrl"] = "https://singular.test/api/v1/necksign/webhook",
                })
                .Build();

        public static NeckSignService CreateService(
            AppDbContext context,
            ProgrammableHttpMessageHandler handler,
            IServiceScopeFactory? scopeFactory = null)
        {
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://necksign.test/") };
            var factory = new SingleClientFactory(httpClient);

            return new NeckSignService(
                factory,
                new MinimalPdfService(),
                context,
                NullLogger<NeckSignService>.Instance,
                Configuration(),
                scopeFactory ?? new ThrowingScopeFactory());
        }

        /// <summary>
        /// Cria um IServiceScopeFactory que, a cada CreateScope, resolve um NeckSignService novo
        /// ligado ao MESMO banco InMemory (mesmo nome), espelhando o padrão de escopo real.
        /// </summary>
        public static IServiceScopeFactory ScopeFactoryFor(string dbName, ProgrammableHttpMessageHandler handler)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IConfiguration>(Configuration());
            services.AddSingleton<IPDFService, MinimalPdfService>();
            services.AddSingleton<IHttpClientFactory>(_ =>
                new SingleClientFactory(new HttpClient(handler) { BaseAddress = new Uri("https://necksign.test/") }));
            services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(dbName), ServiceLifetime.Scoped);
            services.AddScoped<ISignatureService, NeckSignService>();

            var provider = services.BuildServiceProvider();
            return provider.GetRequiredService<IServiceScopeFactory>();
        }
    }

    /// <summary>IHttpClientFactory que sempre devolve o mesmo HttpClient configurado.</summary>
    internal sealed class SingleClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;
        public SingleClientFactory(HttpClient client) => _client = client;
        public HttpClient CreateClient(string name) => _client;
    }

    /// <summary>
    /// HttpMessageHandler programável: resolve a resposta por delegate em função da requisição,
    /// contabilizando quantas chamadas foram feitas (para verificar no-op de cooldown etc.).
    /// </summary>
    internal sealed class ProgrammableHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;
        private int _callCount;

        public ProgrammableHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
            => _responder = responder;

        public int CallCount => _callCount;

        public static ProgrammableHttpMessageHandler Json(HttpStatusCode status, string body) =>
            new(_ => new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _callCount);
            return Task.FromResult(_responder(request));
        }
    }

    /// <summary>IPDFService mínimo: retorna um PDF válido (começa com %PDF-).</summary>
    internal sealed class MinimalPdfService : IPDFService
    {
        private static readonly byte[] Pdf = Encoding.ASCII.GetBytes("%PDF-1.4\n%EOF\n");
        public Task<byte[]> GeneratePdf(Guid documentId) => Task.FromResult(Pdf);
        public Task<byte[]> GeneratePdf(Guid documentId, SingularSystem.Models.PdfProfile profile) => Task.FromResult(Pdf);
        public Task<string?> GetDocumentType(Guid documentId) => Task.FromResult<string?>(null);
        public Task<byte[]> GenerateStudentsPdf(string state, string city, string schoolID, string serie = "") => Task.FromResult(Array.Empty<byte>());
        public Task<byte[]> GenerateReportsAEEPdf(string state, string city, string schoolID, string disabilities, string disorders) => Task.FromResult(Array.Empty<byte>());
        public Task<byte[]> GenerateCaseStudiesTrackingPdf(string state, string city, string schoolID, string searchTerm, string status, int? year, int? month) => Task.FromResult(Array.Empty<byte>());
        public Task<byte[]> GenerateBiopsychosocialTrackingPdf(string state, string city, string schoolID, string searchTerm, string status, string pendingProfessional, int? year, int? month) => Task.FromResult(Array.Empty<byte>());
    }

    /// <summary>Scope factory que lança se usado (para métodos que não devem resolver escopo).</summary>
    internal sealed class ThrowingScopeFactory : IServiceScopeFactory
    {
        public IServiceScope CreateScope() =>
            throw new InvalidOperationException("O scope factory não deve ser usado neste cenário.");
    }
}
