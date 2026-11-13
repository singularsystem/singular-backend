using System;
using System.Collections.Generic;
using FsCheck;
using FsCheck.Xunit;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;
using SingularSystem.Services;
using SingularSystem.Tests.TestHelpers;

namespace SingularSystem.Tests
{
    // Feature: modulo-3-2b-isolamento-pdf-listagens, Property 7: Plano Público por documento segue a Regra_Geografica
    // Validates: Requirements 1.3, 1.4, 3.4
    public sealed class DocumentPublicGeographicPropertyTests
    {
        private static readonly string[] RolesSeed =
        {
            "Teacher", "Psychologist", "Psychopedagogue", "SocialWork",
            "Coordinator", "Principal", "Secretary", "MunicipalManager"
        };

        private static readonly string[] Cities = { "Piripiri", "Teresina", "Parnaiba", "Fortaleza" };
        private static readonly string[] States = { "PI", "Piaui", "CE", "Ceara" };

        [Property(MaxTest = 100)]
        public Property Public_plan_document_access_strictly_matches_geographic_rule(
            PositiveInt roleIdx,
            PositiveInt userCityIdx,
            PositiveInt userStateIdx,
            PositiveInt studentCityIdx,
            PositiveInt studentStateIdx,
            bool hasSchool,
            bool sameSchool)
        {
            var role = RolesSeed[roleIdx.Get % RolesSeed.Length];
            var userCity = Cities[userCityIdx.Get % Cities.Length];
            var userState = States[userStateIdx.Get % States.Length];
            var studentCity = Cities[studentCityIdx.Get % Cities.Length];
            var studentState = States[studentStateIdx.Get % States.Length];

            using var db = PdfServiceTestFactory.CreateDbContext();

            School? school = null;
            Guid? schoolId = null;
            if (hasSchool)
            {
                schoolId = Guid.NewGuid();
                school = new School
                {
                    ID = schoolId.Value,
                    Name = "Escola Municipal",
                    City = studentCity,
                    State = studentState
                };
                db.Schools.Add(school);
            }

            var student = PdfServiceTestFactory.CreateTestStudent(
                city: studentCity,
                state: studentState,
                schoolId: schoolId,
                school: school,
                tenantId: null);
            db.Students.Add(student);

            var docId = Guid.NewGuid();
            var doc = new PEI { ID = docId, StudentID = student.ID };
            db.PEIs.Add(doc);
            db.SaveChanges();

            var userSchoolId = sameSchool ? schoolId : (hasSchool ? Guid.NewGuid() : (Guid?)null);
            var user = new ApplicationUser
            {
                Id = "public-user",
                City = userCity,
                State = userState,
                SchoolID = userSchoolId,
                TenantId = null
            };

            var roles = new[] { role };
            var tenantContext = new TenantContextService { IsPrivatePlan = false, TenantId = null };

            var expectedAccess = LegacyAccessHelper.LegacyHasStudentAccess(user, student, roles, tenantContext);

            var (pdfService, tracker, _, _) = PdfServiceTestFactory.CreatePdfService(db, tenantContext);
            var result = pdfService.GeneratePdfForUser(docId, PdfProfile.View, user, roles)
                .GetAwaiter().GetResult();

            if (expectedAccess)
            {
                var ok = result.Outcome == DocumentPdfOutcome.Success && tracker.CallCount == 1;
                return ok.ToProperty();
            }
            else
            {
                var ok = result.Outcome == DocumentPdfOutcome.Forbidden && tracker.CallCount == 0;
                return ok.ToProperty();
            }
        }
    }
}
