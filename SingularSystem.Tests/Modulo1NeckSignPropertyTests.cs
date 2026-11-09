using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using FsCheck;
using FsCheck.Xunit;
using SingularSystem.DTOs;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;
using SingularSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace SingularSystem.Tests
{
    public sealed class Modulo1NeckSignPropertyTests
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        // Feature: modulo-1-infraestrutura, Property 2: SignatureProvider fixo em NeckSign
        //
        // Para todo tipo de documento suportado (report, pei, paee, caseStudy), quando a criação
        // do envelope é concluída com sucesso, a entidade persistida deve gravar SignatureProvider = NeckSign,
        // independentemente do tipo.
        //
        // Validates: Requirements 4.4
        [Property(MaxTest = 100, Arbitrary = new[] { typeof(DocTypeCaseArb) })]
        public Property SignatureProvider_always_fixed_to_NeckSign(DocTypeCase docCase)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"p2-{Guid.NewGuid()}")
                .Options;
            using var db = new AppDbContext(options);

            var docId = Guid.NewGuid();
            BaseDocument entity = docCase.Type switch
            {
                DocumentType.Report => new Report { ID = docId, DocumentStatus = Status.None, SignatureProvider = (SignatureProvider)0 },
                DocumentType.Pei => new PEI { ID = docId, DocumentStatus = Status.None, SignatureProvider = (SignatureProvider)0 },
                DocumentType.Paee => new PAEE { ID = docId, DocumentStatus = Status.None, SignatureProvider = (SignatureProvider)0 },
                DocumentType.CaseStudy => new RegularTeacherEvaluationDocument { ID = docId, DocumentStatus = Status.None, SignatureProvider = (SignatureProvider)0 },
                _ => new Report { ID = docId, DocumentStatus = Status.None, SignatureProvider = (SignatureProvider)0 }
            };

            var response = new NeckSignService.NeckSignCreateEnvelopeResponse
            {
                EnvelopeId = "env-" + Guid.NewGuid(),
                Status = "PENDING",
                SigningUrl = "https://sign.test/s/" + Guid.NewGuid()
            };

            var handler = new ProgrammableHttpMessageHandler(req => new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK));
            var service = NeckSignTestInfra.CreateService(db, handler);

            service.ApplyCreatedEnvelope(docCase.Type, entity, response, new List<NeckSignSignerInput>());

            var (provider, status, token) = entity switch
            {
                Report r => (r.SignatureProvider, r.DocumentStatus, r.DocumentToken),
                PEI p => (p.SignatureProvider, p.DocumentStatus, p.DocumentToken),
                PAEE pa => (pa.SignatureProvider, pa.DocumentStatus, pa.DocumentToken),
                RegularTeacherEvaluationDocument cs => (cs.SignatureProvider, cs.DocumentStatus, cs.DocumentToken),
                _ => throw new InvalidOperationException()
            };

            var isNeckSign = provider == SignatureProvider.NeckSign;
            var isInProgress = status == Status.InProgress;
            var hasToken = token == response.EnvelopeId;

            return (isNeckSign && isInProgress && hasToken).ToProperty()
                .Label($"DocType={docCase.Type}: Provider={provider}, Status={status}, Token={token}");
        }

        // Feature: modulo-1-infraestrutura, Property 3: Neutralidade do envelope de assinatura
        //
        // Para todo documento submetido à assinatura, o título/branding do envelope gerado não deve
        // conter nenhum termo de identificação municipal proibido (ex.: "IPAAGS", nomes de prefeituras/
        // municípios da lista de bloqueio), em comparação case-insensitive.
        //
        // Validates: Requirements 4.6
        [Property(MaxTest = 100, Arbitrary = new[] { typeof(EnvelopeTitleCaseArb) })]
        public Property Envelope_title_has_no_municipal_branding(EnvelopeTitleCase titleCase)
        {
            var entity = new Report
            {
                ID = titleCase.DocId,
                StudentID = titleCase.StudentId
            };

            var title = NeckSignService.BuildTitle(titleCase.DocType, entity);

            var forbiddenTerms = new[]
            {
                "ipaags", "prefeitura", "municipio", "município", "municipal",
                "secretaria", "governo", "teresina", "semec", "seduc"
            };

            var lowerTitle = title.ToLowerInvariant();
            var containsForbidden = forbiddenTerms.Any(term => lowerTitle.Contains(term));

            var isNonEmpty = !string.IsNullOrWhiteSpace(title);

            return (isNonEmpty && !containsForbidden).ToProperty()
                .Label($"Title='{title}' containsForbidden={containsForbidden}");
        }

        // Feature: modulo-1-infraestrutura, Property 4: Idempotência do processamento de webhook
        //
        // Para todo evento de webhook identificado pela tripla (Provider, EnvelopeOrDocumentToken, EventState),
        // processá-lo duas ou mais vezes deve produzir exatamente o mesmo estado final que processá-lo
        // uma única vez — nenhum efeito adicional é aplicado em reprocessamentos, graças à idempotência
        // transacional garantida pela UNIQUE de ProcessedWebhookEvent.
        //
        // Validates: Requirements 4.8
        [Property(MaxTest = 100, Arbitrary = new[] { typeof(WebhookTripleCaseArb) })]
        public Property Webhook_processing_is_idempotent_over_triple(WebhookTripleCase tc)
        {
            using var harness = new NeckSignControllerHarness();

            var docId = Guid.NewGuid();
            harness.Context.PEIs.Add(new PEI
            {
                ID = docId,
                DocumentToken = tc.EnvelopeId,
                DocumentStatus = Status.InProgress,
                SignatureProvider = SignatureProvider.NeckSign
            });
            harness.Context.SaveChanges();

            var payload = new
            {
                @event = tc.EventState,
                envelopeId = tc.EnvelopeId,
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
            var header = NeckSignControllerHarness.ComputeSignatureHeader(body, NeckSignControllerHarness.WebhookSecret);

            // Primeira execução
            var r1 = harness.PostWebhookAsync(body, header).GetAwaiter().GetResult();
            var statusAfterFirst = harness.Context.PEIs.AsNoTracking().First(p => p.ID == docId).DocumentStatus;
            var processedEventsCountFirst = harness.Context.ProcessedWebhookEvents.AsNoTracking().Count();

            // Reprocessamento (2ª e 3ª execuções)
            var r2 = harness.PostWebhookAsync(body, header).GetAwaiter().GetResult();
            var r3 = harness.PostWebhookAsync(body, header).GetAwaiter().GetResult();

            var statusAfterRepeat = harness.Context.PEIs.AsNoTracking().First(p => p.ID == docId).DocumentStatus;
            var processedEventsCountRepeat = harness.Context.ProcessedWebhookEvents.AsNoTracking().Count();

            var resultsOk = (r1 is OkResult or OkObjectResult) &&
                            (r2 is OkResult or OkObjectResult) &&
                            (r3 is OkResult or OkObjectResult);

            var stateUnchanged = statusAfterFirst == statusAfterRepeat;
            var singleProcessedRecord = processedEventsCountFirst == 1 && processedEventsCountRepeat == 1;

            return (resultsOk && stateUnchanged && singleProcessedRecord).ToProperty()
                .Label($"Event={tc.EventState}, Env={tc.EnvelopeId}: rOk={resultsOk}, stateUnchanged={stateUnchanged}, dedupCount={processedEventsCountRepeat}");
        }

        // --- Geradores ---

        public sealed class DocTypeCase
        {
            public DocumentType Type { get; }
            public DocTypeCase(DocumentType type) => Type = type;
        }

        public static class DocTypeCaseArb
        {
            public static Arbitrary<DocTypeCase> Arbitrary()
            {
                var types = new[] { DocumentType.Report, DocumentType.Pei, DocumentType.Paee, DocumentType.CaseStudy };
                return Gen.Elements(types)
                    .Select(t => new DocTypeCase(t))
                    .ToArbitrary();
            }
        }

        public sealed class EnvelopeTitleCase
        {
            public DocumentType DocType { get; }
            public Guid DocId { get; }
            public Guid? StudentId { get; }

            public EnvelopeTitleCase(DocumentType docType, Guid docId, Guid? studentId)
            {
                DocType = docType;
                DocId = docId;
                StudentId = studentId;
            }
        }

        public static class EnvelopeTitleCaseArb
        {
            public static Arbitrary<EnvelopeTitleCase> Arbitrary()
            {
                var types = new[] { DocumentType.Report, DocumentType.Pei, DocumentType.Paee, DocumentType.CaseStudy };
                return (from dt in Gen.Elements(types)
                        from docId in Arb.Default.Guid().Generator
                        from hasStudent in Arb.Default.Bool().Generator
                        from studentId in Arb.Default.Guid().Generator
                        select new EnvelopeTitleCase(dt, docId, hasStudent ? studentId : null))
                    .ToArbitrary();
            }
        }

        public sealed class WebhookTripleCase
        {
            public string EnvelopeId { get; }
            public string EventState { get; }

            public WebhookTripleCase(string envelopeId, string eventState)
            {
                EnvelopeId = envelopeId;
                EventState = eventState;
            }
        }

        public static class WebhookTripleCaseArb
        {
            public static Arbitrary<WebhookTripleCase> Arbitrary()
            {
                var events = new[] { "envelope.signed", "envelope.declined", "envelope.canceled" };
                return (from ev in Gen.Elements(events)
                        from id in Arb.Default.NonEmptyString().Generator
                        let cleanId = "env-" + new string(id.Get.Where(char.IsLetterOrDigit).Take(12).ToArray())
                        select new WebhookTripleCase(cleanId.Length > 4 ? cleanId : "env-default", ev))
                    .ToArbitrary();
            }
        }
    }
}
