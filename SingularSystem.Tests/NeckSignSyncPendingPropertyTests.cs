using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using FsCheck;
using FsCheck.Xunit;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace SingularSystem.Tests
{
    public sealed class NeckSignSyncPendingPropertyTests
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        // Feature: 012-ipaags-migration, Property 9: Seleção de documentos pendentes para sincronização
        //
        // Para qualquer conjunto misto de documentos, SyncPendingDocuments() deve consultar o NeckSign
        // exatamente para os documentos com DocumentStatus = InProgress e DocumentToken não vazio, não
        // consultando documentos Complete/Declined/Expired nem alterando seus campos locais.
        //
        // Validates: Requirements 8.1, 14.4
        [Property(MaxTest = 100, Arbitrary = new[] { typeof(DocSetArbContainer) })]
        public Property SyncPending_queries_only_in_progress_with_token(DocSet set)
        {
            var dbName = $"necksign-pending-{Guid.NewGuid()}";

            // Rastreia os tokens efetivamente consultados por GET /envelopes/{token}.
            var queried = new System.Collections.Concurrent.ConcurrentBag<string>();
            var handler = new ProgrammableHttpMessageHandler(req =>
            {
                var token = ExtractEnvelopeToken(req);
                if (token != null) queried.Add(token);
                // Devolve PENDING para não alterar nada materialmente além do esperado.
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(PendingEnvelopeJson(set, token), Encoding.UTF8, "application/json")
                };
            });

            using (var seed = NeckSignTestInfra.CreateContext(dbName))
            {
                SeedDocs(seed, set);
                seed.SaveChanges();
            }

            var scopeFactory = NeckSignTestInfra.ScopeFactoryFor(dbName, handler);
            using var context = NeckSignTestInfra.CreateContext(dbName);
            var service = NeckSignTestInfra.CreateService(context, handler, scopeFactory);

            service.SyncPendingDocuments().GetAwaiter().GetResult();

            var expectedTokens = set.Docs
                .Where(d => d.Status == Status.InProgress && !string.IsNullOrWhiteSpace(d.Token))
                .Select(d => d.Token!)
                .Distinct()
                .ToHashSet();

            var queriedSet = queried.ToHashSet();

            return queriedSet.SetEquals(expectedTokens).ToProperty()
                .Label($"consultados=[{string.Join(",", queriedSet)}] esperados=[{string.Join(",", expectedTokens)}]");
        }

        // Feature: 012-ipaags-migration, Property 10: Idempotência do job de sincronização
        //
        // Para qualquer conjunto de documentos, executar SyncPendingDocuments() duas ou mais vezes
        // deve produzir o mesmo estado final que uma única execução.
        //
        // Validates: Requirements 8.6
        [Property(MaxTest = 100, Arbitrary = new[] { typeof(DocSetArbContainer) })]
        public Property SyncPending_is_idempotent(DocSet set)
        {
            var once = RunAndSnapshot(set, runs: 1);
            var thrice = RunAndSnapshot(set, runs: 3);

            var equal = once.Count == thrice.Count &&
                once.All(kv => thrice.TryGetValue(kv.Key, out var s) && s == kv.Value);

            return equal.ToProperty()
                .Label($"estado final após 1 execução != após 3 execuções");
        }

        private static Dictionary<Guid, Status> RunAndSnapshot(DocSet set, int runs)
        {
            var dbName = $"necksign-idem-{Guid.NewGuid()}";
            // Para o SIGNED resolver determinístico, o provedor responde conforme o doc semeado.
            var handler = new ProgrammableHttpMessageHandler(req =>
            {
                var token = ExtractEnvelopeToken(req);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(PendingEnvelopeJson(set, token), Encoding.UTF8, "application/json")
                };
            });

            using (var seed = NeckSignTestInfra.CreateContext(dbName))
            {
                SeedDocs(seed, set);
                seed.SaveChanges();
            }

            var scopeFactory = NeckSignTestInfra.ScopeFactoryFor(dbName, handler);
            for (int i = 0; i < runs; i++)
            {
                using var ctx = NeckSignTestInfra.CreateContext(dbName);
                var service = NeckSignTestInfra.CreateService(ctx, handler, scopeFactory);
                service.SyncPendingDocuments().GetAwaiter().GetResult();
            }

            using var snapCtx = NeckSignTestInfra.CreateContext(dbName);
            var snapshot = new Dictionary<Guid, Status>();
            foreach (var p in snapCtx.PEIs.AsNoTracking()) snapshot[p.ID] = p.DocumentStatus;
            foreach (var p in snapCtx.RegularTeacherEvaluationDocuments.AsNoTracking()) snapshot[p.ID] = p.DocumentStatus;
            return snapshot;
        }

        private static string? ExtractEnvelopeToken(HttpRequestMessage req)
        {
            // Caminho esperado: api/v1/envelopes/{token}
            var path = req.RequestUri?.AbsolutePath ?? string.Empty;
            const string marker = "/api/v1/envelopes/";
            var idx = path.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;
            var rest = path[(idx + marker.Length)..];
            if (rest.Length == 0) return null;
            // Remove sufixos como "/void".
            var slash = rest.IndexOf('/');
            var token = slash >= 0 ? rest[..slash] : rest;
            return Uri.UnescapeDataString(token);
        }

        private static string PendingEnvelopeJson(DocSet set, string? token)
        {
            // Resolve o externalId do doc semeado com aquele token (quando houver) para que a
            // reconciliação encontre a entidade; status PENDING mantém InProgress (idempotente).
            var doc = set.Docs.FirstOrDefault(d => d.Token == token);
            var externalId = doc?.Id.ToString() ?? Guid.NewGuid().ToString();
            var body = new
            {
                envelopeId = token ?? "env",
                externalId,
                status = "PENDING",
                signedFileUrl = (string?)null,
                signers = Array.Empty<object>()
            };
            return JsonSerializer.Serialize(body, JsonOptions);
        }

        private static void SeedDocs(AppDbContext ctx, DocSet set)
        {
            // Usa PEI/caseStudy, que não possuem campos obrigatórios adicionais exigidos pelo InMemory.
            foreach (var d in set.Docs)
            {
                if (d.IsCaseStudy)
                {
                    ctx.RegularTeacherEvaluationDocuments.Add(new RegularTeacherEvaluationDocument
                    {
                        ID = d.Id,
                        DocumentToken = d.Token,
                        DocumentStatus = d.Status,
                        SignatureProvider = SignatureProvider.NeckSign
                    });
                }
                else
                {
                    ctx.PEIs.Add(new PEI
                    {
                        ID = d.Id,
                        DocumentToken = d.Token,
                        DocumentStatus = d.Status,
                        SignatureProvider = SignatureProvider.NeckSign
                    });
                }
            }
        }

        // --- Geradores ---

        public static class DocSetArbContainer
        {
            public static Arbitrary<DocSet> DocSet() => DocSetArb();
        }

        private static Arbitrary<DocSet> DocSetArb()
        {
            var statusGen = Gen.Elements(Status.InProgress, Status.Complete, Status.Declined, Status.Expired, Status.None);
            var tokenGen = Gen.Frequency(
                Tuple.Create(5, Gen.Constant(0).Select(_ => (string?)("tok-" + Guid.NewGuid().ToString("N")[..8]))),
                Tuple.Create(1, Gen.Constant<string?>(null)),
                Tuple.Create(1, Gen.Constant<string?>("")));

            var docGen =
                from status in statusGen
                from token in tokenGen
                from isCaseStudy in Gen.Elements(true, false)
                select new DocEntry(Guid.NewGuid(), token, status, isCaseStudy);

            // Limita o tamanho do conjunto: SyncPendingDocuments cria um escopo DI e um round-trip
            // HTTP mockado por token pendente; conjuntos pequenos mantêm o custo baixo.
            return Arb.From(
                from n in Gen.Choose(0, 6)
                from docs in Gen.ListOf(n, docGen)
                select new DocSet(docs.ToList()));
        }

        public sealed class DocSet
        {
            public IReadOnlyList<DocEntry> Docs { get; }
            public DocSet(IReadOnlyList<DocEntry> docs) => Docs = docs;
            public override string ToString() => $"DocSet(n={Docs.Count})";
        }

        public sealed class DocEntry
        {
            public Guid Id { get; }
            public string? Token { get; }
            public Status Status { get; }
            public bool IsCaseStudy { get; }
            public DocEntry(Guid id, string? token, Status status, bool isCaseStudy)
            {
                Id = id;
                Token = token;
                Status = status;
                IsCaseStudy = isCaseStudy;
            }
        }
    }
}
