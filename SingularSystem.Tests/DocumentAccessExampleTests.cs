using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SingularSystem.Controllers;
using SingularSystem.Domain.Access;
using SingularSystem.Domain.Tenancy;
using SingularSystem.Interfaces;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;
using SingularSystem.Services;
using SingularSystem.Tests.TestHelpers;
using Xunit;

namespace SingularSystem.Tests
{
    public sealed class DocumentAccessExampleTests
    {
        private sealed class TestCapturingLogger<T> : ILogger<T>
        {
            public List<string> Messages { get; } = new();

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

            public void Log<TState>(
                Microsoft.Extensions.Logging.LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                Messages.Add(formatter(state, exception));
            }
        }

        [Fact]
        public void PDFController_HasAuthorizeAttribute()
        {
            var authAttr = typeof(PDFController).GetCustomAttribute<AuthorizeAttribute>();
            Assert.NotNull(authAttr);
        }

        [Fact]
        public async Task PDFController_NonExistentDocument_Returns404()
        {
            using var db = PdfServiceTestFactory.CreateDbContext();
            var spyPdf = new SpyPdfService { NextResult = DocumentPdfResult.Missing() };
            var user = new ApplicationUser { Id = "test-user-1" };

            var (controller, _, _, _) = await PdfServiceTestFactory.CreateControllerAsync(
                db, spyPdf, user, new[] { "Teacher" });

            var result = await controller.ViewDocumentPdf(Guid.NewGuid());
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task PDFController_DocumentWithoutStudent_Returns403()
        {
            using var db = PdfServiceTestFactory.CreateDbContext();
            var spyPdf = new SpyPdfService { NextResult = DocumentPdfResult.Denied() };
            var user = new ApplicationUser { Id = "test-user-2" };

            var (controller, _, _, _) = await PdfServiceTestFactory.CreateControllerAsync(
                db, spyPdf, user, new[] { "Teacher" });

            var result = await controller.ViewDocumentPdf(Guid.NewGuid());
            var objResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, objResult.StatusCode);
            var body = Assert.IsType<ApiResponse<object>>(objResult.Value);
            Assert.False(body.Success);
            Assert.Equal("Você não tem permissão para acessar este documento.", body.Message);
        }

        [Fact]
        public async Task PDFController_InvalidProfile_Returns400()
        {
            using var db = PdfServiceTestFactory.CreateDbContext();
            var spyPdf = new SpyPdfService();
            var user = new ApplicationUser { Id = "test-user-3" };

            var (controller, _, _, _) = await PdfServiceTestFactory.CreateControllerAsync(
                db, spyPdf, user, new[] { "Teacher" });

            var result = await controller.ViewDocumentPdf(Guid.NewGuid(), profile: "invalid-profile-xyz");
            var badReq = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badReq.StatusCode);
        }

        [Fact]
        public async Task PDFController_PdfAndDownload_BothCallHandleDocumentAsync()
        {
            using var db = PdfServiceTestFactory.CreateDbContext();
            var spyPdf = new SpyPdfService();
            var user = new ApplicationUser { Id = "test-user-4" };

            var (controller, _, _, _) = await PdfServiceTestFactory.CreateControllerAsync(
                db, spyPdf, user, new[] { "Teacher" });

            var docId = Guid.NewGuid();

            var viewRes = await controller.ViewDocumentPdf(docId, "view");
            var fileRes1 = Assert.IsType<FileContentResult>(viewRes);
            Assert.Equal("application/pdf", fileRes1.ContentType);

            var downloadRes = await controller.DownloadDocument(docId, "print");
            var fileRes2 = Assert.IsType<FileContentResult>(downloadRes);
            Assert.Equal("application/pdf", fileRes2.ContentType);
            Assert.Equal(2, spyPdf.GeneratePdfForUserCallCount);
        }

        [Fact]
        public async Task PDFController_ForbiddenLog_ContainsOnlyDocumentIdAndUserId()
        {
            using var db = PdfServiceTestFactory.CreateDbContext();
            var spyPdf = new SpyPdfService { NextResult = DocumentPdfResult.Denied() };
            var userId = "user-audit-123";
            var docId = Guid.NewGuid();

            var user = new ApplicationUser
            {
                Id = userId,
                UserName = userId,
                Email = $"{userId}@test.com"
            };

            var logger = new TestCapturingLogger<PDFController>();
            var sp = new ServiceCollection()
                .AddLogging()
                .AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase($"audit-{Guid.NewGuid()}"))
                .AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();
            var provider = sp.Services.BuildServiceProvider();
            var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
            await userManager.CreateAsync(user);

            var controller = new PDFController(
                logger,
                db,
                new NotificationService(db, NullLogger<NotificationService>.Instance, new FakeHubContext(), userManager),
                spyPdf,
                userManager
            );

            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId) };
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth")),
                    RequestServices = provider
                }
            };

            await controller.ViewDocumentPdf(docId);

            Assert.Single(logger.Messages);
            var log = logger.Messages[0];
            Assert.Contains(docId.ToString(), log);
            Assert.Contains(userId, log);
            // Must NOT contain sensitive student info
            Assert.DoesNotContain("Student", log);
            Assert.DoesNotContain("TenantId", log);
        }
    }
}
