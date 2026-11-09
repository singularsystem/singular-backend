using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FsCheck;
using FsCheck.Xunit;
using SingularSystem.DTOs;
using SingularSystem.Interfaces;
using SingularSystem.Models.Entities;
using SingularSystem.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace SingularSystem.Tests
{
    // Feature: 012-ipaags-migration, Property 2: Composição e filtragem de signatários
    //
    // Para qualquer conjunto de papéis/responsável de um documento, a lista `signers`
    // produzida por NeckSignService.ComposeSignersAsync deve conter apenas entradas cujos
    // nome E e-mail sejam não vazios, sem dois signatários com o mesmo e-mail (dedup
    // case-insensitive); e quando nenhuma entrada qualifica (lista vazia), CreateDoc deve
    // retornar false sem enviar requisição ao NeckSign — o que é sinalizado justamente pela
    // lista de signatários vazia. Para `report`, no máximo os três papéis; para
    // `pei`/`paee`/`caseStudy`, no máximo o responsável.
    //
    // A unidade sob teste é a composição interna de signatários (ComposeSignersAsync), que é
    // o ponto único consumido por CreateDoc para montar o corpo do POST /api/v1/envelopes.
    // Um AppDbContext EF Core InMemory é semeado por iteração com os dados gerados; nenhum
    // HTTP é feito durante a composição.
    //
    // Validates: Requirements 2.5, 2.6, 2.7
    public sealed class NeckSignSignerCompositionPropertyTests
    {
        [Property(MaxTest = 100, Arbitrary = new[] { typeof(SignerCaseArbContainer) })]
        public Property ComposeSigners_includes_only_fully_filled_and_deduped_signers(SignerCompositionCase testCase)
        {
            // Arrange: banco InMemory isolado por iteração + entidades semeadas conforme o caso.
            using var context = CreateInMemoryContext();
            var idDoc = Guid.NewGuid();

            DocumentType documentType;
            if (testCase.IsReport)
            {
                documentType = DocumentType.Report;
                SeedReport(context, idDoc, testCase.Roles);
            }
            else
            {
                documentType = DocumentType.Pei;
                SeedPei(context, idDoc, testCase.Roles.FirstOrDefault());
            }
            context.SaveChanges();

            var service = CreateService(context);

            // Act
            IReadOnlyList<NeckSignSignerInput> signers;
            try
            {
                signers = service.ComposeSignersAsync(documentType, idDoc).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                return false.ToProperty().Label($"ComposeSignersAsync propagou exceção: {ex.Message}");
            }

            // Oráculo independente: quais papéis/responsável deveriam qualificar, com dedup por e-mail.
            var expectedEmails = ExpectedIncludedEmails(testCase);

            // 1) Toda entrada deve ter nome E e-mail não vazios (Req 2.5, 2.6).
            bool allFilled = signers.All(s =>
                !string.IsNullOrWhiteSpace(s.Name) && !string.IsNullOrWhiteSpace(s.Email));

            // 2) Não pode haver dois signatários com o mesmo e-mail (dedup case-insensitive) (Req 2.5).
            var distinctEmailCount = signers
                .Select(s => s.Email.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();
            bool noDuplicateEmails = distinctEmailCount == signers.Count;

            // 3) Limite por tipo: report ≤ 3; pei/paee/caseStudy ≤ 1 (Req 2.5, 2.6).
            int maxAllowed = testCase.IsReport ? 3 : 1;
            bool withinLimit = signers.Count <= maxAllowed;

            // 4) O conjunto de e-mails produzidos deve ser exatamente o esperado pelo oráculo.
            var producedEmails = signers
                .Select(s => s.Email.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            bool matchesExpected = producedEmails.SetEquals(expectedEmails);

            // 5) Quando nada qualifica, a lista é VAZIA (sinaliza CreateDoc → false sem request) (Req 2.7).
            bool emptinessWhenNothingQualifies =
                expectedEmails.Count != 0 || signers.Count == 0;

            var holds = allFilled && noDuplicateEmails && withinLimit && matchesExpected && emptinessWhenNothingQualifies;

            return holds.ToProperty()
                .Label(
                    $"tipo={(testCase.IsReport ? "report" : "pei")} " +
                    $"produzidos=[{string.Join(", ", producedEmails)}] " +
                    $"esperados=[{string.Join(", ", expectedEmails)}] " +
                    $"allFilled={allFilled} noDup={noDuplicateEmails} withinLimit={withinLimit} " +
                    $"matches={matchesExpected} emptyWhenNone={emptinessWhenNothingQualifies}");
        }

        /// <summary>
        /// Oráculo independente da implementação: calcula o conjunto de e-mails que devem ser
        /// incluídos, aplicando as regras "nome E e-mail não vazios" e dedup por e-mail
        /// (case-insensitive), respeitando a ordem de avaliação dos papéis.
        /// </summary>
        private static HashSet<string> ExpectedIncludedEmails(SignerCompositionCase testCase)
        {
            var included = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var roles = testCase.IsReport
                ? testCase.Roles
                : testCase.Roles.Take(1).ToList();

            foreach (var role in roles)
            {
                if (string.IsNullOrWhiteSpace(role.Name) || string.IsNullOrWhiteSpace(role.Email))
                    continue;

                included.Add(role.Email.Trim());
            }

            return included;
        }

        private static AppDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"necksign-signers-{Guid.NewGuid()}")
                .EnableSensitiveDataLogging()
                .Options;

            return new AppDbContext(options);
        }

        private static NeckSignService CreateService(AppDbContext context)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["NeckSign:BaseUrl"] = "https://necksign.test/",
                    ["NeckSign:ApiKey"] = "test-api-key",
                    ["NeckSign:WebhookUrl"] = "https://ipaags.test/api/v1/necksign/webhook",
                })
                .Build();

            return new NeckSignService(
                new StubHttpClientFactory(),
                new StubPdfService(),
                context,
                NullLogger<NeckSignService>.Instance,
                configuration,
                new StubServiceScopeFactory());
        }

        /// <summary>
        /// Semeia um Report com os três papéis (assistente social, psicólogo, psicopedagogo),
        /// cada um ligado a um ApplicationUser cujos FullName/Email vêm do caso gerado. O
        /// ExternalId de cada signatário corresponde ao ID do respectivo sub-report (fallback
        /// pelo User do sub-report, sem semear os questionários).
        /// </summary>
        private static void SeedReport(AppDbContext context, Guid idDoc, IReadOnlyList<SignerRole> roles)
        {
            var socialWorker = roles.ElementAtOrDefault(0);
            var psychologist = roles.ElementAtOrDefault(1);
            var psychopedagogue = roles.ElementAtOrDefault(2);

            var swReport = new SocialWorkerReport { ID = Guid.NewGuid(), User = MakeUser(socialWorker) };
            var psyReport = new PsychologistReport { ID = Guid.NewGuid(), User = MakeUser(psychologist) };
            var psychopedReport = new PsychopedagogueReport { ID = Guid.NewGuid(), User = MakeUser(psychopedagogue) };

            var report = new Report
            {
                ID = idDoc,
                SocialWorkerReport = swReport,
                SocialWorkerReportID = swReport.ID,
                PsychologistReport = psyReport,
                PsychologistReportID = psyReport.ID,
                PsychopedagogueReport = psychopedReport,
                PsychopedagogueReportID = psychopedReport.ID,
            };

            context.Reports.Add(report);
        }

        /// <summary>
        /// Semeia um PEI com o responsável (PEI.User) cujos FullName/Email vêm do caso gerado.
        /// </summary>
        private static void SeedPei(AppDbContext context, Guid idDoc, SignerRole? responsible)
        {
            var pei = new PEI
            {
                ID = idDoc,
                User = MakeUser(responsible),
            };

            context.PEIs.Add(pei);
        }

        /// <summary>
        /// Cria um ApplicationUser a partir de um papel gerado. Um papel nulo representa a
        /// ausência total do usuário (navegação null), caso que também deve ser filtrado.
        /// </summary>
        private static ApplicationUser? MakeUser(SignerRole? role)
        {
            if (role is null)
                return null;

            return new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = Guid.NewGuid().ToString("N") + "@user.test",
                FullName = role.Name ?? string.Empty,
                Email = role.Email,
            };
        }

        // ----------------------------------------------------------------------------------
        // Geradores
        // ----------------------------------------------------------------------------------

        /// <summary>
        /// Gerador inteligente que constrói casos de composição cobrindo: campos faltantes
        /// (nome e/ou e-mail nulo/vazio/só-espaços), e-mails duplicados (inclusive variando o
        /// casing para exercitar a dedup case-insensitive) e papéis totalmente ausentes.
        /// Restringe o espaço de entrada ao relevante para manter o custo das 100+ iterações baixo.
        /// </summary>
        private static Arbitrary<SignerCompositionCase> SignerCaseArb()
        {
            // Pool pequeno de e-mails base para forçar duplicatas com frequência.
            string[] baseEmails = { "ana@aee.test", "bruno@aee.test", "carla@aee.test" };

            Gen<string?> nameGen = Gen.Frequency(
                Tuple.Create(5, Gen.Elements<string?>("Ana Souza", "Bruno Lima", "Carla Dias")),
                Tuple.Create(2, Gen.Constant<string?>(null)),
                Tuple.Create(1, Gen.Constant<string?>(string.Empty)),
                Tuple.Create(1, Gen.Elements<string?>("   ", "\t")));

            // E-mail: às vezes um dos e-mails base (gera duplicatas), às vezes com casing
            // alterado (dedup case-insensitive), às vezes faltante.
            Gen<string?> emailGen = Gen.Frequency(
                Tuple.Create(4, Gen.Elements<string?>(baseEmails)),
                Tuple.Create(2, Gen.Elements<string?>(baseEmails).Select(e => e?.ToUpperInvariant())),
                Tuple.Create(2, Gen.Constant<string?>(null)),
                Tuple.Create(1, Gen.Constant<string?>(string.Empty)),
                Tuple.Create(1, Gen.Elements<string?>("  ", "\n")));

            // Um papel: ou totalmente ausente (null), ou um par (nome, e-mail) possivelmente parcial.
            Gen<SignerRole?> roleGen = Gen.Frequency(
                Tuple.Create(1, Gen.Constant<SignerRole?>(null)),
                Tuple.Create(6, nameGen.SelectMany(n => emailGen.Select(e => (SignerRole?)new SignerRole(n, e)))));

            // report: 3 papéis; pei: 1 responsável (usamos a mesma estrutura, variando IsReport).
            var reportCaseGen =
                from r0 in roleGen
                from r1 in roleGen
                from r2 in roleGen
                select new SignerCompositionCase(true, ToRoleList(r0, r1, r2));

            var singleCaseGen =
                from r0 in roleGen
                select new SignerCompositionCase(false, ToRoleList(r0));

            var gen = Gen.Frequency(
                Tuple.Create(3, reportCaseGen),
                Tuple.Create(2, singleCaseGen));

            return Arb.From(gen);
        }

        private static List<SignerRole> ToRoleList(params SignerRole?[] roles)
        {
            // Preserva as posições: um papel null vira um marcador "ausente" (usuário null).
            // Mantemos a lista com o mesmo comprimento para o oráculo respeitar a ordem.
            return roles.Select(r => r ?? SignerRole.Absent).ToList();
        }

        public static class SignerCaseArbContainer
        {
            public static Arbitrary<SignerCompositionCase> SignerCompositionCase() => SignerCaseArb();
        }

        /// <summary>
        /// Caso gerado: se é um report (3 papéis) ou um documento de responsável único (1 papel),
        /// e a lista ordenada de papéis.
        /// </summary>
        public sealed class SignerCompositionCase
        {
            public bool IsReport { get; }
            public IReadOnlyList<SignerRole> Roles { get; }

            public SignerCompositionCase(bool isReport, IReadOnlyList<SignerRole> roles)
            {
                IsReport = isReport;
                Roles = roles;
            }

            public override string ToString()
            {
                var rolesText = string.Join(" | ", Roles.Select(r => r.ToString()));
                return $"Case(report={IsReport}, roles=[{rolesText}])";
            }
        }

        /// <summary>
        /// Dados brutos de um papel/responsável gerados pelo FsCheck. Name/Email podem ser
        /// null/vazio/só-espaços. <see cref="Absent"/> representa a ausência do usuário
        /// (navegação null na entidade).
        /// </summary>
        public sealed class SignerRole
        {
            public string? Name { get; }
            public string? Email { get; }
            public bool IsAbsent { get; }

            public SignerRole(string? name, string? email)
            {
                Name = name;
                Email = email;
                IsAbsent = false;
            }

            private SignerRole()
            {
                IsAbsent = true;
            }

            public static readonly SignerRole Absent = new();

            public override string ToString()
            {
                if (IsAbsent)
                    return "<absent>";
                return $"({Describe(Name)}, {Describe(Email)})";
            }

            private static string Describe(string? value)
            {
                if (value is null) return "<null>";
                if (value.Length == 0) return "<empty>";
                if (string.IsNullOrWhiteSpace(value)) return "<ws>";
                return value;
            }
        }

        // ----------------------------------------------------------------------------------
        // Stubs (sem HTTP real; a composição não faz chamadas de rede nem gera PDF)
        // ----------------------------------------------------------------------------------

        private sealed class StubHttpClientFactory : IHttpClientFactory
        {
            public HttpClient CreateClient(string name) => new HttpClient
            {
                BaseAddress = new Uri("https://necksign.test/")
            };
        }

        /// <summary>
        /// Scope factory stub. ComposeSignersAsync usa o <see cref="AppDbContext"/> injetado
        /// diretamente e não aciona o scope factory (usado apenas por SyncPendingDocuments),
        /// portanto este stub nunca é efetivamente consumido durante a composição.
        /// </summary>
        private sealed class StubServiceScopeFactory : IServiceScopeFactory
        {
            public IServiceScope CreateScope() =>
                throw new InvalidOperationException(
                    "O scope factory não deve ser usado durante ComposeSignersAsync.");
        }

        private sealed class StubPdfService : IPDFService
        {
            public Task<byte[]> GeneratePdf(Guid documentId) => Task.FromResult(Array.Empty<byte>());

            public Task<string?> GetDocumentType(Guid documentId) => Task.FromResult<string?>(null);

            public Task<byte[]> GenerateStudentsPdf(string state, string city, string schoolID, string serie = "")
                => Task.FromResult(Array.Empty<byte>());

            public Task<byte[]> GenerateReportsAEEPdf(string state, string city, string schoolID, string disabilities, string disorders)
                => Task.FromResult(Array.Empty<byte>());

            public Task<byte[]> GenerateCaseStudiesTrackingPdf(string state, string city, string schoolID, string searchTerm, string status, int? year, int? month)
                => Task.FromResult(Array.Empty<byte>());

            public Task<byte[]> GenerateBiopsychosocialTrackingPdf(string state, string city, string schoolID, string searchTerm, string status, string pendingProfessional, int? year, int? month)
                => Task.FromResult(Array.Empty<byte>());
        }
    }
}
