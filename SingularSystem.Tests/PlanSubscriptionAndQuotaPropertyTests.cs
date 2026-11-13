using FsCheck;
using FsCheck.Xunit;
using Microsoft.EntityFrameworkCore;
using SingularSystem.Domain.Plans;
using SingularSystem.Domain.Plans.Enums;
using SingularSystem.Infrastructure.Plans;
using SingularSystem.Models.Entities;
using SingularSystem.Services;
using Xunit;

namespace SingularSystem.Tests
{
    public sealed class PlanSubscriptionAndQuotaPropertyTests
    {
        private static AppDbContext CreateDb(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new AppDbContext(options);
        }

        // Feature: modulo-3-2-travas-backend, Property 6: A trava enxerga assinaturas em qualquer status
        // Validates: Requirements 1.2, 10.1
        [Property(MaxTest = 100)]
        public Property Trava_enxerga_assinaturas_em_qualquer_status(PositiveInt statusInt, PositiveInt daysAgo1, PositiveInt daysAgo2)
        {
            var dbName = $"p6-{Guid.NewGuid()}";
            using var db = CreateDb(dbName);
            var repo = new PlanSubscriptionRepository(db);

            var tenantId = $"tenant-{Guid.NewGuid()}";
            // 1. Qualquer status (Active, PastDue, Suspended, Cancelled) é enxergado
            var status = (SubscriptionStatus)((statusInt.Get % 4) + 1);

            var sub = new PlanSubscription
            {
                ID = Guid.NewGuid(),
                TenantId = tenantId,
                PlanType = PlanType.Standard,
                Status = status,
                QuotaDocuments = 10,
                MaxUsers = 1,
                StartDate = DateTime.UtcNow.AddDays(-daysAgo1.Get),
                CreatedAt = DateTime.UtcNow
            };
            db.PlanSubscriptions.Add(sub);
            db.SaveChanges();

            var current = repo.GetCurrentAsync(tenantId).GetAwaiter().GetResult();
            var found = current != null && current.ID == sub.ID && current.Status == status;

            // 2. Se a primeira for não-Active (PastDue, Suspended, Cancelled), uma Active mais antiga deve ser priorizada
            var nonActiveStatus = (SubscriptionStatus)((statusInt.Get % 3) + 2); // 2=PastDue, 3=Suspended, 4=Cancelled
            var tenant2Id = $"tenant2-{Guid.NewGuid()}";
            var nonActiveSub = new PlanSubscription
            {
                ID = Guid.NewGuid(),
                TenantId = tenant2Id,
                PlanType = PlanType.Standard,
                Status = nonActiveStatus,
                QuotaDocuments = 10,
                MaxUsers = 1,
                StartDate = DateTime.UtcNow.AddDays(-daysAgo1.Get),
                CreatedAt = DateTime.UtcNow
            };
            var olderActiveSub = new PlanSubscription
            {
                ID = Guid.NewGuid(),
                TenantId = tenant2Id,
                PlanType = PlanType.Standard,
                Status = SubscriptionStatus.Active,
                QuotaDocuments = 10,
                MaxUsers = 1,
                StartDate = DateTime.UtcNow.AddDays(-daysAgo1.Get - daysAgo2.Get - 10),
                CreatedAt = DateTime.UtcNow
            };
            db.PlanSubscriptions.AddRange(nonActiveSub, olderActiveSub);
            db.SaveChanges();

            var currentForTenant2 = repo.GetCurrentAsync(tenant2Id).GetAwaiter().GetResult();
            var activePrioritized = currentForTenant2 != null && currentForTenant2.ID == olderActiveSub.ID;

            return (found && activePrioritized).ToProperty()
                .Label($"status={status}, found={found}, activePrioritized={activePrioritized}");
        }

        // Feature: modulo-3-2-travas-backend, Property 11: Cota considera N linhas
        // Validates: Requirements 1.2, 3.1, 3.4, 10.1, 11.2
        [Property(MaxTest = 100)]
        public Property Cota_considera_N_linhas(NonNegativeInt usedCountGen, NonNegativeInt additionalGen, PositiveInt quotaGen)
        {
            var dbName = $"p11-{Guid.NewGuid()}";
            using var db = CreateDb(dbName);
            var repo = new PlanSubscriptionRepository(db);
            var service = new PlanService(repo, db);

            var tenantId = $"tenant-{Guid.NewGuid()}";
            var used = usedCountGen.Get % 25; // 0..24 alunos existentes
            var additional = additionalGen.Get % 15; // 0..14 a importar
            var quota = (quotaGen.Get % 20) + 1; // 1..20

            // 1. Sem assinatura => Allowed == true, (0, 0)
            var noSubResult = service.CheckStudentQuotaAsync(tenantId, additional).GetAwaiter().GetResult();
            var noSubOk = noSubResult.Allowed && noSubResult.Current == 0 && noSubResult.Max == 0;

            // 2. Com assinatura QuotaDocuments == 0 (ilimitado)
            var unlimSub = new PlanSubscription
            {
                ID = Guid.NewGuid(),
                TenantId = tenantId,
                PlanType = PlanType.Golden,
                Status = SubscriptionStatus.Active,
                QuotaDocuments = 0,
                MaxUsers = 0,
                StartDate = DateTime.UtcNow.AddDays(-1),
                CreatedAt = DateTime.UtcNow
            };
            db.PlanSubscriptions.Add(unlimSub);
            db.SaveChanges();

            var unlimResult = service.CheckStudentQuotaAsync(tenantId, additional).GetAwaiter().GetResult();
            var unlimOk = unlimResult.Allowed && unlimResult.Current == 0 && unlimResult.Max == 0;

            // 3. Com cota limitada
            unlimSub.QuotaDocuments = quota;
            for (var i = 0; i < used; i++)
            {
                db.Students.Add(new Student
                {
                    ID = Guid.NewGuid(),
                    FullName = $"Student {i}",
                    City = "TestCity",
                    State = "TS",
                    TenantId = tenantId,
                    IsActive = true
                });
            }
            // Add inactive student for this tenant (should not count)
            db.Students.Add(new Student
            {
                ID = Guid.NewGuid(),
                FullName = "Inactive Student",
                City = "TestCity",
                State = "TS",
                TenantId = tenantId,
                IsActive = false
            });
            // Add active student for another tenant (should not count)
            db.Students.Add(new Student
            {
                ID = Guid.NewGuid(),
                FullName = "Other Tenant Student",
                City = "TestCity",
                State = "TS",
                TenantId = "other-tenant",
                IsActive = true
            });
            db.SaveChanges();

            var quotaResult = service.CheckStudentQuotaAsync(tenantId, additional).GetAwaiter().GetResult();
            var expectedAllowed = (used + additional) <= quota;
            var quotaOk = quotaResult.Allowed == expectedAllowed &&
                          quotaResult.Current == used &&
                          quotaResult.Max == quota;

            // 4. Equivalência quando additional == 1
            var q1Result = service.CheckStudentQuotaAsync(tenantId).GetAwaiter().GetResult();
            var q1Explicit = service.CheckStudentQuotaAsync(tenantId, 1).GetAwaiter().GetResult();
            var eqOk = q1Result.Allowed == q1Explicit.Allowed &&
                       q1Result.Current == q1Explicit.Current &&
                       q1Result.Max == q1Explicit.Max;

            return (noSubOk && unlimOk && quotaOk && eqOk).ToProperty()
                .Label($"used={used}, add={additional}, quota={quota}: noSub={noSubOk}, unlim={unlimOk}, quotaOk={quotaOk}, eqOk={eqOk}");
        }
    }
}
