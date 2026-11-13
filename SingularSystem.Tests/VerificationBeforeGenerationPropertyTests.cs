using System;
using FsCheck;
using FsCheck.Xunit;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;
using SingularSystem.Services;
using SingularSystem.Tests.TestHelpers;

namespace SingularSystem.Tests
{
    // Feature: modulo-3-2b-isolamento-pdf-listagens, Property 5: Verificação antes da geração
    // Validates: Requirements 1.8
    public sealed class VerificationBeforeGenerationPropertyTests
    {
        [Property(MaxTest = 100)]
        public Property Verification_happens_strictly_before_pdf_generation(
            bool isAllowed,
            bool documentExists)
        {
            using var db = PdfServiceTestFactory.CreateDbContext();
            var docId = Guid.NewGuid();
            var tenantA = "tenant-A";

            var student = PdfServiceTestFactory.CreateTestStudent(
                tenantId: tenantA,
                city: "Piripiri",
                state: "PI");
            db.Students.Add(student);

            if (documentExists)
            {
                db.PEIs.Add(new PEI { ID = docId, StudentID = student.ID });
            }
            db.SaveChanges();

            var userTenant = isAllowed ? tenantA : "tenant-B";
            var user = new ApplicationUser
            {
                Id = "test-user",
                TenantId = userTenant,
                City = "Piripiri",
                State = "PI"
            };

            var tenantContext = new TenantContextService
            {
                IsPrivatePlan = true,
                TenantId = userTenant
            };

            var (pdfService, tracker, issuerResolver, validationService) =
                PdfServiceTestFactory.CreatePdfService(db, tenantContext);

            var result = pdfService.GeneratePdfForUser(docId, PdfProfile.View, user, new[] { "Teacher" })
                .GetAwaiter().GetResult();

            if (!documentExists)
            {
                var correctOutcome = result.Outcome == DocumentPdfOutcome.NotFound;
                var zeroCalls = tracker.CallCount == 0
                             && issuerResolver.CallCount == 0
                             && validationService.GetOrIssueCallCount == 0;
                return (correctOutcome && zeroCalls).ToProperty();
            }

            if (!isAllowed)
            {
                var correctOutcome = result.Outcome == DocumentPdfOutcome.Forbidden;
                var zeroCalls = tracker.CallCount == 0
                             && issuerResolver.CallCount == 0
                             && validationService.GetOrIssueCallCount == 0;
                return (correctOutcome && zeroCalls).ToProperty();
            }
            else
            {
                var correctOutcome = result.Outcome == DocumentPdfOutcome.Success;
                var exactlyOneCall = tracker.CallCount == 1
                                  && issuerResolver.CallCount == 1;
                return (correctOutcome && exactlyOneCall).ToProperty();
            }
        }
    }
}
