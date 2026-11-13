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
    // Feature: modulo-3-2b-isolamento-pdf-listagens, Property 6: Documento sem aluno é negado ao não-Admin`
    // Validates: Requirements 1.7
    public sealed class DocumentWithoutStudentPropertyTests
    {
        private static readonly string[] NonAdminRoles =
        {
            "Teacher", "Psychologist", "Psychopedagogue", "SocialWork",
            "Coordinator", "Principal", "Secretary", "MunicipalManager",
            "AutonomousProfessional", "SchoolAdmin"
        };

        [Property(MaxTest = 100)]
        public Property Document_without_student_is_denied_to_non_admin(
            PositiveInt roleIdx,
            bool isPrivatePlan,
            bool studentIdIsNull)
        {
            var role = NonAdminRoles[roleIdx.Get % NonAdminRoles.Length];

            using var db = PdfServiceTestFactory.CreateDbContext();
            var docId = Guid.NewGuid();

            var doc = new PEI
            {
                ID = docId,
                StudentID = studentIdIsNull ? null : Guid.NewGuid() // pointing to non-existent student
            };
            db.PEIs.Add(doc);
            db.SaveChanges();

            var user = new ApplicationUser
            {
                Id = "non-admin-user",
                City = "Piripiri",
                State = "PI",
                TenantId = "tenant-test"
            };

            var tenantContext = new TenantContextService
            {
                IsPrivatePlan = isPrivatePlan,
                TenantId = user.TenantId
            };

            var (pdfService, tracker, _, _) = PdfServiceTestFactory.CreatePdfService(db, tenantContext);

            var result = pdfService.GeneratePdfForUser(docId, PdfProfile.View, user, new[] { role })
                .GetAwaiter().GetResult();

            var isForbidden = result.Outcome == DocumentPdfOutcome.Forbidden;
            var generatorNeverCalled = tracker.CallCount == 0;

            return (isForbidden && generatorNeverCalled).ToProperty();
        }
    }
}
