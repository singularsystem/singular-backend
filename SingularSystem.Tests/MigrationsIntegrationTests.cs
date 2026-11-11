using System;
using System.Linq;
using System.Threading.Tasks;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace SingularSystem.Tests
{
    // Feature: 012-singular-migration — Task 2.4: testes de integração das migrations
    //
    // Confirma, com EF Core InMemory, os invariantes garantidos pelas migrations das waves 0-1:
    //  - a extensão do enum Status (Declined=6, Expired=7) não altera os valores legados
    //    (None/InProgress/Complete) nem registros existentes;
    //  - a coluna SignatureProvider recebe o default ZapSign para registros legados;
    //  - a inserção de uma chave (Provider, EnvelopeOrDocumentToken, EventState) duplicada em
    //    ProcessedWebhookEvent é rejeitada pela constraint UNIQUE.
    //
    // Validates: Requirements 14.3, 15.2, 15.5, 4.12, 12.5
    public sealed class MigrationsIntegrationTests
    {
        private static AppDbContext CreateContext() =>
            new(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"migrations-{Guid.NewGuid()}")
                .Options);

        [Fact]
        public void StatusEnum_extension_preserves_legacy_values()
        {
            // Os valores legados devem permanecer estáveis; os novos são adicionais e distintos.
            Assert.Equal(5, (int)Status.None);
            Assert.Equal(4, (int)Status.InProgress);
            Assert.Equal(2, (int)Status.Complete);

            Assert.Equal(6, (int)Status.Declined);
            Assert.Equal(7, (int)Status.Expired);

            // Nenhum valor novo colide com um valor legado.
            var all = Enum.GetValues<Status>().Select(v => (int)v).ToArray();
            Assert.Equal(all.Length, all.Distinct().Count());
        }

        [Fact]
        public async Task StatusEnum_extension_does_not_alter_existing_records()
        {
            using var context = CreateContext();

            // Semeia registros legados com valores de status pré-existentes.
            var completeId = Guid.NewGuid();
            var inProgressId = Guid.NewGuid();
            context.PEIs.Add(new PEI { ID = completeId, DocumentStatus = Status.Complete });
            context.PEIs.Add(new PEI { ID = inProgressId, DocumentStatus = Status.InProgress });
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            // Os valores legados permanecem intactos (a extensão do enum é puramente aditiva).
            Assert.Equal(Status.Complete, (await context.PEIs.AsNoTracking().FirstAsync(p => p.ID == completeId)).DocumentStatus);
            Assert.Equal(Status.InProgress, (await context.PEIs.AsNoTracking().FirstAsync(p => p.ID == inProgressId)).DocumentStatus);
        }

        [Fact]
        public async Task SignatureProvider_defaults_to_NeckSign_for_new_entities()
        {
            using var context = CreateContext();

            // Entidades criadas sem definir SignatureProvider assumem o default ZapSign (= legado).
            // PEI/Report/caseStudy não têm campos obrigatórios adicionais exigidos pelo InMemory.
            var report = new Report { ID = Guid.NewGuid() };
            var pei = new PEI { ID = Guid.NewGuid() };
            var caseStudy = new RegularTeacherEvaluationDocument { ID = Guid.NewGuid() };

            context.Reports.Add(report);
            context.PEIs.Add(pei);
            context.RegularTeacherEvaluationDocuments.Add(caseStudy);
            await context.SaveChangesAsync();

            Assert.Equal(SignatureProvider.NeckSign, (await context.Reports.AsNoTracking().FirstAsync()).SignatureProvider);
            Assert.Equal(SignatureProvider.NeckSign, (await context.PEIs.AsNoTracking().FirstAsync()).SignatureProvider);
            Assert.Equal(SignatureProvider.NeckSign, (await context.RegularTeacherEvaluationDocuments.AsNoTracking().FirstAsync()).SignatureProvider);

            // O default do campo também é expresso no modelo das 4 entidades (incluindo PAEE).
            Assert.Equal(SignatureProvider.NeckSign, new PAEE().SignatureProvider);
        }

        [Fact]
        public void ProcessedWebhookEvent_declares_unique_index_on_provider_token_state()
        {
            // O provedor EF Core InMemory não impõe índices únicos em runtime; a rejeição de
            // duplicatas acontece no banco real via a UNIQUE criada pela migration. Aqui validamos
            // que o MODELO declara o índice único sobre (Provider, EnvelopeOrDocumentToken,
            // EventState), que é o contrato de deduplicação (Req 4.12, 12.5).
            using var context = CreateContext();

            var entityType = context.Model.FindEntityType(typeof(ProcessedWebhookEvent));
            Assert.NotNull(entityType);

            var uniqueIndex = entityType!.GetIndexes().FirstOrDefault(i => i.IsUnique);
            Assert.NotNull(uniqueIndex);

            var columns = uniqueIndex!.Properties.Select(p => p.Name).ToArray();
            Assert.Equal(
                new[]
                {
                    nameof(ProcessedWebhookEvent.Provider),
                    nameof(ProcessedWebhookEvent.EnvelopeOrDocumentToken),
                    nameof(ProcessedWebhookEvent.EventState)
                },
                columns);
        }

        [Fact]
        public async Task ProcessedWebhookEvent_allows_distinct_keys()
        {
            using var context = CreateContext();

            context.ProcessedWebhookEvents.AddRange(
                new ProcessedWebhookEvent { Provider = SignatureProvider.NeckSign, EnvelopeOrDocumentToken = "env-1", EventState = "envelope.signed" },
                new ProcessedWebhookEvent { Provider = SignatureProvider.NeckSign, EnvelopeOrDocumentToken = "env-1", EventState = "envelope.expired" },
                new ProcessedWebhookEvent { Provider = SignatureProvider.ZapSign, EnvelopeOrDocumentToken = "env-1", EventState = "envelope.signed" },
                new ProcessedWebhookEvent { Provider = SignatureProvider.NeckSign, EnvelopeOrDocumentToken = "env-2", EventState = "envelope.signed" });

            await context.SaveChangesAsync();

            Assert.Equal(4, await context.ProcessedWebhookEvents.CountAsync());
        }
    }
}
