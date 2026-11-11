using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FsCheck;
using FsCheck.Xunit;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;
using SingularSystem.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace SingularSystem.Tests
{
    // Feature: 012-singular-migration, Property 3: Correlação de signingUrl por papel
    //
    // Para qualquer resposta `201 Created` de criação de um `report` contendo signatários
    // por papel, cada campo de URL por papel (`SocialWorkerSignUrl`, `PsychologistSignUrl`,
    // `PsychopedagogueSignUrl`) deve receber a `signingUrl` do signatário cujo `externalId`
    // corresponde àquele papel (correlação por externalId → SocialWorkerReportID /
    // PsychologistReportID / PsychopedagogueReportID).
    //
    // A unidade sob teste é NeckSignService.CreateDoc (público): o 201-handling e a correlação
    // de ApplyCreatedEnvelope (Req 2.9). CreateDoc é exercitado de ponta a ponta com um
    // HttpMessageHandler FAKE que devolve um envelope 201 gerado (envelopeId, signingUrl,
    // signers[] com externalId + signingUrl por papel), um AppDbContext EF Core InMemory
    // semeado com um Report (+ sub-relatórios/usuários para a composição produzir signers) e
    // os três FKs de papel, um IPDFService stub (bytes `%PDF-` válidos) e IConfiguration com
    // NeckSign:BaseUrl/ApiKey. Geramos atribuições aleatórias de signingUrl por papel e
    // ordenações aleatórias dos signatários; afirmamos que cada campo por papel recebe a
    // signingUrl cujo externalId corresponde àquele papel.
    //
    // Validates: Requirements 2.9
    public sealed class NeckSignSigningUrlCorrelationPropertyTests
    {
        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web);

        [Property(MaxTest = 100, Arbitrary = new[] { typeof(ReportSigningCaseArbContainer) })]
        public Property SigningUrl_is_correlated_to_the_matching_role_by_externalId(ReportSigningCase testCase)
        {
            // --- Arrange: IDs por papel (externalId de correlação) ---
            var reportId = Guid.NewGuid();
            var socialWorkerReportId = Guid.NewGuid();
            var psychologistReportId = Guid.NewGuid();
            var psychopedagogueReportId = Guid.NewGuid();

            // signingUrl aleatória por papel (geradas pelo caso de teste).
            var socialWorkerUrl = testCase.SocialWorkerUrl;
            var psychologistUrl = testCase.PsychologistUrl;
            var psychopedagogueUrl = testCase.PsychopedagogueUrl;

            using var context = CreateInMemoryContext();
            SeedReport(
                context,
                reportId,
                socialWorkerReportId,
                psychologistReportId,
                psychopedagogueReportId);

            // Corpo do 201: signers por papel, em ordem embaralhada pelo caso de teste.
            var signers = new List<(string ExternalId, string SigningUrl)>
            {
                (socialWorkerReportId.ToString(), socialWorkerUrl),
                (psychologistReportId.ToString(), psychologistUrl),
                (psychopedagogueReportId.ToString(), psychopedagogueUrl),
            };
            var orderedSigners = testCase.SignerOrder.Select(i => signers[i]).ToList();

            var envelopeId = Guid.NewGuid().ToString();
            var responseJson = BuildCreatedEnvelopeJson(
                envelopeId,
                reportId.ToString(),
                // signingUrl de nível de envelope distinta das por papel, para garantir que a
                // correlação use a URL por signatário e não o fallback de nível de envelope.
                topLevelSigningUrl: "https://necksign.test/envelope-level",
                orderedSigners);

            var service = BuildService(context, responseJson);

            // --- Act ---
            var created = service.CreateDoc(reportId, "report").GetAwaiter().GetResult();

            // --- Assert ---
            var report = context.Reports.AsNoTracking().First(r => r.ID == reportId);

            var okCreated = created.ToProperty().Label("CreateDoc deveria retornar true no 201 completo");

            var socialOk = (report.SocialWorkerSignUrl == socialWorkerUrl).ToProperty()
                .Label($"SocialWorkerSignUrl esperado '{socialWorkerUrl}' mas foi '{report.SocialWorkerSignUrl}'");

            var psychOk = (report.PsychologistSignUrl == psychologistUrl).ToProperty()
                .Label($"PsychologistSignUrl esperado '{psychologistUrl}' mas foi '{report.PsychologistSignUrl}'");

            var psychopedOk = (report.PsychopedagogueSignUrl == psychopedagogueUrl).ToProperty()
                .Label($"PsychopedagogueSignUrl esperado '{psychopedagogueUrl}' mas foi '{report.PsychopedagogueSignUrl}'");

            var tokenOk = (report.DocumentToken == envelopeId).ToProperty()
                .Label("DocumentToken deveria ser persistido com o envelopeId retornado");

            var statusOk = (report.DocumentStatus == Status.InProgress).ToProperty()
                .Label("DocumentStatus deveria ser InProgress após 201 completo");

            return okCreated
                .And(socialOk)
                .And(psychOk)
                .And(psychopedOk)
                .And(tokenOk)
                .And(statusOk);
        }

        // Cenário REAL do NeckSign: a resposta 201 traz signers[] com { email, signingUrl } e SEM
        // externalId (o NeckSign não envia externalId). A correlação de signingUrl por papel deve
        // então ser feita POR E-MAIL — cada papel do Singular System tem e-mail conhecido (social@/psi@/psiped@)
        // e deve receber a URL correspondente. Garante que o relatório multi-signatário obtém as três
        // URLs mesmo sem externalId na resposta do provedor.
        [Fact]
        public void SigningUrl_is_correlated_by_email_when_provider_omits_externalId()
        {
            var reportId = Guid.NewGuid();
            var socialWorkerReportId = Guid.NewGuid();
            var psychologistReportId = Guid.NewGuid();
            var psychopedagogueReportId = Guid.NewGuid();

            using var context = CreateInMemoryContext();
            SeedReport(context, reportId, socialWorkerReportId, psychologistReportId, psychopedagogueReportId);

            const string socialUrl = "https://necksign.test/sign/social-xyz";
            const string psychUrl = "https://necksign.test/sign/psi-xyz";
            const string psychopedUrl = "https://necksign.test/sign/psiped-xyz";

            // signers por E-MAIL (sem externalId), como o NeckSign real retorna.
            var body = new
            {
                envelopeId = Guid.NewGuid().ToString(),
                externalId = reportId.ToString(),
                status = "PENDING",
                signingUrl = "https://necksign.test/sign/social-xyz",
                signers = new object[]
                {
                    new { email = "social@test.local", signingUrl = socialUrl },
                    new { email = "psi@test.local", signingUrl = psychUrl },
                    new { email = "psiped@test.local", signingUrl = psychopedUrl },
                },
            };
            var responseJson = JsonSerializer.Serialize(body, JsonOptions);

            var service = BuildService(context, responseJson);
            var created = service.CreateDoc(reportId, "report").GetAwaiter().GetResult();

            Assert.True(created);
            var report = context.Reports.AsNoTracking().First(r => r.ID == reportId);
            Assert.Equal(socialUrl, report.SocialWorkerSignUrl);
            Assert.Equal(psychUrl, report.PsychologistSignUrl);
            Assert.Equal(psychopedUrl, report.PsychopedagogueSignUrl);
        }

        /// <summary>
        /// Monta o AppDbContext InMemory com um Report completo o suficiente para que a
        /// composição de signatários produza entradas qualificadas (nome + e-mail) e para que
        /// os FKs de papel (<c>SocialWorkerReportID</c>/<c>PsychologistReportID</c>/
        /// <c>PsychopedagogueReportID</c>) coincidam com o <c>ID</c> dos sub-relatórios, que são
        /// os <c>externalId</c> usados na correlação do 201.
        /// </summary>
        private static void SeedReport(
            AppDbContext context,
            Guid reportId,
            Guid socialWorkerReportId,
            Guid psychologistReportId,
            Guid psychopedagogueReportId)
        {
            var socialWorkerUser = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                FullName = "Assistente Social",
                Email = "social@test.local",
                UserName = "social@test.local",
            };
            var psychologistUser = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                FullName = "Psicologo",
                Email = "psi@test.local",
                UserName = "psi@test.local",
            };
            var psychopedagogueUser = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                FullName = "Psicopedagogo",
                Email = "psiped@test.local",
                UserName = "psiped@test.local",
            };

            context.Users.AddRange(socialWorkerUser, psychologistUser, psychopedagogueUser);

            var socialWorkerReport = new SocialWorkerReport
            {
                ID = socialWorkerReportId,
                UserID = socialWorkerUser.Id,
                User = socialWorkerUser,
                ReportID = reportId,
            };
            var psychologistReport = new PsychologistReport
            {
                ID = psychologistReportId,
                UserID = psychologistUser.Id,
                User = psychologistUser,
                ReportID = reportId,
            };
            var psychopedagogueReport = new PsychopedagogueReport
            {
                ID = psychopedagogueReportId,
                UserID = psychopedagogueUser.Id,
                User = psychopedagogueUser,
                ReportID = reportId,
            };

            context.SocialWorkerReports.Add(socialWorkerReport);
            context.PsychologistReports.Add(psychologistReport);
            context.PsychopedagogueReports.Add(psychopedagogueReport);

            var report = new Report
            {
                ID = reportId,
                SocialWorkerReportID = socialWorkerReportId,
                PsychologistReportID = psychologistReportId,
                PsychopedagogueReportID = psychopedagogueReportId,
                SocialWorkerReport = socialWorkerReport,
                PsychologistReport = psychologistReport,
                PsychopedagogueReport = psychopedagogueReport,
                DocumentStatus = Status.None,
                SignatureProvider = SignatureProvider.NeckSign,
            };

            context.Reports.Add(report);
            context.SaveChanges();
            context.ChangeTracker.Clear();
        }

        /// <summary>
        /// Serializa um corpo de resposta <c>201 Created</c> no formato consumido por
        /// <c>NeckSignCreateEnvelopeResponse</c> (camelCase): <c>envelopeId</c>, <c>externalId</c>,
        /// <c>signingUrl</c> (nível de envelope) e <c>signers[]</c> com <c>externalId</c> e
        /// <c>signingUrl</c> por papel.
        /// </summary>
        private static string BuildCreatedEnvelopeJson(
            string envelopeId,
            string externalId,
            string topLevelSigningUrl,
            IReadOnlyList<(string ExternalId, string SigningUrl)> signers)
        {
            var body = new
            {
                envelopeId,
                externalId,
                status = "PENDING",
                signingUrl = topLevelSigningUrl,
                signers = signers.Select(s => new
                {
                    externalId = s.ExternalId,
                    email = (string?)null,
                    role = "SIGNER",
                    signingUrl = s.SigningUrl,
                }).ToArray(),
            };

            return JsonSerializer.Serialize(body, JsonOptions);
        }

        private static AppDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .EnableSensitiveDataLogging()
                .Options;

            return new AppDbContext(options);
        }

        private static NeckSignService BuildService(AppDbContext context, string responseJson)
        {
            var handler = new FakeHttpMessageHandler(responseJson);
            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://necksign.test/"),
            };
            var httpClientFactory = new FakeHttpClientFactory(httpClient);
            var pdfService = new FakePdfService();

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["NeckSign:BaseUrl"] = "https://necksign.test/",
                    ["NeckSign:ApiKey"] = "test-api-key",
                    ["NeckSign:WebhookUrl"] = "https://singular.test/api/webhooks/necksign",
                })
                .Build();

            return new NeckSignService(
                httpClientFactory,
                pdfService,
                context,
                NullLogger<NeckSignService>.Instance,
                configuration,
                new NoopServiceScopeFactory());
        }

        // --- Geradores FsCheck ---

        /// <summary>
        /// Container exposto ao atributo [Property] para registrar o Arbitrary customizado.
        /// </summary>
        public static class ReportSigningCaseArbContainer
        {
            public static Arbitrary<ReportSigningCase> ReportSigningCase() => ReportSigningCaseArb();
        }

        private static Arbitrary<ReportSigningCase> ReportSigningCaseArb()
        {
            // Gerador de URL de assinatura: segmento aleatório não vazio, garantindo URLs
            // distintas por papel para que uma troca de correlação seja detectável.
            var urlSegmentGen = Gen.Elements(
                    "abc", "x1y2", "signer", "url-42", "token99", "aeeDoc", "zz", "q7")
                .Two()
                .Select(pair => $"https://necksign.test/sign/{pair.Item1}-{pair.Item2}");

            // Permutações dos três signatários (índices 0,1,2) para variar a ordem no corpo 201.
            var permutations = new[]
            {
                new[] { 0, 1, 2 },
                new[] { 0, 2, 1 },
                new[] { 1, 0, 2 },
                new[] { 1, 2, 0 },
                new[] { 2, 0, 1 },
                new[] { 2, 1, 0 },
            };
            var orderGen = Gen.Elements(permutations);

            var gen = from social in urlSegmentGen
                      from psych in urlSegmentGen
                      from psychoped in urlSegmentGen
                      from order in orderGen
                      // Garante três URLs distintas para tornar a correlação por papel
                      // verificável sem ambiguidade.
                      where social != psych && psych != psychoped && social != psychoped
                      select new ReportSigningCase(social, psych, psychoped, order);

            return Arb.From(gen);
        }

        /// <summary>
        /// Caso de teste gerado: a signingUrl de cada papel e a ordem em que os signatários
        /// aparecem no corpo do 201.
        /// </summary>
        public sealed class ReportSigningCase
        {
            public string SocialWorkerUrl { get; }
            public string PsychologistUrl { get; }
            public string PsychopedagogueUrl { get; }
            public int[] SignerOrder { get; }

            public ReportSigningCase(
                string socialWorkerUrl,
                string psychologistUrl,
                string psychopedagogueUrl,
                int[] signerOrder)
            {
                SocialWorkerUrl = socialWorkerUrl;
                PsychologistUrl = psychologistUrl;
                PsychopedagogueUrl = psychopedagogueUrl;
                SignerOrder = signerOrder;
            }

            public override string ToString() =>
                $"ReportSigningCase(order=[{string.Join(",", SignerOrder)}], " +
                $"sw='{SocialWorkerUrl}', psi='{PsychologistUrl}', psiped='{PsychopedagogueUrl}')";
        }

        // --- Fakes de colaboradores externos ---

        /// <summary>HttpMessageHandler fake que sempre responde 201 com o corpo fornecido.</summary>
        private sealed class FakeHttpMessageHandler : HttpMessageHandler
        {
            private readonly string _responseJson;

            public FakeHttpMessageHandler(string responseJson) => _responseJson = responseJson;

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var response = new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = new StringContent(_responseJson, Encoding.UTF8, "application/json"),
                };
                return Task.FromResult(response);
            }
        }

        /// <summary>IHttpClientFactory fake que devolve sempre o HttpClient configurado.</summary>
        private sealed class FakeHttpClientFactory : IHttpClientFactory
        {
            private readonly HttpClient _httpClient;

            public FakeHttpClientFactory(HttpClient httpClient) => _httpClient = httpClient;

            public HttpClient CreateClient(string name) => _httpClient;
        }

        /// <summary>IPDFService stub: retorna um PDF mínimo válido (começa com `%PDF-`).</summary>
        private sealed class FakePdfService : SingularSystem.Interfaces.IPDFService
        {
            private static readonly byte[] MinimalPdf = Encoding.ASCII.GetBytes("%PDF-1.4\n%EOF\n");

            public Task<byte[]> GeneratePdf(Guid documentId) => Task.FromResult(MinimalPdf);
            public Task<byte[]> GeneratePdf(Guid documentId, SingularSystem.Models.PdfProfile profile) => Task.FromResult(MinimalPdf);

            public Task<string?> GetDocumentType(Guid documentId) => Task.FromResult<string?>("report");

            public Task<byte[]> GenerateStudentsPdf(string state, string city, string schoolID, string serie = "")
                => Task.FromResult(Array.Empty<byte>());

            public Task<byte[]> GenerateReportsAEEPdf(string state, string city, string schoolID, string disabilities, string disorders)
                => Task.FromResult(Array.Empty<byte>());

            public Task<byte[]> GenerateCaseStudiesTrackingPdf(string state, string city, string schoolID, string searchTerm, string status, int? year, int? month)
                => Task.FromResult(Array.Empty<byte>());

            public Task<byte[]> GenerateBiopsychosocialTrackingPdf(string state, string city, string schoolID, string searchTerm, string status, string pendingProfessional, int? year, int? month)
                => Task.FromResult(Array.Empty<byte>());
        }

        /// <summary>
        /// IServiceScopeFactory no-op exigido pelo construtor de <see cref="NeckSignService"/>.
        /// Não é usado por <c>CreateDoc</c>, apenas por <c>SyncPendingDocuments</c>.
        /// </summary>
        private sealed class NoopServiceScopeFactory : IServiceScopeFactory
        {
            public IServiceScope CreateScope() => new NoopServiceScope();

            private sealed class NoopServiceScope : IServiceScope
            {
                public IServiceProvider ServiceProvider { get; } = new NoopServiceProvider();

                public void Dispose() { }

                private sealed class NoopServiceProvider : IServiceProvider
                {
                    public object? GetService(Type serviceType) => null;
                }
            }
        }
    }
}
