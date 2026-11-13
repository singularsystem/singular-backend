using FsCheck;
using FsCheck.Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SingularSystem.Domain.Plans;
using SingularSystem.Domain.Plans.Enums;
using SingularSystem.Models.Entities;
using SingularSystem.Services;
using Xunit;

namespace SingularSystem.Tests
{
    public sealed class SubscriptionExpirationPropertyTests
    {
        private static AppDbContext CreateDb(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new AppDbContext(options);
        }

        // Feature: modulo-3-2-travas-backend, Property 16: Expiração de Active para PastDue
        // Validates: Requirements 8.1
        [Property(MaxTest = 100)]
        public Property Expiracao_de_Active_para_PastDue(int offsetMinutes)
        {
            var dbName = $"p16-{Guid.NewGuid()}";
            using var db = CreateDb(dbName);
            var service = new SubscriptionExpirationService(null!, NullLogger<SubscriptionExpirationService>.Instance);

            var now = new DateTime(2026, 11, 13, 12, 0, 0, DateTimeKind.Utc);
            var endDate = now.AddMinutes(offsetMinutes);

            var sub = new PlanSubscription
            {
                ID = Guid.NewGuid(),
                TenantId = $"tenant-{Guid.NewGuid()}",
                PlanType = PlanType.Standard,
                Status = SubscriptionStatus.Active,
                StartDate = now.AddDays(-30),
                EndDate = endDate,
                CreatedAt = now.AddDays(-30)
            };

            var subNoEndDate = new PlanSubscription
            {
                ID = Guid.NewGuid(),
                TenantId = $"tenant-noend-{Guid.NewGuid()}",
                PlanType = PlanType.Standard,
                Status = SubscriptionStatus.Active,
                StartDate = now.AddDays(-30),
                EndDate = null,
                CreatedAt = now.AddDays(-30)
            };

            db.PlanSubscriptions.AddRange(sub, subNoEndDate);
            db.SaveChanges();

            service.ProcessExpirationsCoreAsync(db, now).GetAwaiter().GetResult();

            var reloadedSub = db.PlanSubscriptions.Find(sub.ID)!;
            var reloadedNoEnd = db.PlanSubscriptions.Find(subNoEndDate.ID)!;

            bool expectedSubTransition = offsetMinutes < 0; // Se endDate < now, deve virar PastDue
            bool subOk = expectedSubTransition
                ? reloadedSub.Status == SubscriptionStatus.PastDue
                : reloadedSub.Status == SubscriptionStatus.Active;

            bool noEndOk = reloadedNoEnd.Status == SubscriptionStatus.Active; // sem EndDate nunca expira

            return (subOk && noEndOk).ToProperty()
                .Label($"offsetMinutes={offsetMinutes}, status={reloadedSub.Status}, noEndStatus={reloadedNoEnd.Status}");
        }

        // Feature: modulo-3-2-travas-backend, Property 17: Expiração de PastDue para Suspended após grace period
        // Validates: Requirements 8.1
        [Property(MaxTest = 100)]
        public Property Expiracao_de_PastDue_para_Suspended_apos_grace_period(int offsetDaysPast30)
        {
            var dbName = $"p17-{Guid.NewGuid()}";
            using var db = CreateDb(dbName);
            var service = new SubscriptionExpirationService(null!, NullLogger<SubscriptionExpirationService>.Instance);

            var now = new DateTime(2026, 11, 13, 12, 0, 0, DateTimeKind.Utc);
            // Grace period = 30 dias após EndDate. Ou seja, EndDate < now - 30 dias.
            // offsetDaysPast30 > 0 significa que já passaram mais de 30 dias (ex: EndDate = now - 30 - offset)
            var endDate = now.AddDays(-30).AddDays(-offsetDaysPast30);

            var sub = new PlanSubscription
            {
                ID = Guid.NewGuid(),
                TenantId = $"tenant-{Guid.NewGuid()}",
                PlanType = PlanType.Standard,
                Status = SubscriptionStatus.PastDue,
                StartDate = now.AddDays(-60),
                EndDate = endDate,
                CreatedAt = now.AddDays(-60)
            };
            db.PlanSubscriptions.Add(sub);
            db.SaveChanges();

            service.ProcessExpirationsCoreAsync(db, now).GetAwaiter().GetResult();

            var reloadedSub = db.PlanSubscriptions.Find(sub.ID)!;

            bool shouldSuspend = endDate < now.AddDays(-30);
            bool subOk = shouldSuspend
                ? reloadedSub.Status == SubscriptionStatus.Suspended
                : reloadedSub.Status == SubscriptionStatus.PastDue;

            return subOk.ToProperty()
                .Label($"offsetDaysPast30={offsetDaysPast30}, endDate={endDate}, nowMinus30={now.AddDays(-30)}, status={reloadedSub.Status}");
        }
    }
}
