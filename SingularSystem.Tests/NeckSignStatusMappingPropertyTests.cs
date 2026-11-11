using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using FsCheck;
using FsCheck.Xunit;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace SingularSystem.Tests
{
    public sealed class NeckSignStatusMappingPropertyTests
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        // Feature: 012-singular-migration, Property 4: Mapeamento de status do provedor para campos locais
        //
        // Para qualquer status de envelope do NeckSign, a sincronização de estado local deve seguir:
        // PENDING/SIGNING → InProgress; SIGNED → Complete com SignedAt/SignedFile preenchidos;
        // DECLINED → Declined; EXPIRED → Expired. Para report, os campos por papel são atualizados
        // por externalId do signatário e DocumentStatus só vira Complete com os três papéis assinados,
        // permanecendo InProgress enquanto houver papel sem assinar — nunca Complete em DECLINED/EXPIRED.
        // Para report em DECLINED/EXPIRED, DocumentStatus torna-se terminal independentemente de papéis
        // assinados, e campos por papel já preenchidos são preservados (nunca zerados).
        //
        // Validates: Requirements 3.6, 4.5, 4.6, 4.7, 4.8, 8.2, 8.3, 8.4, 16.5
        [Property(MaxTest = 100, Arbitrary = new[] { typeof(SingleStatusCaseArbContainer) })]
        public Property Sync_maps_single_signer_status_to_local_fields(SingleStatusCase c)
        {
            using var context = NeckSignTestInfra.CreateContext();
            var id = Guid.NewGuid();
            var token = "env-" + Guid.NewGuid().ToString("N");

            context.PEIs.Add(new PEI
            {
                ID = id,
                DocumentToken = token,
                DocumentStatus = Status.InProgress,
                SignatureProvider = SignatureProvider.NeckSign
            });
            context.SaveChanges();
            context.ChangeTracker.Clear();

            var json = BuildEnvelopeJson(id, c.ProviderStatus, signedFile: c.ProviderStatus == "SIGNED" ? "https://files.test/x.pdf" : null, signerSigned: c.ProviderStatus == "SIGNED");
            var handler = ProgrammableHttpMessageHandler.Json(HttpStatusCode.OK, json);
            var service = NeckSignTestInfra.CreateService(context, handler);

            var ok = service.SyncDocument(token).GetAwaiter().GetResult();

            var pei = context.PEIs.AsNoTracking().First(p => p.ID == id);
            var expected = ExpectedLocalStatus(c.ProviderStatus);

            // VOIDED → sem alteração (preserva InProgress).
            var statusOk = (pei.DocumentStatus == expected).ToProperty()
                .Label($"status provedor '{c.ProviderStatus}' → esperado {expected}, obteve {pei.DocumentStatus}");

            var signedFieldsOk = c.ProviderStatus switch
            {
                // SIGNED preenche TeacherSign/SignedAt, mas NÃO persiste SignedFile: a URL do PDF
                // assinado do NeckSign é efêmera (presigned, TTL ~1h) e resolvida sempre na hora via
                // GetDocumentDetails → /download; persistí-la quebraria após expirar e seria re-assinada
                // indevidamente como "S3 próprio". Logo, SignedFile permanece nulo para documentos NeckSign.
                "SIGNED" => (pei.TeacherSign && pei.SignedAt != null && pei.SignedFile == null).ToProperty()
                    .Label("SIGNED deveria preencher TeacherSign/SignedAt e NÃO persistir SignedFile"),
                _ => (pei.SignedFile == null && pei.SignedAt == null && pei.TeacherSign == false).ToProperty()
                    .Label($"'{c.ProviderStatus}' não deveria preencher campos de assinatura")
            };

            return ok.ToProperty().Label("SyncDocument deveria retornar true").And(statusOk).And(signedFieldsOk);
        }

        // Property 4 (report): terminal DECLINED/EXPIRED preserva campos por papel; Complete só com 3.
        [Property(MaxTest = 100, Arbitrary = new[] { typeof(ReportStatusCaseArbContainer) })]
        public Property Sync_maps_report_status_preserving_per_role_fields(ReportStatusCase c)
        {
            using var context = NeckSignTestInfra.CreateContext();
            var reportId = Guid.NewGuid();
            var swId = Guid.NewGuid();
            var psiId = Guid.NewGuid();
            var ppId = Guid.NewGuid();
            var token = "env-" + Guid.NewGuid().ToString("N");

            // Pré-preenche alguns campos por papel para checar preservação em estados terminais.
            var preSocialSign = c.PreSignedSocial;
            context.Reports.Add(new Report
            {
                ID = reportId,
                DocumentToken = token,
                DocumentStatus = Status.InProgress,
                SignatureProvider = SignatureProvider.NeckSign,
                SocialWorkerReportID = swId,
                PsychologistReportID = psiId,
                PsychopedagogueReportID = ppId,
                SocialWorkerSign = preSocialSign,
                SocialWorkerSignedAt = preSocialSign ? DateTime.UtcNow.AddDays(-1) : null,
            });
            context.SaveChanges();
            context.ChangeTracker.Clear();

            // No SIGNED, decide quantos papéis assinam conforme o caso.
            var signedRoles = c.ProviderStatus == "SIGNED" ? c.SignedRoleCount : 0;
            var roleIds = new[] { swId, psiId, ppId };
            var json = BuildReportEnvelopeJson(reportId, c.ProviderStatus, roleIds, signedRoles);
            var handler = ProgrammableHttpMessageHandler.Json(HttpStatusCode.OK, json);
            var service = NeckSignTestInfra.CreateService(context, handler);

            service.SyncDocument(token).GetAwaiter().GetResult();
            var report = context.Reports.AsNoTracking().First(r => r.ID == reportId);

            if (c.ProviderStatus is "DECLINED" or "EXPIRED")
            {
                var expected = c.ProviderStatus == "DECLINED" ? Status.Declined : Status.Expired;
                var terminal = (report.DocumentStatus == expected).ToProperty()
                    .Label($"report terminal esperado {expected}, obteve {report.DocumentStatus}");
                // Campo por papel pré-preenchido preservado.
                var preserved = (report.SocialWorkerSign == preSocialSign).ToProperty()
                    .Label("campo por papel pré-preenchido deveria ser preservado em estado terminal");
                return terminal.And(preserved);
            }

            if (c.ProviderStatus == "SIGNED")
            {
                // No NeckSign, o envelope só atinge SIGNED quando TODOS assinaram ("Everyone has
                // signed") — não existe SIGNED parcial (parcial é SIGNING). Além disso, o GET não
                // popula signedAt por signatário nem envia externalId. Logo, SIGNED → Complete com
                // os três papéis marcados como assinados, independentemente do que o GET detalha.
                var complete = (report.DocumentStatus == Status.Complete).ToProperty()
                    .Label($"SIGNED → esperado Complete, obteve {report.DocumentStatus}");
                var allRoles = (report.SocialWorkerSign && report.PsychologistSign && report.PsychopedagogueSign)
                    .ToProperty().Label("SIGNED deveria marcar os três papéis como assinados");
                return complete.And(allRoles);
            }

            // PENDING/SIGNING → InProgress.
            return (report.DocumentStatus == Status.InProgress).ToProperty()
                .Label($"'{c.ProviderStatus}' → esperado InProgress, obteve {report.DocumentStatus}");
        }

        [Fact]
        public async Task Sync_partial_signatures_by_email_updates_only_signed_roles_and_keeps_in_progress()
        {
            using var context = NeckSignTestInfra.CreateContext();
            var reportId = Guid.NewGuid();
            var swId = Guid.NewGuid();
            var psiId = Guid.NewGuid();
            var ppId = Guid.NewGuid();
            var token = "env-partial-" + Guid.NewGuid().ToString("N");

            var swUser = new ApplicationUser { Id = "u-sw", Email = "social@escola.org", FullName = "Assistente Social" };
            var psiUser = new ApplicationUser { Id = "u-psi", Email = "psico@escola.org", FullName = "Psicólogo" };
            var ppUser = new ApplicationUser { Id = "u-pp", Email = "psicoped@escola.org", FullName = "Psicopedagoga" };

            var swReport = new SocialWorkerReport { ID = swId, User = swUser, UserID = swUser.Id };
            var psiReport = new PsychologistReport { ID = psiId, User = psiUser, UserID = psiUser.Id };
            var ppReport = new PsychopedagogueReport { ID = ppId, User = ppUser, UserID = ppUser.Id };

            context.Reports.Add(new Report
            {
                ID = reportId,
                DocumentToken = token,
                DocumentStatus = Status.InProgress,
                SignatureProvider = SignatureProvider.NeckSign,
                SocialWorkerReportID = swId,
                SocialWorkerReport = swReport,
                PsychologistReportID = psiId,
                PsychologistReport = psiReport,
                PsychopedagogueReportID = ppId,
                PsychopedagogueReport = ppReport,
                SocialWorkerSign = false,
                PsychologistSign = false,
                PsychopedagogueSign = false
            });
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            // Simula resposta real do NeckSign (SIGNING, sem externalId nos signatários, apenas email; apenas psicólogo assinou)
            var neckSignResponse = new
            {
                id = token,
                externalId = reportId.ToString(),
                status = "SIGNING",
                signers = new[]
                {
                    new
                    {
                        id = "signer-1",
                        name = "Psicólogo",
                        email = "psico@escola.org",
                        role = "SIGNER",
                        order = 1,
                        signedAt = (DateTimeOffset?)DateTimeOffset.UtcNow
                    },
                    new
                    {
                        id = "signer-2",
                        name = "Assistente Social",
                        email = "social@escola.org",
                        role = "SIGNER",
                        order = 2,
                        signedAt = (DateTimeOffset?)null
                    },
                    new
                    {
                        id = "signer-3",
                        name = "Psicopedagoga",
                        email = "psicoped@escola.org",
                        role = "SIGNER",
                        order = 3,
                        signedAt = (DateTimeOffset?)null
                    }
                }
            };

            var json = JsonSerializer.Serialize(neckSignResponse, JsonOptions);
            var handler = ProgrammableHttpMessageHandler.Json(HttpStatusCode.OK, json);
            var service = NeckSignTestInfra.CreateService(context, handler);

            var ok = await service.SyncDocument(token);
            Assert.True(ok);

            var report = await context.Reports.AsNoTracking().FirstAsync(r => r.ID == reportId);
            Assert.True(report.PsychologistSign);
            Assert.NotNull(report.PsychologistSignedAt);
            Assert.False(report.SocialWorkerSign);
            Assert.Null(report.SocialWorkerSignedAt);
            Assert.False(report.PsychopedagogueSign);
            Assert.Null(report.PsychopedagogueSignedAt);
            Assert.Equal(Status.InProgress, report.DocumentStatus);
        }

        // Feature: 012-singular-migration, Property 5: Falha de chamada ao provedor preserva o estado local
        //
        // Para qualquer chamada ao provedor que não conclua com sucesso (404, >=500, erro de
        // comunicação), os campos locais persistidos permanecem inalterados e o método sinaliza
        // falha (false/null).
        //
        // Validates: Requirements 2.12, 3.3, 3.7
        [Property(MaxTest = 100, Arbitrary = new[] { typeof(FailureCaseArbContainer) })]
        public Property Provider_failure_preserves_local_state(FailureCase c)
        {
            using var context = NeckSignTestInfra.CreateContext();
            var id = Guid.NewGuid();
            var token = "env-" + Guid.NewGuid().ToString("N");

            context.PEIs.Add(new PEI
            {
                ID = id,
                DocumentToken = token,
                DocumentStatus = Status.InProgress,
                SignedFile = "original-key",
                SignatureProvider = SignatureProvider.NeckSign
            });
            context.SaveChanges();
            context.ChangeTracker.Clear();

            var handler = c.IsCommunicationError
                ? new ProgrammableHttpMessageHandler(_ => throw new HttpRequestException("boom"))
                : ProgrammableHttpMessageHandler.Json((HttpStatusCode)c.StatusCode, "{}");
            var service = NeckSignTestInfra.CreateService(context, handler);

            var details = service.GetDocumentDetails(token).GetAwaiter().GetResult();
            var syncResult = service.SyncDocument(token).GetAwaiter().GetResult();

            var pei = context.PEIs.AsNoTracking().First(p => p.ID == id);

            var detailsNull = (details == null).ToProperty().Label("GetDocumentDetails deveria retornar null em falha");
            var syncFalse = (!syncResult).ToProperty().Label("SyncDocument deveria retornar false em falha");
            var preserved = (pei.DocumentStatus == Status.InProgress
                             && pei.DocumentToken == token
                             && pei.SignedFile == "original-key"
                             && pei.SignedAt == null
                             && pei.TeacherSign == false)
                .ToProperty().Label("estado local deveria ser preservado em falha");

            return detailsNull.And(syncFalse).And(preserved);
        }

        // Feature: 012-singular-migration, Property 20: bypassCooldown é no-op
        //
        // Para qualquer sequência de chamadas a GetDocumentDetails com qualquer valor de
        // bypassCooldown, cada chamada deve emitir uma nova consulta ao NeckSign (sem cache/cooldown).
        //
        // Validates: Requirements 3.5
        [Property(MaxTest = 100, Arbitrary = new[] { typeof(BypassSeqArbContainer) })]
        public Property BypassCooldown_is_noop_every_call_queries(bool[] flags)
        {
            using var context = NeckSignTestInfra.CreateContext();
            var id = Guid.NewGuid();
            var token = "env-" + Guid.NewGuid().ToString("N");

            var json = BuildEnvelopeJson(id, "PENDING", null, false);
            var handler = ProgrammableHttpMessageHandler.Json(HttpStatusCode.OK, json);
            var service = NeckSignTestInfra.CreateService(context, handler);

            var seq = (flags is { Length: > 0 }) ? flags : new[] { true, false };
            foreach (var bypass in seq)
            {
                service.GetDocumentDetails(token, bypassCooldown: bypass).GetAwaiter().GetResult();
            }

            return (handler.CallCount == seq.Length).ToProperty()
                .Label($"esperava {seq.Length} consultas HTTP, obteve {handler.CallCount}");
        }

        // --- Helpers ---

        private static Status ExpectedLocalStatus(string providerStatus) => providerStatus switch
        {
            "PENDING" => Status.InProgress,
            "SIGNING" => Status.InProgress,
            "SIGNED" => Status.Complete,
            "DECLINED" => Status.Declined,
            "EXPIRED" => Status.Expired,
            "VOIDED" => Status.InProgress, // no-op → preserva o InProgress semeado
            _ => Status.InProgress
        };

        private static string BuildEnvelopeJson(Guid externalId, string status, string? signedFile, bool signerSigned)
        {
            var body = new
            {
                envelopeId = "env-x",
                externalId = externalId.ToString(),
                status,
                signedFileUrl = signedFile,
                signers = new[]
                {
                    new
                    {
                        externalId = Guid.NewGuid().ToString(),
                        name = "R",
                        email = "r@test.local",
                        status = signerSigned ? "SIGNED" : "PENDING",
                        signedAt = signerSigned ? (DateTimeOffset?)DateTimeOffset.UtcNow : null
                    }
                }
            };
            return JsonSerializer.Serialize(body, JsonOptions);
        }

        private static string BuildReportEnvelopeJson(Guid externalId, string status, Guid[] roleIds, int signedRoles)
        {
            var signers = roleIds.Select((rid, i) => new
            {
                externalId = rid.ToString(),
                name = "R" + i,
                email = $"r{i}@test.local",
                status = i < signedRoles ? "SIGNED" : "PENDING",
                signedAt = i < signedRoles ? (DateTimeOffset?)DateTimeOffset.UtcNow : null
            }).ToArray();

            var body = new
            {
                envelopeId = "env-x",
                externalId = externalId.ToString(),
                status,
                signedFileUrl = status == "SIGNED" ? "https://files.test/report.pdf" : null,
                signers
            };
            return JsonSerializer.Serialize(body, JsonOptions);
        }

        // --- Geradores ---

        public static class SingleStatusCaseArbContainer
        {
            public static Arbitrary<SingleStatusCase> SingleStatusCase() =>
                Arb.From(Gen.Elements("PENDING", "SIGNING", "SIGNED", "DECLINED", "EXPIRED", "VOIDED")
                    .Select(s => new SingleStatusCase(s)));
        }

        public sealed class SingleStatusCase
        {
            public string ProviderStatus { get; }
            public SingleStatusCase(string s) => ProviderStatus = s;
            public override string ToString() => $"SingleStatusCase({ProviderStatus})";
        }

        public static class ReportStatusCaseArbContainer
        {
            public static Arbitrary<ReportStatusCase> ReportStatusCase() =>
                Arb.From(
                    from status in Gen.Elements("PENDING", "SIGNING", "SIGNED", "DECLINED", "EXPIRED")
                    from signedCount in Gen.Choose(0, 3)
                    from preSigned in Gen.Elements(true, false)
                    select new ReportStatusCase(status, signedCount, preSigned));
        }

        public sealed class ReportStatusCase
        {
            public string ProviderStatus { get; }
            public int SignedRoleCount { get; }
            public bool PreSignedSocial { get; }
            public ReportStatusCase(string status, int signedCount, bool preSigned)
            {
                ProviderStatus = status;
                SignedRoleCount = signedCount;
                PreSignedSocial = preSigned;
            }
            public override string ToString() => $"ReportStatusCase({ProviderStatus}, signed={SignedRoleCount}, preSW={PreSignedSocial})";
        }

        public static class FailureCaseArbContainer
        {
            public static Arbitrary<FailureCase> FailureCase() =>
                Arb.From(Gen.OneOf(
                    Gen.Constant(new FailureCase(0, true)),
                    Gen.Elements(404, 500, 502, 503).Select(s => new FailureCase(s, false))));
        }

        public sealed class FailureCase
        {
            public int StatusCode { get; }
            public bool IsCommunicationError { get; }
            public FailureCase(int statusCode, bool isCommError)
            {
                StatusCode = statusCode;
                IsCommunicationError = isCommError;
            }
            public override string ToString() => IsCommunicationError ? "FailureCase(comm-error)" : $"FailureCase({StatusCode})";
        }

        public static class BypassSeqArbContainer
        {
            public static Arbitrary<bool[]> BoolSeq() =>
                Arb.From(Gen.NonEmptyListOf(Gen.Elements(true, false)).Select(l => l.ToArray()));
        }
    }
}
