using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FsCheck;
using FsCheck.Xunit;
using SingularSystem.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace SingularSystem.Tests
{
    public sealed class BackupTests
    {
        // Feature: modulo-1-infraestrutura, Property 6: Política de retenção de backup (30 dias)
        //
        // Para todo conjunto de dumps com instantes de geração arbitrários, ao aplicar a política
        // de retenção em um instante de referência, devem permanecer exatamente os dumps com idade
        // estritamente inferior a 30 dias e devem ser removidos exatamente os dumps com idade igual
        // ou superior a 30 dias.
        //
        // Validates: Requirements 7.2, 7.3
        [Property(MaxTest = 100, Arbitrary = new[] { typeof(BackupDumpsCaseArb) })]
        public Property Backup_retention_policy_30_days(BackupDumpsCase c)
        {
            var (keep, remove) = BackupRetentionPolicy.ApplyRetention(c.Dumps, c.ReferenceTime);

            // Verifica que todos os mantidos têm idade < 30 dias
            var allKeepUnder30Days = keep.All(d => (c.ReferenceTime - d.GeneratedAt) < BackupRetentionPolicy.RetentionPeriod);

            // Verifica que todos os removidos têm idade >= 30 dias
            var allRemoveAtOrOver30Days = remove.All(d => (c.ReferenceTime - d.GeneratedAt) >= BackupRetentionPolicy.RetentionPeriod);

            // Verifica partição completa sem perdas ou duplicações
            var totalPartitionMatches = (keep.Count + remove.Count) == c.Dumps.Count;

            return (allKeepUnder30Days && allRemoveAtOrOver30Days && totalPartitionMatches).ToProperty()
                .Label($"Total={c.Dumps.Count}, Keep={keep.Count}, Remove={remove.Count}");
        }

        // Task 11.3: Teste de integração do backup (Req 7.1, 7.4, 7.5, 7.6)
        [Fact]
        public async Task Daily_dump_is_generated_and_verified_restorable()
        {
            var service = new BackupService(NullLogger<BackupService>.Instance);
            var dump = await service.TriggerDailyDumpAsync();

            Assert.NotNull(dump);
            Assert.True(dump.IsRestorable);
            Assert.True(dump.SizeBytes > 0);
            Assert.False(string.IsNullOrWhiteSpace(dump.StoragePath));
        }

        [Fact]
        public async Task Backup_failure_triggers_notification_channel()
        {
            var notificationMock = new TestNotificationChannel();
            var service = new FailingBackupService(NullLogger<BackupService>.Instance, notificationMock);

            var dump = await service.TriggerDailyDumpAsync();

            Assert.Null(dump);
            Assert.True(notificationMock.NotifyCalled);
            Assert.Contains("falhou no teste de restaurabilidade", notificationMock.LastMessage);
        }

        [Fact]
        public async Task Backup_failure_triggers_fallback_alert_when_primary_fails()
        {
            var failingNotification = new ExceptionThrowingNotificationChannel();
            var alertMock = new TestAlertFallbackChannel();
            var service = new FailingBackupService(NullLogger<BackupService>.Instance, failingNotification, alertMock);

            var dump = await service.TriggerDailyDumpAsync();

            Assert.Null(dump);
            Assert.True(alertMock.EmergencyAlertCalled);
            Assert.Contains("falhou no teste de restaurabilidade", alertMock.LastMessage);
        }

        // --- Classes auxiliares para testes ---

        private sealed class TestNotificationChannel : IBackupNotificationChannel
        {
            public bool NotifyCalled { get; private set; }
            public string LastMessage { get; private set; } = "";

            public Task NotifyFailureAsync(string message, Exception? ex = null)
            {
                NotifyCalled = true;
                LastMessage = message;
                return Task.CompletedTask;
            }
        }

        private sealed class ExceptionThrowingNotificationChannel : IBackupNotificationChannel
        {
            public Task NotifyFailureAsync(string message, Exception? ex = null)
            {
                throw new InvalidOperationException("Canal primário de notificação indisponível.");
            }
        }

        private sealed class TestAlertFallbackChannel : IBackupAlertFallbackChannel
        {
            public bool EmergencyAlertCalled { get; private set; }
            public string LastMessage { get; private set; } = "";

            public Task SendEmergencyAlertAsync(string message, Exception? ex = null)
            {
                EmergencyAlertCalled = true;
                LastMessage = message;
                return Task.CompletedTask;
            }
        }

        private sealed class FailingBackupService : BackupService
        {
            public FailingBackupService(
                Microsoft.Extensions.Logging.ILogger<BackupService> logger,
                IBackupNotificationChannel? notificationChannel = null,
                IBackupAlertFallbackChannel? fallbackChannel = null)
                : base(logger, notificationChannel, fallbackChannel) { }

            public override Task<bool> VerifyRestorabilityAsync(BackupDump dump)
            {
                // Simula falha de restaurabilidade / corrupção do dump
                return Task.FromResult(false);
            }
        }

        // --- Geradores ---

        public sealed class BackupDumpsCase
        {
            public DateTime ReferenceTime { get; }
            public IReadOnlyList<BackupDump> Dumps { get; }

            public BackupDumpsCase(DateTime referenceTime, IReadOnlyList<BackupDump> dumps)
            {
                ReferenceTime = referenceTime;
                Dumps = dumps;
            }
        }

        public static class BackupDumpsCaseArb
        {
            public static Arbitrary<BackupDumpsCase> Arbitrary()
            {
                var refTime = new DateTime(2026, 11, 9, 12, 0, 0, DateTimeKind.Utc);

                var dumpGen = from ageInHours in Gen.Choose(-24 * 5, 24 * 60) // de -5 dias (futuro) até 60 dias atrás
                              select new BackupDump(
                                  Id: $"dump_{Guid.NewGuid():N}",
                                  GeneratedAt: refTime.AddHours(-ageInHours),
                                  SizeBytes: 1024,
                                  IsRestorable: true
                              );

                return (from dumps in Gen.ListOf(dumpGen)
                        select new BackupDumpsCase(refTime, dumps.ToList()))
                    .ToArbitrary();
            }
        }
    }
}
