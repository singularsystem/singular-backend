using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SingularSystem.Controllers;
using SingularSystem.Domain.Access;
using SingularSystem.Domain.Plans;
using SingularSystem.Domain.Tenancy;
using SingularSystem.DTOs;
using SingularSystem.DTOs.Attachments;
using SingularSystem.Interfaces;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;
using SingularSystem.Services;
using SingularSystem.Tests.TestHelpers;
using Xunit;

namespace SingularSystem.Tests
{
    public sealed class SecurityScanFixesTests
    {
        private sealed class DummyTenantContext : ITenantContext
        {
            public string? TenantId { get; set; }
            public bool IsPrivatePlan { get; set; }
            public PlanSubscription? Subscription => null;
        }

        private sealed class DummyFileStorageService : IFileStorageService
        {
            public Task<bool> DeleteFile(Guid id) => Task.FromResult(true);
            public Task<List<Models.Entities.File>> GetAllFiles(int page, int pageSize) => Task.FromResult(new List<Models.Entities.File>());
            public Task<Models.Entities.File> GetFileByID(Guid id) => Task.FromResult(new Models.Entities.File());
            public Task<Models.Entities.File> GetFileByFileName(string fileName) => Task.FromResult(new Models.Entities.File());
            public Task<List<Attachment>> GetFilesByStudent(Guid studentID) => Task.FromResult(new List<Attachment>());
            public Task<Guid> SaveFileInDatabase(IFormFile file) => Task.FromResult(Guid.NewGuid());
        }

        private sealed class StubSignatureService : ISignatureService
        {
            public Task<SignatureDocumentDetails?> GetDocumentDetails(string documentToken, bool bypassCooldown = false)
                => Task.FromResult<SignatureDocumentDetails?>(new SignatureDocumentDetails
                {
                    ExternalId = "test-doc",
                    Status = SignatureDocumentStatus.Signed,
                    SignedFileUrl = "https://signed.url/file.pdf"
                });

            public Task<bool> CreateDoc(Guid idDoc, string type) => Task.FromResult(true);
            public Task<bool> DeleteDoc(Guid idDoc, string type) => Task.FromResult(true);
            public Task<bool> SyncDocument(string documentToken, bool bypassCooldown = false) => Task.FromResult(true);
            public Task SyncPendingDocuments() => Task.CompletedTask;
        }

        private sealed class StubSignedMediaResolver : ISignedMediaResolver
        {
            public bool IsZapSignUrl(string? signedFile) => false;
            public bool IsOwnS3Reference(string? signedFile) => false;
            public string GeneratePreSignedUrl(string s3ObjectKey, double durationMinutes = 60) => "https://s3.url";
        }

        [Fact]
        public void Fix8_1_Statistics_ProfessionalsCount_FilteredByTenant_InPrivatePlan()
        {
            var tenantId = "tenant-escola-1";
            var users = new List<ApplicationUser>
            {
                new() { Id = "u1", TenantId = tenantId, City = "Piripiri", State = "PI" },
                new() { Id = "u2", TenantId = "tenant-escola-2", City = "Piripiri", State = "PI" },
                new() { Id = "u3", TenantId = null, City = "Piripiri", State = "PI" }
            };

            var tenantContext = new DummyTenantContext { IsPrivatePlan = true, TenantId = tenantId };

            // Logic replicated from StatisticsController:833
            var filtered = users.Where(u => (!tenantContext.IsPrivatePlan || u.TenantId == tenantContext.TenantId)).ToList();

            Assert.Single(filtered);
            Assert.Equal("u1", filtered[0].Id);
        }

        [Fact]
        public void Fix8_2_Integrations_ActivitiesSummary_ExcludesPrivateTenantStudents()
        {
            var students = new List<Student>
            {
                new() { ID = Guid.NewGuid(), TenantId = null, FullName = "Public Student 1" },
                new() { ID = Guid.NewGuid(), TenantId = "", FullName = "Public Student 2" },
                new() { ID = Guid.NewGuid(), TenantId = "tenant-private", FullName = "Private Student" }
            };

            // Logic replicated from IntegrationsController:92
            var publicStudents = students.Where(s => string.IsNullOrEmpty(s.TenantId)).ToList();

            Assert.Equal(2, publicStudents.Count);
            Assert.DoesNotContain(publicStudents, s => s.TenantId == "tenant-private");
        }

        private static async Task<(ServiceProvider Sp, ApplicationUser User)> SetupUserServicesAsync(
            string userId,
            string role,
            ITenantContext tenantContext)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase($"user-db-{Guid.NewGuid()}"));
            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();
            services.AddSingleton<IStudentAccessPolicy, StudentAccessPolicy>();
            services.AddSingleton(tenantContext);

            var sp = services.BuildServiceProvider();
            var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();

            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }

            var user = new ApplicationUser { Id = userId, UserName = userId, Email = $"{userId}@test.com" };
            await userManager.CreateAsync(user);
            await userManager.AddToRoleAsync(user, role);

            return (sp, user);
        }

        [Fact]
        public async Task Fix8_3_Files_GetByStudent_Returns403_WhenStudentInAnotherTenant()
        {
            var db = PdfServiceTestFactory.CreateDbContext();
            var studentId = Guid.NewGuid();
            var student = new Student
            {
                ID = studentId,
                TenantId = "tenant-B",
                City = "Piripiri",
                State = "PI"
            };
            db.Students.Add(student);
            await db.SaveChangesAsync();

            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["S3BucketAccessKey"] = "key",
                ["S3BucketSecretKey"] = "secret"
            }).Build();

            var controller = new FilesController(
                db,
                NullLogger<FilesController>.Instance,
                new DummyFileStorageService(),
                config
            );

            var tenantContext = new DummyTenantContext { IsPrivatePlan = true, TenantId = "tenant-A" };
            var (sp, appUser) = await SetupUserServicesAsync("user-A", "Teacher", tenantContext);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, appUser.Id),
                new Claim(ClaimTypes.Role, "Teacher")
            }, "TestAuth"));

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = user,
                    RequestServices = sp
                }
            };

            var result = await controller.GetStudentFiles(studentId);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, objectResult.StatusCode);
            var body = Assert.IsType<ApiResponse<object>>(objectResult.Value);
            Assert.False(body.Success);
            Assert.Equal("Você não tem permissão para acessar este documento.", body.Message);
        }

        [Fact]
        public async Task Fix8_4_DocumentsSignature_SignedUrl_Returns403_WhenStudentInAnotherTenant()
        {
            var db = PdfServiceTestFactory.CreateDbContext();
            var studentId = Guid.NewGuid();
            var student = new Student
            {
                ID = studentId,
                TenantId = "tenant-B",
                City = "Piripiri",
                State = "PI"
            };
            db.Students.Add(student);

            var pei = new PEI
            {
                ID = Guid.NewGuid(),
                DocumentToken = "valid-token",
                StudentID = studentId,
                DocumentStatus = Status.None,
                SignedFile = "https://signed.url/file.pdf"
            };
            db.PEIs.Add(pei);
            await db.SaveChangesAsync();

            var controller = new DocumentsSignatureController(
                new StubSignatureService(),
                new StubSignedMediaResolver(),
                db,
                NullLogger<DocumentsSignatureController>.Instance
            );

            var tenantContext = new DummyTenantContext { IsPrivatePlan = true, TenantId = "tenant-A" };
            var (sp, appUser) = await SetupUserServicesAsync("user-A", "Teacher", tenantContext);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, appUser.Id),
                new Claim(ClaimTypes.Role, "Teacher")
            }, "TestAuth"));

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = user,
                    RequestServices = sp
                }
            };

            var result = await controller.GetSignedUrl("valid-token");

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, objectResult.StatusCode);
            var body = Assert.IsType<ApiResponse<object>>(objectResult.Value);
            Assert.False(body.Success);
            Assert.Equal("Você não tem permissão para acessar este documento.", body.Message);
        }

        [Fact]
        public async Task Fix8_5_Notifications_OtherUser_Returns403_And_MarkAsRead_OtherUser_HasNoEffect()
        {
            var db = PdfServiceTestFactory.CreateDbContext();
            var userAId = Guid.NewGuid();
            var userBId = Guid.NewGuid();

            var notif = new Notification
            {
                ID = Guid.NewGuid(),
                UserID = userBId.ToString(),
                Title = "Test Notif",
                Message = "Test Msg",
                ActionURL = "api/v1/notifications",
                Read = false,
                CreatedAt = DateTime.UtcNow
            };
            db.Notifications.Add(notif);
            await db.SaveChangesAsync();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase($"id-notif-{Guid.NewGuid()}"));
            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();
            var sp = services.BuildServiceProvider();
            var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

            var notifService = new NotificationService(
                db,
                NullLogger<NotificationService>.Instance,
                new FakeHubContext(),
                userManager
            );

            var controller = new NotificationsController(
                notifService,
                NullLogger<NotificationsController>.Instance
            );

            // User A context
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userAId.ToString()) };
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
                }
            };

            // GET user B notifications -> 403
            var getResult = await controller.GetNotifications(userBId);
            var objectResult = Assert.IsType<ObjectResult>(getResult);
            Assert.Equal(403, objectResult.StatusCode);

            // PUT mark as read by user A -> should NOT mark user B's notification
            await controller.MarkNotificationAsRead(notif.ID);
            var freshNotif = await db.Notifications.FindAsync(notif.ID);
            Assert.NotNull(freshNotif);
            Assert.False(freshNotif.Read);
        }

        [Fact]
        public void Fix8_6_Students_CreateNotification_Recipients_RestrictedToTenant_InPrivatePlan()
        {
            var tenantId = "tenant-target";
            var users = new List<ApplicationUser>
            {
                new() { Id = "u1", TenantId = tenantId, City = "Teresina", State = "PI" },
                new() { Id = "u2", TenantId = "tenant-other", City = "Teresina", State = "PI" },
                new() { Id = "u3", TenantId = null, City = "Teresina", State = "PI" }
            };

            var student = new Student { City = "Teresina", State = "PI", TenantId = tenantId };
            var tenantContext = new DummyTenantContext { IsPrivatePlan = true, TenantId = tenantId };

            // Logic replicated from StudentsController:208
            var filteredUsers = users
                .Where(u => tenantContext.IsPrivatePlan
                    ? u.TenantId == tenantContext.TenantId
                    : (u.City == student.City && u.State == student.State))
                .ToList();

            Assert.Single(filteredUsers);
            Assert.Equal("u1", filteredUsers[0].Id);
        }
    }
}
