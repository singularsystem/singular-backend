using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SingularSystem.Controllers;
using SingularSystem.Domain.Access;
using SingularSystem.Domain.Tenancy;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;
using SingularSystem.Services;
using SingularSystem.Tests.TestHelpers;

namespace SingularSystem.Tests
{
    // Feature: modulo-3-2b-isolamento-pdf-listagens, Property 3: Isolamento de tenant em documento
    // Validates: Requirements 1.1, 1.2, 1.6, 1.9, 8.2
    public sealed class DocumentTenantIsolationPropertyTests
    {
        private static readonly string[] DocKinds = { "PEI", "PAEE", "Report", "RegularTeacherEvaluationDocument" };

        [Property(MaxTest = 100)]
        public Property Document_tenant_isolation_is_enforced(
            PositiveInt docKindIdx,
            bool download)
        {
            var docKind = DocKinds[docKindIdx.Get % DocKinds.Length];
            var tenantA = "tenant-escola-A";
            var tenantB = "tenant-escola-B";

            using var db = PdfServiceTestFactory.CreateDbContext();

            var student = PdfServiceTestFactory.CreateTestStudent(
                tenantId: tenantA,
                name: "Student Tenant A",
                city: "Piripiri",
                state: "PI");
            db.Students.Add(student);

            var docId = Guid.NewGuid();
            switch (docKind)
            {
                case "PEI":
                    db.PEIs.Add(new PEI { ID = docId, StudentID = student.ID });
                    break;
                case "PAEE":
                    var portage = new PortageGuide { ID = Guid.NewGuid(), StudentID = student.ID };
                    db.PortageGuides.Add(portage);
                    db.PAEEs.Add(new PAEE
                    {
                        ID = docId,
                        StudentID = student.ID,
                        PortageID = portage.ID,
                        AcademicRecord = "",
                        MedicalReport = "",
                        Medications = "",
                        FollowUp = "",
                        CompositionOfAssisting = "",
                        AdditionalObservations = ""
                    });
                    break;
                case "Report":
                    db.Reports.Add(new Report { ID = docId, StudentID = student.ID });
                    break;
                case "RegularTeacherEvaluationDocument":
                    var screening = new RegularTeacherScreening
                    {
                        ID = Guid.NewGuid(),
                        StudentID = student.ID
                    };
                    var questionnaire = new RegularTeacherQuestionnaire
                    {
                        ID = Guid.NewGuid(),
                        StudentID = student.ID,
                        ClassroomDevelopment = "",
                        ClassroomBehavior = "",
                        MainDifficulties = "",
                        LearningCharacteristics = "",
                        ReadingAndWritingDescription = "",
                        LogicalMathematicalReasoningDescription = "",
                        CompletesSchoolActivities = "",
                        CompletesHomework = "",
                        ReactionToFrustration = "",
                        InappropriateBehaviorDescription = "",
                        DifficultyWorkingInGroup = "",
                        DifficultyOrganizingTasks = "",
                        PeerAcceptance = "",
                        BehaviorObservations = "",
                        DevelopmentObservations = ""
                    };
                    db.RegularTeacherScreenings.Add(screening);
                    db.RegularTeacherQuestionnaires.Add(questionnaire);
                    db.RegularTeacherEvaluationDocuments.Add(new RegularTeacherEvaluationDocument
                    {
                        ID = docId,
                        StudentID = student.ID,
                        RegularTeacherScreeningID = screening.ID,
                        RegularTeacherQuestionnaireID = questionnaire.ID
                    });
                    break;
            }
            db.SaveChanges();

            var roles = new[] { "Teacher" };
            var userTenantA = new ApplicationUser { Id = "user-A", TenantId = tenantA, City = "Piripiri", State = "PI" };
            var userTenantB = new ApplicationUser { Id = "user-B", TenantId = tenantB, City = "Piripiri", State = "PI" };

            // 1. Service evaluation for userTenantA (same tenant) -> Success
            var tenantContextA = new TenantContextService { IsPrivatePlan = true, TenantId = tenantA };
            var (serviceA, _, _, _) = PdfServiceTestFactory.CreatePdfService(db, tenantContextA);
            var resA = serviceA.GeneratePdfForUser(docId, PdfProfile.View, userTenantA, roles)
                .GetAwaiter().GetResult();
            var successForA = resA.Outcome == DocumentPdfOutcome.Success;

            // 2. Service evaluation for userTenantB (different tenant) -> Forbidden
            var tenantContextB = new TenantContextService { IsPrivatePlan = true, TenantId = tenantB };
            var (serviceB, _, _, _) = PdfServiceTestFactory.CreatePdfService(db, tenantContextB);
            var resB = serviceB.GeneratePdfForUser(docId, PdfProfile.View, userTenantB, roles)
                .GetAwaiter().GetResult();
            var forbiddenForB = resB.Outcome == DocumentPdfOutcome.Forbidden;

            // 3. Service evaluation for non-existent document -> NotFound
            var resMissing = serviceA.GeneratePdfForUser(Guid.NewGuid(), PdfProfile.View, userTenantA, roles)
                .GetAwaiter().GetResult();
            var notFoundForMissing = resMissing.Outcome == DocumentPdfOutcome.NotFound;

            return (successForA && forbiddenForB && notFoundForMissing).ToProperty();
        }
    }
}
