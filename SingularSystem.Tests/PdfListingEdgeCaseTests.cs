using System;
using System.Linq;
using SingularSystem.Models.Entities;
using SingularSystem.Services;
using SingularSystem.Tests.TestHelpers;
using Xunit;

namespace SingularSystem.Tests
{
    public sealed class PdfListingEdgeCaseTests
    {
        [Fact]
        public void PrivatePlan_MalformedSchoolId_ReturnsEmptyList_WithoutThrowing()
        {
            using var db = PdfServiceTestFactory.CreateDbContext();
            db.Students.Add(new Student
            {
                ID = Guid.NewGuid(),
                FullName = "Student Alpha",
                City = "Piripiri",
                State = "PI",
                TenantId = "tenant-priv-1",
                IsActive = true
            });
            db.SaveChanges();

            var tenantContext = new TenantContextService { IsPrivatePlan = true, TenantId = "tenant-priv-1" };
            var (pdfService, _, _, _) = PdfServiceTestFactory.CreatePdfService(db, tenantContext);

            var query = pdfService.BuildStudentsListQuery("", "", "not-a-guid", "");
            var results = query.ToList();

            Assert.Empty(results);
        }

        [Fact]
        public void PrivatePlan_ForeignTenantSchoolId_ReturnsZeroRecords()
        {
            using var db = PdfServiceTestFactory.CreateDbContext();
            var foreignSchoolId = Guid.NewGuid();

            db.Students.Add(new Student
            {
                ID = Guid.NewGuid(),
                FullName = "Student Alpha",
                City = "Piripiri",
                State = "PI",
                TenantId = "tenant-priv-1",
                SchoolID = Guid.NewGuid(),
                IsActive = true
            });
            db.SaveChanges();

            var tenantContext = new TenantContextService { IsPrivatePlan = true, TenantId = "tenant-priv-1" };
            var (pdfService, _, _, _) = PdfServiceTestFactory.CreatePdfService(db, tenantContext);

            var query = pdfService.BuildStudentsListQuery("", "", foreignSchoolId.ToString(), "");
            var results = query.ToList();

            Assert.Empty(results);
        }

        [Fact]
        public void PrivatePlan_AbsentParameters_ReturnsAllStudentsOfTenant()
        {
            using var db = PdfServiceTestFactory.CreateDbContext();
            var targetTenant = "tenant-priv-all";

            db.Students.Add(new Student
            {
                ID = Guid.NewGuid(),
                FullName = "Student 1",
                City = "Piripiri",
                State = "PI",
                TenantId = targetTenant,
                IsActive = true
            });
            db.Students.Add(new Student
            {
                ID = Guid.NewGuid(),
                FullName = "Student 2",
                City = "Piripiri",
                State = "PI",
                TenantId = targetTenant,
                IsActive = true
            });
            db.Students.Add(new Student
            {
                ID = Guid.NewGuid(),
                FullName = "Student 3",
                City = "Piripiri",
                State = "PI",
                TenantId = "other-tenant",
                IsActive = true
            });
            db.SaveChanges();

            var tenantContext = new TenantContextService { IsPrivatePlan = true, TenantId = targetTenant };
            var (pdfService, _, _, _) = PdfServiceTestFactory.CreatePdfService(db, tenantContext);

            var query = pdfService.BuildStudentsListQuery("", "", "", "");
            var results = query.ToList();

            Assert.Equal(2, results.Count);
            Assert.All(results, s => Assert.Equal(targetTenant, s.TenantId));
        }

        [Fact]
        public void PublicPlan_MalformedSchoolId_ThrowsFormatException()
        {
            using var db = PdfServiceTestFactory.CreateDbContext();
            db.Students.Add(new Student
            {
                ID = Guid.NewGuid(),
                FullName = "Student Public",
                City = "Piripiri",
                State = "PI",
                TenantId = null,
                IsActive = true
            });
            db.SaveChanges();

            var publicContext = new TenantContextService { IsPrivatePlan = false, TenantId = null };
            var (pdfService, _, _, _) = PdfServiceTestFactory.CreatePdfService(db, publicContext);

            Assert.Throws<FormatException>(() =>
            {
                var query = pdfService.BuildStudentsListQuery("", "", "malformed-school-id", "");
                _ = query.ToList();
            });
        }
    }
}
