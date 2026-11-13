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
    // Feature: modulo-3-2b-isolamento-pdf-listagens, Property 9: Monotonicidade do refinamento
    // Validates: Requirements 5.2, 5.5
    public sealed class PdfListingMonotonicityPropertyTests
    {
        private static readonly string[] SampleCities = { "Piripiri", "Teresina", "Parnaiba" };
        private static readonly string[] SampleStates = { "PI", "CE" };

        [Property(MaxTest = 100)]
        public Property Refined_query_result_is_subset_of_unrefined_query_result(
            PositiveInt cityIdx,
            PositiveInt stateIdx)
        {
            var tenant = "tenant-mono-A";

            using var db = PdfServiceTestFactory.CreateDbContext();

            for (int i = 0; i < 10; i++)
            {
                var city = SampleCities[i % SampleCities.Length];
                var state = SampleStates[i % SampleStates.Length];
                db.Students.Add(PdfServiceTestFactory.CreateTestStudent(
                    name: $"Student {i}",
                    city: city,
                    state: state,
                    tenantId: tenant));
            }
            db.SaveChanges();

            var paramCity = SampleCities[cityIdx.Get % SampleCities.Length];
            var paramState = SampleStates[stateIdx.Get % SampleStates.Length];

            var tenantContext = new TenantContextService
            {
                IsPrivatePlan = true,
                TenantId = tenant
            };

            var (pdfService, _, _, _) = PdfServiceTestFactory.CreatePdfService(db, tenantContext);

            // 1. Unrefined queries (empty parameters)
            var base1 = pdfService.BuildStudentsListQuery("", "", "", "").Select(s => s.ID).ToHashSet();
            var base2 = pdfService.BuildReportsAeeQuery("", "", "", "", "").Select(s => s.ID).ToHashSet();
            var base3 = pdfService.BuildCaseStudiesTrackingQuery("", "", "", "", "", null, null).Select(s => s.ID).ToHashSet();
            var base4 = pdfService.BuildBiopsychosocialTrackingQuery("", "", "", "", "", "", null, null).Select(s => s.ID).ToHashSet();

            // 2. Refined queries
            var ref1 = pdfService.BuildStudentsListQuery(paramState, paramCity, "", "").Select(s => s.ID).ToList();
            var ref2 = pdfService.BuildReportsAeeQuery(paramState, paramCity, "", "", "").Select(s => s.ID).ToList();
            var ref3 = pdfService.BuildCaseStudiesTrackingQuery(paramState, paramCity, "", "", "", null, null).Select(s => s.ID).ToList();
            var ref4 = pdfService.BuildBiopsychosocialTrackingQuery(paramState, paramCity, "", "", "", "", null, null).Select(s => s.ID).ToList();

            var ok1 = ref1.All(id => base1.Contains(id));
            var ok2 = ref2.All(id => base2.Contains(id));
            var ok3 = ref3.All(id => base3.Contains(id));
            var ok4 = ref4.All(id => base4.Contains(id));

            return (ok1 && ok2 && ok3 && ok4).ToProperty();
        }
    }
}
