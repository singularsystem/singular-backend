using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.EntityFrameworkCore;
using SingularSystem.Models.Entities;
using SingularSystem.Services;
using SingularSystem.Tests.TestHelpers;

namespace SingularSystem.Tests
{
    // Feature: modulo-3-2b-isolamento-pdf-listagens, Property 10: Plano Público e Admin inalterados nas listagens
    // Validates: Requirements 3.2, 5.7, 6.7, 16.5, 16.6
    public sealed class PdfListingPublicUnchangedPropertyTests
    {
        private static readonly string[] SampleCities = { "Piripiri", "Teresina", "" };
        private static readonly string[] SampleStates = { "PI", "CE", "" };

        private static IQueryable<Student> LegacyStudentsQuery(
            AppDbContext db,
            string state,
            string city,
            string schoolID)
        {
            var query = db.Students.AsNoTracking().Include(s => s.School).Where(s => s.IsActive);

            if (!string.IsNullOrWhiteSpace(state))
            {
                query = query.Where(s => (s.State == state) || (s.School != null && s.School.State == state));
            }

            if (!string.IsNullOrWhiteSpace(city))
            {
                query = query.Where(s => (s.City == city) || (s.School != null && s.School.City == city));
            }

            if (!string.IsNullOrWhiteSpace(schoolID))
            {
                var guid = Guid.Parse(schoolID);
                query = query.Where(s => (s.SchoolID == guid) || (s.School != null && s.School.ID == guid));
            }

            return query;
        }

        [Property(MaxTest = 100)]
        public Property Public_and_admin_listings_are_unchanged_and_match_legacy(
            PositiveInt cityIdx,
            PositiveInt stateIdx)
        {
            using var db = PdfServiceTestFactory.CreateDbContext();

            for (int i = 0; i < 6; i++)
            {
                db.Students.Add(PdfServiceTestFactory.CreateTestStudent(
                    name: $"Student {5 - i}",
                    city: SampleCities[i % SampleCities.Length],
                    state: SampleStates[i % SampleStates.Length],
                    tenantId: null));
            }
            db.SaveChanges();

            var city = SampleCities[cityIdx.Get % SampleCities.Length];
            var state = SampleStates[stateIdx.Get % SampleStates.Length];

            // In public plan: IsPrivatePlan == false
            var publicContext = new TenantContextService { IsPrivatePlan = false, TenantId = null };
            var (pdfService, _, _, _) = PdfServiceTestFactory.CreatePdfService(db, publicContext);

            var actualList = pdfService.BuildStudentsListQuery(state, city, "", "")
                .OrderBy(s => s.FullName)
                .Select(s => s.ID)
                .ToList();

            var legacyList = LegacyStudentsQuery(db, state, city, "")
                .OrderBy(s => s.FullName)
                .Select(s => s.ID)
                .ToList();

            var sameCount = actualList.Count == legacyList.Count;
            var sameOrder = actualList.SequenceEqual(legacyList);

            return (sameCount && sameOrder).ToProperty();
        }
    }
}
