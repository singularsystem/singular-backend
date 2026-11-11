using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;
using SingularSystem.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace SingularSystem.Tests
{
    // Feature: 012-singular-migration — Task 14.1: testes de integração de NeckSignService.CreateDoc
    //
    // Com HttpClient mockado: o POST /api/v1/envelopes contém externalId=GUID, title não vazio,
    // webhookUrl do Singular System, signers ≥1 e PDF Base64; X-Api-Key presente; 201 completo persiste
    // DocumentToken/SignUrl/Status=InProgress/SignatureProvider=NeckSign; 201 malformado/não-201 →
    // false sem persistir.
    //
    // Validates: Requirements 13.1, 13.2, 13.6, 15.3
    public sealed class NeckSignCreateDocIntegrationTests
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private static AppDbContext CreateContext() =>
            new(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"createdoc-{Guid.NewGuid()}")
                .EnableSensitiveDataLogging()
                .Options);

        private static void SeedPei(AppDbContext ctx, Guid id)
        {
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                FullName = "Responsável",
                Email = "resp@test.local",
                UserName = "resp@test.local"
            };
            ctx.Users.Add(user);
            ctx.PEIs.Add(new PEI { ID = id, User = user, UserID = user.Id, DocumentStatus = Status.None, SignatureProvider = SignatureProvider.NeckSign });
            ctx.SaveChanges();
            ctx.ChangeTracker.Clear();
        }

        [Fact]
        public async Task CreateDoc_posts_well_formed_envelope_and_persists_on_201()
        {
            using var context = CreateContext();
            var id = Guid.NewGuid();
            SeedPei(context, id);

            HttpRequestMessage? captured = null;
            string? capturedBody = null;
            var handler = new CapturingHandler((req, body) =>
            {
                captured = req;
                capturedBody = body;
                var resp = new
                {
                    envelopeId = "env-123",
                    externalId = id.ToString(),
                    status = "PENDING",
                    signingUrl = "https://necksign.test/sign/abc",
                    signers = new[] { new { externalId = Guid.NewGuid().ToString(), signingUrl = "https://necksign.test/sign/abc" } }
                };
                return new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = new StringContent(JsonSerializer.Serialize(resp, JsonOptions), Encoding.UTF8, "application/json")
                };
            });

            var service = BuildService(context, handler);
            var ok = await service.CreateDoc(id, "pei");

            Assert.True(ok);
            Assert.NotNull(captured);
            Assert.Equal(HttpMethod.Post, captured!.Method);
            Assert.Contains("api/v1/envelopes", captured.RequestUri!.ToString());
            Assert.True(captured.Headers.Contains("X-Api-Key"));

            using var doc = JsonDocument.Parse(capturedBody!);
            var root = doc.RootElement;
            Assert.Equal(id.ToString(), root.GetProperty("externalId").GetString());
            Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("title").GetString()));
            Assert.Equal("https://singular.test/api/v1/necksign/webhook", root.GetProperty("webhookUrl").GetString());
            Assert.True(root.GetProperty("signers").GetArrayLength() >= 1);

            // O contrato estrito do NeckSign exige o papel em MAIÚSCULAS (SIGNER/WITNESS/APPROVER).
            var firstSignerRole = root.GetProperty("signers")[0].GetProperty("role").GetString();
            Assert.Equal("SIGNER", firstSignerRole);

            // O schema estrito do NeckSign nomeia o campo do PDF como 'pdfBase64' (não 'base64Pdf').
            var base64 = root.GetProperty("pdfBase64").GetString();
            Assert.False(string.IsNullOrWhiteSpace(base64));
            Assert.StartsWith("%PDF-", Encoding.ASCII.GetString(Convert.FromBase64String(base64!)));

            var pei = context.PEIs.AsNoTracking().First(p => p.ID == id);
            Assert.Equal("env-123", pei.DocumentToken);
            Assert.Equal("https://necksign.test/sign/abc", pei.SignUrl);
            Assert.Equal(Status.InProgress, pei.DocumentStatus);
            Assert.Equal(SignatureProvider.NeckSign, pei.SignatureProvider);
        }

        [Fact]
        public async Task CreateDoc_returns_false_without_persisting_on_malformed_201()
        {
            using var context = CreateContext();
            var id = Guid.NewGuid();
            SeedPei(context, id);

            var handler = new CapturingHandler((_, _) =>
                new HttpResponseMessage(HttpStatusCode.Created)
                {
                    // Sem envelopeId/signingUrl → corpo malformado.
                    Content = new StringContent("{\"status\":\"PENDING\"}", Encoding.UTF8, "application/json")
                });

            var service = BuildService(context, handler);
            var ok = await service.CreateDoc(id, "pei");

            Assert.False(ok);
            var pei = context.PEIs.AsNoTracking().First(p => p.ID == id);
            Assert.Null(pei.DocumentToken);
            Assert.Equal(Status.None, pei.DocumentStatus);
            Assert.Equal(SignatureProvider.NeckSign, pei.SignatureProvider);
        }

        [Fact]
        public async Task CreateDoc_returns_false_without_persisting_on_non_201()
        {
            using var context = CreateContext();
            var id = Guid.NewGuid();
            SeedPei(context, id);

            var handler = new CapturingHandler((_, _) =>
                new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("{\"error\":\"bad\"}", Encoding.UTF8, "application/json")
                });

            var service = BuildService(context, handler);
            var ok = await service.CreateDoc(id, "pei");

            Assert.False(ok);
            var pei = context.PEIs.AsNoTracking().First(p => p.ID == id);
            Assert.Null(pei.DocumentToken);
            Assert.Equal(Status.None, pei.DocumentStatus);
            Assert.Equal(SignatureProvider.NeckSign, pei.SignatureProvider);
        }

        private static NeckSignService BuildService(AppDbContext context, HttpMessageHandler handler)
        {
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://necksign.test/") };
            httpClient.DefaultRequestHeaders.Add("X-Api-Key", "test-api-key");
            var factory = new SingleClientFactory(httpClient);

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new System.Collections.Generic.Dictionary<string, string?>
                {
                    ["NeckSign:BaseUrl"] = "https://necksign.test/",
                    ["NeckSign:ApiKey"] = "test-api-key",
                    ["NeckSign:WebhookUrl"] = "https://singular.test/api/v1/necksign/webhook",
                })
                .Build();

            return new NeckSignService(
                factory,
                new MinimalPdfService(),
                context,
                Microsoft.Extensions.Logging.Abstractions.NullLogger<NeckSignService>.Instance,
                configuration,
                new ThrowingScopeFactory());
        }

        private sealed class CapturingHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, string?, HttpResponseMessage> _responder;
            public CapturingHandler(Func<HttpRequestMessage, string?, HttpResponseMessage> responder) => _responder = responder;

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                string? body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
                return _responder(request, body);
            }
        }
    }
}
