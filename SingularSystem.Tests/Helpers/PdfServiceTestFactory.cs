using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SingularSystem.Controllers;
using SingularSystem.Domain.Access;
using SingularSystem.Domain.Plans;
using SingularSystem.Domain.Tenancy;
using SingularSystem.DTOs.ReportAEE;
using SingularSystem.DTOs.Students;
using SingularSystem.Interfaces;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;
using SingularSystem.Services;

namespace SingularSystem.Tests.TestHelpers
{
    public class GeneratorCallTracker
    {
        public int CallCount { get; set; }
        public BaseDocument? LastDocument { get; set; }
        public PdfRenderContext? LastContext { get; set; }
    }

    public class SpyDocumentPdfGenerator : IDocumentPdfGenerator
    {
        public string Discriminator { get; }
        private readonly GeneratorCallTracker _tracker;

        public SpyDocumentPdfGenerator(string discriminator, GeneratorCallTracker tracker)
        {
            Discriminator = discriminator;
            _tracker = tracker;
        }

        public Task<byte[]> GenerateAsync(BaseDocument document, PdfRenderContext context, CancellationToken ct = default)
        {
            _tracker.CallCount++;
            _tracker.LastDocument = document;
            _tracker.LastContext = context;
            return Task.FromResult(Encoding.UTF8.GetBytes("%PDF-1.4\n1 0 obj\n<<>>\nendobj\ntrailer\n<<>>\n%%EOF"));
        }

        public Task<byte[]> GenerateAsync(BaseDocument document, CancellationToken ct = default)
            => GenerateAsync(document, PdfRenderContext.Default(), ct);
    }

    public class FakeIssuerResolver : IIssuerResolver
    {
        public int CallCount { get; private set; }
        public BaseDocument? LastDocument { get; private set; }
        public PdfProfile? LastProfile { get; private set; }

        public Task<IssuerHeaderModel> ResolveAsync(BaseDocument document, PdfProfile profile, CancellationToken ct = default)
        {
            CallCount++;
            LastDocument = document;
            LastProfile = profile;
            return Task.FromResult(new IssuerHeaderModel("Test Issuer", null, null, null, null, Array.Empty<string>()));
        }
    }

    public class FakeDocumentValidationService : IDocumentValidationService
    {
        public int GetOrIssueCallCount { get; private set; }
        public int ValidateCallCount { get; private set; }

        public Task<string> GetOrIssueAsync(Guid documentId, string documentType, CancellationToken ct = default)
        {
            GetOrIssueCallCount++;
            return Task.FromResult("fake-token-" + documentId);
        }

        public Task<DocumentValidationResult?> ValidateAsync(string token, CancellationToken ct = default)
        {
            ValidateCallCount++;
            return Task.FromResult<DocumentValidationResult?>(new DocumentValidationResult(
                true,
                "Report",
                DateTime.UtcNow,
                Array.Empty<DocumentValidationSignature>()
            ));
        }
    }

    public class DummyStudentPDFGenerator : IStudentPDFGenerator
    {
        public Task<byte[]> GenerateStudentsPDFAsync(List<StudentsPDFResponse> rows, CancellationToken ct = default)
            => Task.FromResult(Encoding.UTF8.GetBytes("%PDF-students"));
    }

    public class DummyReportAEEPDFGenerator : IReportAEEPDFGenerator
    {
        public Task<byte[]> GenerateReportsAEEPDFAsync(List<ReportAEEPDFResponse> rows, CancellationToken ct = default)
            => Task.FromResult(Encoding.UTF8.GetBytes("%PDF-report-aee"));
    }

    public class FakeHubContext : IHubContext<NotificationHub>
    {
        public IHubClients Clients { get; } = new HubClients();
        public IGroupManager Groups { get; } = new GroupManager();

        private sealed class HubClients : IHubClients
        {
            private readonly IClientProxy _p = new Proxy();
            public IClientProxy All => _p;
            public IClientProxy AllExcept(IReadOnlyList<string> e) => _p;
            public IClientProxy Client(string c) => _p;
            public IClientProxy Clients(IReadOnlyList<string> c) => _p;
            public IClientProxy Group(string g) => _p;
            public IClientProxy GroupExcept(string g, IReadOnlyList<string> e) => _p;
            public IClientProxy Groups(IReadOnlyList<string> g) => _p;
            public IClientProxy User(string u) => _p;
            public IClientProxy Users(IReadOnlyList<string> u) => _p;
        }

        private sealed class Proxy : IClientProxy
        {
            public Task SendCoreAsync(string method, object?[] args, CancellationToken ct = default) => Task.CompletedTask;
        }

        private sealed class GroupManager : IGroupManager
        {
            public Task AddToGroupAsync(string c, string g, CancellationToken ct = default) => Task.CompletedTask;
            public Task RemoveFromGroupAsync(string c, string g, CancellationToken ct = default) => Task.CompletedTask;
        }
    }

    public class SpyPdfService : IPDFService
    {
        public int GeneratePdfForUserCallCount { get; private set; }
        public Guid? LastDocumentId { get; private set; }
        public PdfProfile? LastProfile { get; private set; }
        public ApplicationUser? LastUser { get; private set; }
        public IList<string>? LastRoles { get; private set; }

        public DocumentPdfResult NextResult { get; set; } = DocumentPdfResult.Ok(
            Encoding.UTF8.GetBytes("%PDF-default"),
            "PEI"
        );

        public Task<DocumentPdfResult> GeneratePdfForUser(
            Guid documentId,
            PdfProfile profile,
            ApplicationUser user,
            IList<string> roles)
        {
            GeneratePdfForUserCallCount++;
            LastDocumentId = documentId;
            LastProfile = profile;
            LastUser = user;
            LastRoles = roles;
            return Task.FromResult(NextResult);
        }

        public Task<byte[]> GeneratePdf(Guid documentId) => Task.FromResult(Encoding.UTF8.GetBytes("%PDF-internal"));
        public Task<byte[]> GeneratePdf(Guid documentId, PdfProfile profile) => Task.FromResult(Encoding.UTF8.GetBytes("%PDF-internal-profile"));
        public Task<string?> GetDocumentType(Guid documentId) => Task.FromResult<string?>("PEI");
        public Task<byte[]> GenerateStudentsPdf(string state, string city, string schoolID, string serie = "") => Task.FromResult(Encoding.UTF8.GetBytes("%PDF-students"));
        public Task<byte[]> GenerateReportsAEEPdf(string state, string city, string schoolID, string disabilities, string disorders) => Task.FromResult(Encoding.UTF8.GetBytes("%PDF-aee"));
        public Task<byte[]> GenerateCaseStudiesTrackingPdf(string state = "", string city = "", string schoolID = "", string searchTerm = "", string status = "", int? year = null, int? month = null) => Task.FromResult(Encoding.UTF8.GetBytes("%PDF-cases"));
        public Task<byte[]> GenerateBiopsychosocialTrackingPdf(string state = "", string city = "", string schoolID = "", string searchTerm = "", string status = "", string pendingProfessional = "", int? year = null, int? month = null) => Task.FromResult(Encoding.UTF8.GetBytes("%PDF-bio"));
    }

    public static class PdfServiceTestFactory
    {
        public static AppDbContext CreateDbContext(string? dbName = null)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName ?? $"pdf-test-{Guid.NewGuid()}")
                .Options;
            return new AppDbContext(options);
        }

        public static (
            PDFService Service,
            GeneratorCallTracker Tracker,
            FakeIssuerResolver IssuerResolver,
            FakeDocumentValidationService ValidationService
        ) CreatePdfService(
            AppDbContext db,
            TenantContextService tenantContext,
            IStudentAccessPolicy? accessPolicy = null,
            IConfiguration? configuration = null)
        {
            var tracker = new GeneratorCallTracker();
            var generators = new List<IDocumentPdfGenerator>
            {
                new SpyDocumentPdfGenerator("PEI", tracker),
                new SpyDocumentPdfGenerator("PAEE", tracker),
                new SpyDocumentPdfGenerator("Report", tracker),
                new SpyDocumentPdfGenerator("RegularTeacherEvaluationDocument", tracker)
            };

            var issuerResolver = new FakeIssuerResolver();
            var validationService = new FakeDocumentValidationService();
            var config = configuration ?? new ConfigurationBuilder().Build();
            var policy = accessPolicy ?? new StudentAccessPolicy();

            var service = new PDFService(
                db,
                generators,
                new DummyStudentPDFGenerator(),
                new DummyReportAEEPDFGenerator(),
                issuerResolver,
                validationService,
                config,
                tenantContext,
                policy
            );

            return (service, tracker, issuerResolver, validationService);
        }

        public static Student CreateTestStudent(
            Guid? id = null,
            string? tenantId = null,
            string city = "Piripiri",
            string state = "PI",
            string name = "Student Test",
            Guid? schoolId = null,
            School? school = null)
        {
            return new Student
            {
                ID = id ?? Guid.NewGuid(),
                FullName = name,
                DateOfBirth = new DateTime(2010, 1, 1),
                Address = "Rua Teste, 100",
                City = city,
                State = state,
                NameOfPersonResponsible = "Responsavel",
                SchoolID = schoolId,
                School = school,
                TenantId = tenantId,
                IsActive = true
            };
        }

        public static async Task<(
            PDFController Controller,
            UserManager<ApplicationUser> UserManager,
            RoleManager<IdentityRole> RoleManager,
            ServiceProvider ServiceProvider
        )> CreateControllerAsync(
            AppDbContext db,
            IPDFService pdfService,
            ApplicationUser currentUser,
            IList<string> roles,
            ITenantContext? tenantContext = null)
        {
            var dbName = $"id-{Guid.NewGuid()}";
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(dbName));

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

            services.AddSingleton<IStudentAccessPolicy, StudentAccessPolicy>();
            if (tenantContext != null)
            {
                services.AddSingleton(tenantContext);
            }

            var sp = services.BuildServiceProvider();
            var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();

            foreach (var r in roles)
            {
                if (!await roleManager.RoleExistsAsync(r))
                {
                    await roleManager.CreateAsync(new IdentityRole(r));
                }
            }

            currentUser.UserName ??= currentUser.Id;
            currentUser.Email ??= $"{currentUser.Id}@example.com";
            var createRes = await userManager.CreateAsync(currentUser);
            if (!createRes.Succeeded)
            {
                throw new InvalidOperationException("Failed to create test user: " + string.Join(", ", createRes.Errors.Select(e => e.Description)));
            }
            if (createRes.Succeeded && roles.Count > 0)
            {
                await userManager.AddToRolesAsync(currentUser, roles);
            }

            var notificationService = new NotificationService(
                db,
                NullLogger<NotificationService>.Instance,
                new FakeHubContext(),
                userManager
            );

            var controller = new PDFController(
                NullLogger<PDFController>.Instance,
                db,
                notificationService,
                pdfService,
                userManager
            );

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, currentUser.Id)
            };
            foreach (var r in roles)
            {
                claims.Add(new(ClaimTypes.Role, r));
            }

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = principal,
                    RequestServices = sp
                }
            };

            return (controller, userManager, roleManager, sp);
        }
    }
}
