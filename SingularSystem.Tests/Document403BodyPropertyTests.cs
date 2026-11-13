using System;
using System.Text.Json;
using FsCheck;
using FsCheck.Xunit;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;
using SingularSystem.Services;
using SingularSystem.Tests.TestHelpers;

namespace SingularSystem.Tests
{
    // Feature: modulo-3-2b-isolamento-pdf-listagens, Property 4: Corpo do 403 é uniforme e sem vazamento
    // Validates: Requirements 2.1, 2.2, 2.3, 8.1
    public sealed class Document403BodyPropertyTests
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        [Property(MaxTest = 100)]
        public Property Document_403_body_is_uniform_and_does_not_leak_sentinels(
            PositiveInt scenarioIdx,
            PositiveInt seed)
        {
            var sentinelStudentName = $"SENTINEL_STUDENT_{seed.Get}";
            var sentinelTenantId = $"SENTINEL_TENANT_{seed.Get}";
            var sentinelCity = $"SENTINEL_CITY_{seed.Get}";
            var sentinelState = $"SENTINEL_STATE_{seed.Get}";

            using var db = PdfServiceTestFactory.CreateDbContext();

            var student = new Student
            {
                ID = Guid.NewGuid(),
                FullName = sentinelStudentName,
                DateOfBirth = new DateTime(2010, 1, 1),
                Address = "Rua Teste, 100",
                NameOfPersonResponsible = "Responsavel",
                TenantId = sentinelTenantId,
                City = sentinelCity,
                State = sentinelState
            };
            db.Students.Add(student);

            var docId = Guid.NewGuid();
            var doc = new PEI { ID = docId, StudentID = student.ID };
            db.PEIs.Add(doc);
            db.SaveChanges();

            var scenario = scenarioIdx.Get % 4;
            var tenantContext = new TenantContextService();
            var user = new ApplicationUser { Id = "test-user" };

            switch (scenario)
            {
                case 0: // Other tenant
                    tenantContext.IsPrivatePlan = true;
                    tenantContext.TenantId = "different-tenant-" + Guid.NewGuid();
                    user.TenantId = tenantContext.TenantId;
                    break;
                case 1: // Student without tenant in private plan
                    tenantContext.IsPrivatePlan = true;
                    tenantContext.TenantId = "tenant-target";
                    student.TenantId = null;
                    db.SaveChanges();
                    break;
                case 2: // Geographic mismatch in public plan
                    tenantContext.IsPrivatePlan = false;
                    user.City = "MismatchCity";
                    user.State = "MismatchState";
                    break;
                case 3: // Document without student
                    tenantContext.IsPrivatePlan = false;
                    doc.StudentID = null;
                    db.SaveChanges();
                    break;
            }

            var (pdfService, tracker, _, _) = PdfServiceTestFactory.CreatePdfService(db, tenantContext);
            var result = pdfService.GeneratePdfForUser(docId, PdfProfile.View, user, new[] { "Teacher" })
                .GetAwaiter().GetResult();

            var isForbidden = result.Outcome == DocumentPdfOutcome.Forbidden;

            var deniedPayload = DocumentAccessDenied.Body();
            var json = JsonSerializer.Serialize(deniedPayload, JsonOptions);

            var noLeakedStudentName = !json.Contains(sentinelStudentName, StringComparison.OrdinalIgnoreCase);
            var noLeakedTenantId = !json.Contains(sentinelTenantId, StringComparison.OrdinalIgnoreCase);
            var noLeakedCity = !json.Contains(sentinelCity, StringComparison.OrdinalIgnoreCase);
            var noLeakedState = !json.Contains(sentinelState, StringComparison.OrdinalIgnoreCase);

            var expectedMessage = "Você não tem permissão para acessar este documento.";
            var hasUniformMessage = deniedPayload.Message == expectedMessage && deniedPayload.Success == false && deniedPayload.Data == null;

            return (isForbidden && noLeakedStudentName && noLeakedTenantId && noLeakedCity && noLeakedState && hasUniformMessage).ToProperty();
        }
    }
}
