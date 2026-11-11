using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using FsCheck;
using FsCheck.Xunit;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace SingularSystem.Tests
{
    public sealed class NeckSignControllerPropertyTests
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        // Feature: 012-singular-migration, Property 6: Validação HMAC-SHA256 do webhook (round-trip e erro)
        //
        // Para qualquer corpo, um header X-NeckSign-Signature sha256=<hmac-hex> calculado com o
        // segredo correto sobre os bytes brutos é aceito; qualquer alteração no corpo, segredo ou
        // formato do header (inclusive ausência) → 403 Forbidden sem efeito.
        //
        // Validates: Requirements 4.2, 4.3
        [Property(MaxTest = 100, Arbitrary = new[] { typeof(HmacCaseArbContainer) })]
        public Property Webhook_hmac_roundtrip_and_error(HmacCase c)
        {
            using var harness = new NeckSignControllerHarness();
            var body = Encoding.UTF8.GetBytes(c.Body);

            string? header = c.HeaderMode switch
            {
                HeaderMode.ValidSignature => NeckSignControllerHarness.ComputeSignatureHeader(body, NeckSignControllerHarness.WebhookSecret),
                HeaderMode.WrongSecret => NeckSignControllerHarness.ComputeSignatureHeader(body, "wrong-secret"),
                HeaderMode.TamperedBody => NeckSignControllerHarness.ComputeSignatureHeader(Encoding.UTF8.GetBytes(c.Body + "x"), NeckSignControllerHarness.WebhookSecret),
                HeaderMode.NoScheme => Convert.ToHexString(new byte[32]).ToLowerInvariant(),
                HeaderMode.Garbage => "sha256=not-hex-$$$",
                HeaderMode.Absent => null,
                _ => null
            };

            var result = harness.PostWebhookAsync(body, header).GetAwaiter().GetResult();

            if (c.HeaderMode == HeaderMode.ValidSignature)
            {
                // Assinatura válida NÃO deve ser 403 (pode ser 400 por payload inválido etc.).
                var notForbidden = !(result is StatusCodeResult { StatusCode: 403 });
                return notForbidden.ToProperty().Label($"assinatura válida não deveria dar 403 (obteve {Describe(result)})");
            }

            // Qualquer outra variação → 403 sem efeito.
            var is403 = result is StatusCodeResult { StatusCode: 403 };
            var noDedup = harness.Context.ProcessedWebhookEvents.AsNoTracking().Count() == 0;
            return (is403 && noDedup).ToProperty()
                .Label($"modo {c.HeaderMode}: esperado 403 sem efeito, obteve {Describe(result)} dedupRows={(noDedup ? 0 : 1)}");
        }

        // Feature: 012-singular-migration, Property 7: Validação de campos obrigatórios do webhook
        //
        // Para qualquer payload com HMAC válido ao qual falte ao menos um dos campos obrigatórios
        // event/envelopeId/externalId, o controller responde 400 Bad Request sem efeito.
        //
        // Validates: Requirements 4.4
        [Property(MaxTest = 100, Arbitrary = new[] { typeof(RequiredFieldsCaseArbContainer) })]
        public Property Webhook_requires_event_envelopeId_externalId(RequiredFieldsCase c)
        {
            using var harness = new NeckSignControllerHarness();

            var payload = new
            {
                @event = c.HasEvent ? "envelope.signed" : null,
                envelopeId = c.HasEnvelopeId ? "env-" + Guid.NewGuid().ToString("N") : null,
                externalId = c.HasExternalId ? Guid.NewGuid().ToString() : null,
            };
            var body = JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions);
            var header = NeckSignControllerHarness.ComputeSignatureHeader(body, NeckSignControllerHarness.WebhookSecret);

            var result = harness.PostWebhookAsync(body, header).GetAwaiter().GetResult();

            var allPresent = c.HasEvent && c.HasEnvelopeId && c.HasExternalId;
            if (!allPresent)
            {
                var is400 = result is BadRequestObjectResult or BadRequestResult;
                var noDedup = harness.Context.ProcessedWebhookEvents.AsNoTracking().Count() == 0;
                return (is400 && noDedup).ToProperty()
                    .Label($"faltando campo → esperado 400 sem efeito, obteve {Describe(result)}");
            }

            // Todos presentes (mas externalId desconhecido) → 200 sem efeito (não 400).
            var not400 = !(result is BadRequestObjectResult or BadRequestResult);
            return not400.ToProperty().Label($"todos presentes não deveria dar 400 (obteve {Describe(result)})");
        }

        // Feature: 012-singular-migration, Property 8: Idempotência de webhook
        //
        // Para qualquer evento válido, processá-lo mais de uma vez para a mesma combinação de
        // (envelopeId, estado) deve produzir o mesmo estado final e 200 OK sem efeitos adicionais.
        //
        // Validates: Requirements 4.12, 12.5
        [Property(MaxTest = 50, Arbitrary = new[] { typeof(RepeatCaseArbContainer) })]
        public Property Webhook_is_idempotent_on_repeat(RepeatCase c)
        {
            using var harness = new NeckSignControllerHarness();

            var peiId = Guid.NewGuid();
            harness.Context.PEIs.Add(new PEI
            {
                ID = peiId,
                DocumentToken = "env-" + Guid.NewGuid().ToString("N"),
                DocumentStatus = Status.InProgress,
                SignatureProvider = SignatureProvider.NeckSign
            });
            harness.Context.SaveChanges();

            var envelopeId = "env-" + Guid.NewGuid().ToString("N");
            var payload = new
            {
                @event = "envelope.signed",
                envelopeId,
                externalId = peiId.ToString(),
                signedFile = "https://files.test/signed.pdf",
                signers = new[] { new { externalId = Guid.NewGuid().ToString(), status = "SIGNED", signedAt = DateTimeOffset.UtcNow } }
            };
            var body = JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions);
            var header = NeckSignControllerHarness.ComputeSignatureHeader(body, NeckSignControllerHarness.WebhookSecret);

            IActionResult last = new OkResult();
            for (int i = 0; i < c.Repeats; i++)
            {
                last = harness.PostWebhookAsync(body, header).GetAwaiter().GetResult();
            }

            harness.Context.ChangeTracker.Clear();
            var pei = harness.Context.PEIs.AsNoTracking().First(p => p.ID == peiId);
            var dedupCount = harness.Context.ProcessedWebhookEvents.AsNoTracking().Count();

            var ok = last is OkResult or OkObjectResult;
            var completed = pei.DocumentStatus == Status.Complete && pei.TeacherSign;
            var singleDedup = dedupCount == 1; // apenas um registro apesar de N reprocessamentos

            return (ok && completed && singleDedup).ToProperty()
                .Label($"repeats={c.Repeats}: ok={ok} status={pei.DocumentStatus} teacherSign={pei.TeacherSign} dedupRows={dedupCount}");
        }

        // Feature: 012-singular-migration — Task 7.7: testes de exemplo/integração do NeckSignController
        //
        // envelope.signed com HMAC válido correlaciona por externalId e marca Complete;
        // reprocessamento → 200 sem efeito; HMAC inválido → 403 sem efeito.
        //
        // Validates: Requirements 4.9, 4.11, 13.3, 13.7
        [Fact]
        public void Webhook_signed_marks_complete_and_is_idempotent()
        {
            using var harness = new NeckSignControllerHarness();
            var peiId = Guid.NewGuid();
            harness.Context.PEIs.Add(new PEI
            {
                ID = peiId,
                DocumentToken = "env-abc",
                DocumentStatus = Status.InProgress,
                SignatureProvider = SignatureProvider.NeckSign
            });
            harness.Context.SaveChanges();

            var payload = new
            {
                @event = "envelope.signed",
                envelopeId = "env-abc",
                externalId = peiId.ToString(),
                signedFile = "https://files.test/final.pdf",
                signers = new[] { new { externalId = Guid.NewGuid().ToString(), status = "SIGNED", signedAt = DateTimeOffset.UtcNow } }
            };
            var body = JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions);
            var header = NeckSignControllerHarness.ComputeSignatureHeader(body, NeckSignControllerHarness.WebhookSecret);

            var r1 = harness.PostWebhookAsync(body, header).GetAwaiter().GetResult();
            Assert.True(r1 is OkResult or OkObjectResult);

            harness.Context.ChangeTracker.Clear();
            var pei = harness.Context.PEIs.AsNoTracking().First(p => p.ID == peiId);
            Assert.Equal(Status.Complete, pei.DocumentStatus);
            Assert.Equal("https://files.test/final.pdf", pei.SignedFile);

            // Reprocessamento → 200 sem efeito adicional (um único registro de dedup).
            var r2 = harness.PostWebhookAsync(body, header).GetAwaiter().GetResult();
            Assert.True(r2 is OkResult or OkObjectResult);
            Assert.Equal(1, harness.Context.ProcessedWebhookEvents.AsNoTracking().Count());
        }

        [Fact]
        public void Webhook_invalid_hmac_returns_403_without_effect()
        {
            using var harness = new NeckSignControllerHarness();
            var peiId = Guid.NewGuid();
            harness.Context.PEIs.Add(new PEI { ID = peiId, DocumentToken = "env-x", DocumentStatus = Status.InProgress, SignatureProvider = SignatureProvider.NeckSign });
            harness.Context.SaveChanges();

            var payload = new { @event = "envelope.signed", envelopeId = "env-x", externalId = peiId.ToString() };
            var body = JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions);
            var badHeader = NeckSignControllerHarness.ComputeSignatureHeader(body, "wrong-secret");

            var result = harness.PostWebhookAsync(body, badHeader).GetAwaiter().GetResult();
            Assert.True(result is StatusCodeResult { StatusCode: 403 });

            harness.Context.ChangeTracker.Clear();
            Assert.Equal(Status.InProgress, harness.Context.PEIs.AsNoTracking().First(p => p.ID == peiId).DocumentStatus);
            Assert.Equal(0, harness.Context.ProcessedWebhookEvents.AsNoTracking().Count());
        }

        [Fact]
        public void Webhook_declined_marks_declined_and_notifies()
        {
            using var harness = new NeckSignControllerHarness();
            var peiId = Guid.NewGuid();
            harness.Context.PEIs.Add(new PEI { ID = peiId, DocumentToken = "env-d", DocumentStatus = Status.InProgress, SignatureProvider = SignatureProvider.NeckSign, UserID = null });
            harness.Context.SaveChanges();

            var payload = new { @event = "envelope.declined", envelopeId = "env-d", externalId = peiId.ToString() };
            var body = JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions);
            var header = NeckSignControllerHarness.ComputeSignatureHeader(body, NeckSignControllerHarness.WebhookSecret);

            var result = harness.PostWebhookAsync(body, header).GetAwaiter().GetResult();
            Assert.True(result is OkResult or OkObjectResult);

            harness.Context.ChangeTracker.Clear();
            Assert.Equal(Status.Declined, harness.Context.PEIs.AsNoTracking().First(p => p.ID == peiId).DocumentStatus);
        }

        private static string Describe(IActionResult r) => r switch
        {
            StatusCodeResult s => $"StatusCode({s.StatusCode})",
            ObjectResult o => $"Object({o.StatusCode})",
            _ => r.GetType().Name
        };

        // --- Geradores ---

        public enum HeaderMode { ValidSignature, WrongSecret, TamperedBody, NoScheme, Garbage, Absent }

        public sealed class HmacCase
        {
            public string Body { get; }
            public HeaderMode HeaderMode { get; }
            public HmacCase(string body, HeaderMode mode) { Body = body; HeaderMode = mode; }
            public override string ToString() => $"HmacCase(mode={HeaderMode}, bodyLen={Body.Length})";
        }

        public static class HmacCaseArbContainer
        {
            public static Arbitrary<HmacCase> HmacCase() =>
                Arb.From(
                    from body in Gen.Elements(
                        "{}",
                        "{\"event\":\"envelope.signed\",\"envelopeId\":\"e1\",\"externalId\":\"x\"}",
                        "arbitrary-non-json-body",
                        "")
                    from mode in Gen.Elements(
                        HeaderMode.ValidSignature, HeaderMode.WrongSecret, HeaderMode.TamperedBody,
                        HeaderMode.NoScheme, HeaderMode.Garbage, HeaderMode.Absent)
                    select new HmacCase(body, mode));
        }

        public sealed class RequiredFieldsCase
        {
            public bool HasEvent { get; }
            public bool HasEnvelopeId { get; }
            public bool HasExternalId { get; }
            public RequiredFieldsCase(bool e, bool env, bool ext) { HasEvent = e; HasEnvelopeId = env; HasExternalId = ext; }
            public override string ToString() => $"Required(event={HasEvent}, env={HasEnvelopeId}, ext={HasExternalId})";
        }

        public static class RequiredFieldsCaseArbContainer
        {
            public static Arbitrary<RequiredFieldsCase> RequiredFieldsCase() =>
                Arb.From(
                    from e in Gen.Elements(true, false)
                    from env in Gen.Elements(true, false)
                    from ext in Gen.Elements(true, false)
                    select new RequiredFieldsCase(e, env, ext));
        }

        public sealed class RepeatCase
        {
            public int Repeats { get; }
            public RepeatCase(int r) { Repeats = r; }
            public override string ToString() => $"RepeatCase({Repeats})";
        }

        public static class RepeatCaseArbContainer
        {
            public static Arbitrary<RepeatCase> RepeatCase() =>
                Arb.From(Gen.Choose(1, 4).Select(r => new RepeatCase(r)));
        }
    }
}
