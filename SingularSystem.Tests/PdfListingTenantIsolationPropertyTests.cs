using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using FsCheck.Xunit;
using SingularSystem.Models.Entities;
using SingularSystem.Services;
using SingularSystem.Tests.TestHelpers;

namespace SingularSystem.Tests
{
    // Feature: modulo-3-2b-isolamento-pdf-listagens, Property 8: Listagens em PDF ficam dentro do tenant e parâmetros só refinam
    // Validates: Requirements 5.1, 5.2, 5.3, 5.6, 5.8, 6.1, 6.2, 6.3, 6.4, 16.2, 16.3, 16.4
    public sealed class PdfListingTenantIsolationPropertyTests
    {
        private static readonly string[] SampleCities = { "Piripiri", "Teresina", "Parnaiba", "" };
        private static readonly string[] SampleStates = { "PI", "CE", "" };
        private static readonly string[] SampleStatuses = { "Todos", "Completo", "Incompleto", "Não Iniciado" };
        private static readonly string[] SamplePendingProf = { "Todos", "Psicologo", "Psicopedagogo", "AssistenteSocial" };

        [Property(MaxTest = 100)]
        public Property Pdf_listings_stay_within_tenant_and_parameters_only_refine(
            PositiveInt cityIdx,
            PositiveInt stateIdx,
            PositiveInt statusIdx,
            PositiveInt profIdx,
            bool malformedSchoolId,
            bool foreignSchoolId)
        {
            var targetTenant = "tenant-A";
            var otherTenant = "tenant-B";

            using var db = PdfServiceTestFactory.CreateDbContext();

            var schoolA = new School { ID = Guid.NewGuid(), Name = "School A", City = "Piripiri", State = "PI" };
            var schoolB = new School { ID = Guid.NewGuid(), Name = "School B", City = "Piripiri", State = "PI" };
            db.Schools.AddRange(schoolA, schoolB);

            for (int i = 0; i < 5; i++)
            {
                db.Students.Add(PdfServiceTestFactory.CreateTestStudent(
                    name: $"Student A{i}",
                    city: "Piripiri",
                    state: "PI",
                    tenantId: targetTenant,
                    schoolId: schoolA.ID,
                    school: schoolA));

                db.Students.Add(PdfServiceTestFactory.CreateTestStudent(
                    name: $"Student B{i}",
                    city: "Piripiri",
                    state: "PI",
                    tenantId: otherTenant,
                    schoolId: schoolB.ID,
                    school: schoolB));
            }
            db.SaveChanges();

            string schoolIdParam = "";
            if (malformedSchoolId)
            {
                schoolIdParam = "not-a-guid";
            }
            else if (foreignSchoolId)
            {
                schoolIdParam = schoolB.ID.ToString();
            }

            var city = SampleCities[cityIdx.Get % SampleCities.Length];
            var state = SampleStates[stateIdx.Get % SampleStates.Length];
            var status = SampleStatuses[statusIdx.Get % SampleStatuses.Length];
            var prof = SamplePendingProf[profIdx.Get % SamplePendingProf.Length];

            var tenantContext = new TenantContextService
            {
                IsPrivatePlan = true,
                TenantId = targetTenant
            };

            var (pdfService, _, _, _) = PdfServiceTestFactory.CreatePdfService(db, tenantContext);

            // Test all 4 builders
            var q1 = pdfService.BuildStudentsListQuery(state, city, schoolIdParam, "");
            var res1 = q1.ToList();

            var q2 = pdfService.BuildReportsAeeQuery(state, city, schoolIdParam, "", "");
            var res2 = q2.ToList();

            var q3 = pdfService.BuildCaseStudiesTrackingQuery(state, city, schoolIdParam, "", status, null, null);
            var res3 = q3.ToList();

            var q4 = pdfService.BuildBiopsychosocialTrackingQuery(state, city, schoolIdParam, "", status, prof, null, null);
            var res4 = q4.ToList();

            bool AllInTargetTenant(List<Student> students) =>
                students.All(s => s.TenantId == targetTenant);

            var ok = AllInTargetTenant(res1)
                  && AllInTargetTenant(res2)
                  && AllInTargetTenant(res3)
                  && AllInTargetTenant(res4);

            if (malformedSchoolId)
            {
                // In private plan, malformed schoolID yields empty list
                ok = ok && res1.Count == 0 && res2.Count == 0 && res3.Count == 0 && res4.Count == 0;
            }

            return ok.ToProperty();
        }
    }
}
