using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using FsCheck;
using FsCheck.Xunit;
using SingularSystem.Domain.Access;
using SingularSystem.Domain.Tenancy;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;
using SingularSystem.Services;
using SingularSystem.Tests.TestHelpers;

namespace SingularSystem.Tests
{
    // Feature: modulo-3-2b-isolamento-pdf-listagens, Property 2: Admin tem acesso total
    // Validates: Requirements 1.5, 4.4
    public sealed class AdminAccessPropertyTests
    {
        private static readonly string[] SmallCities = { "Piripiri", "Teresina", "Parnaiba", "Fortaleza" };
        private static readonly string[] SmallStates = { "PI", "CE", "SP", "MA" };
        private static readonly string[] Tenants = { "tenant-alpha", "tenant-beta", "tenant-gamma", "" };

        private sealed class DummyTenantContext : ITenantContext
        {
            public string? TenantId { get; set; }
            public bool IsPrivatePlan { get; set; }
            public SingularSystem.Domain.Plans.PlanSubscription? Subscription => null;
        }

        [Property(MaxTest = 100)]
        public Property Admin_has_total_access_in_policy_and_pdf_generation(
            PositiveInt cityIdx,
            PositiveInt stateIdx,
            PositiveInt tenantIdx,
            bool isPrivatePlan,
            bool hasStudent)
        {
            var adminUser = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                City = SmallCities[cityIdx.Get % SmallCities.Length],
                State = SmallStates[stateIdx.Get % SmallStates.Length],
                TenantId = Tenants[tenantIdx.Get % Tenants.Length]
            };

            var student = PdfServiceTestFactory.CreateTestStudent(
                tenantId: "different-tenant-" + Guid.NewGuid(),
                city: SmallCities[(cityIdx.Get + 1) % SmallCities.Length],
                state: SmallStates[(stateIdx.Get + 1) % SmallStates.Length]);

            var tenantContext = new DummyTenantContext
            {
                IsPrivatePlan = isPrivatePlan,
                TenantId = adminUser.TenantId
            };

            var policy = new StudentAccessPolicy();
            var roles = new[] { "Admin" };

            // 1. Policy check: Admin must always have access
            var policyAllowed = policy.CanAccess(adminUser, student, roles, tenantContext);

            // 2. Service check: Admin receives Success even if document has no student or is in another tenant
            using var db = PdfServiceTestFactory.CreateDbContext();
            var docId = Guid.NewGuid();

            var pei = new PEI
            {
                ID = docId,
                StudentID = hasStudent ? student.ID : null
            };
            db.PEIs.Add(pei);
            if (hasStudent)
            {
                db.Students.Add(student);
            }
            db.SaveChanges();

            var (pdfService, tracker, issuerResolver, _) = PdfServiceTestFactory.CreatePdfService(db, new TenantContextService
            {
                IsPrivatePlan = isPrivatePlan,
                TenantId = adminUser.TenantId
            }, policy);

            var result = pdfService.GeneratePdfForUser(docId, PdfProfile.View, adminUser, roles)
                .GetAwaiter().GetResult();

            var serviceSuccess = result.Outcome == DocumentPdfOutcome.Success;
            var generatorCalled = tracker.CallCount == 1;

            return (policyAllowed && serviceSuccess && generatorCalled).ToProperty();
        }
    }
}
