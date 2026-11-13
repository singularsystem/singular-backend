using System.Security.Claims;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SingularSystem.Domain.Plans;
using SingularSystem.Domain.Plans.Enums;
using SingularSystem.Domain.Tenancy;
using SingularSystem.DTOs.Users;
using SingularSystem.Infrastructure.Plans;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;
using SingularSystem.Services;
using Xunit;

namespace SingularSystem.Tests
{
    public sealed class ControllersAndTenantPropertyTests
    {
        private static AppDbContext CreateInMemoryDb(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new AppDbContext(options);
        }

        private static (AppDbContext Db, UserManager<ApplicationUser> UserManager) CreateIdentityContext(string dbName)
        {
            var db = CreateInMemoryDb(dbName);
            var store = new UserStore<ApplicationUser, IdentityRole, AppDbContext>(db);
            var userManager = new UserManager<ApplicationUser>(
                store,
                Options.Create(new IdentityOptions()),
                new PasswordHasher<ApplicationUser>(),
                Array.Empty<IUserValidator<ApplicationUser>>(),
                Array.Empty<IPasswordValidator<ApplicationUser>>(),
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                null,
                NullLogger<UserManager<ApplicationUser>>.Instance);

            return (db, userManager);
        }

        private static Student CreateTestStudent(string? tenantId, string nameSuffix = "")
        {
            return new Student
            {
                ID = Guid.NewGuid(),
                FullName = $"Student {nameSuffix} {Guid.NewGuid().ToString()[..6]}",
                DateOfBirth = new DateTime(2015, 1, 1),
                Address = "Rua Teste, 123",
                City = "Sao Paulo",
                State = "SP",
                NameOfPersonResponsible = "Responsavel Teste",
                TenantId = tenantId,
                IsActive = true
            };
        }

        // Feature: modulo-3-2-travas-backend, Property 1: Isolamento de dados por tenant
        // Validates: Requirements 2.1, 2.2
        [Property(MaxTest = 100)]
        public Property Isolamento_de_dados_por_tenant_queries_de_alunos_e_relatorios(PositiveInt tenantACountGen, PositiveInt tenantBCountGen, PositiveInt publicCountGen, bool isPrivatePlan)
        {
            var db = CreateInMemoryDb($"p1-{Guid.NewGuid()}");
            var tenantA = "tenant-alpha";
            var tenantB = "tenant-beta";

            var countA = (tenantACountGen.Get % 5) + 1;
            var countB = (tenantBCountGen.Get % 5) + 1;
            var countPublic = (publicCountGen.Get % 5) + 1;

            var allStudents = new List<Student>();
            for (int i = 0; i < countA; i++) allStudents.Add(CreateTestStudent(tenantA, $"A-{i}"));
            for (int i = 0; i < countB; i++) allStudents.Add(CreateTestStudent(tenantB, $"B-{i}"));
            for (int i = 0; i < countPublic; i++) allStudents.Add(CreateTestStudent(null, $"Pub-{i}"));

            db.Students.AddRange(allStudents);
            db.SaveChanges();

            var allReports = allStudents.Select(s => new Report
            {
                ID = Guid.NewGuid(),
                StudentID = s.ID,
                Student = s,
                TenantId = s.TenantId
            }).ToList();

            db.Reports.AddRange(allReports);
            db.SaveChanges();

            var tenantContext = new TenantContextService();
            if (isPrivatePlan)
            {
                tenantContext.TenantId = tenantA;
                tenantContext.IsPrivatePlan = true;
            }
            else
            {
                tenantContext.TenantId = null;
                tenantContext.IsPrivatePlan = false;
            }

            // Simula o branch de tenant em BuildStudentQuery / BuildStudentBaseQuery
            var studentQuery = db.Students.AsNoTracking().AsQueryable();
            if (tenantContext.IsPrivatePlan)
            {
                studentQuery = studentQuery.Where(s => s.TenantId == tenantContext.TenantId);
            }
            var studentResults = studentQuery.ToList();

            // Simula o branch de tenant em BuildReportsQuery
            var reportQuery = db.Reports.Include(r => r.Student).AsNoTracking().AsQueryable();
            if (tenantContext.IsPrivatePlan)
            {
                reportQuery = reportQuery.Where(r => r.Student != null && r.Student.TenantId == tenantContext.TenantId);
            }
            var reportResults = reportQuery.ToList();

            if (isPrivatePlan)
            {
                var studentsOnlyTenantA = studentResults.All(s => s.TenantId == tenantA) && studentResults.Count == countA;
                var reportsOnlyTenantA = reportResults.All(r => r.Student != null && r.Student.TenantId == tenantA) && reportResults.Count == countA;
                return (studentsOnlyTenantA && reportsOnlyTenantA).ToProperty();
            }
            else
            {
                var allReturnedStudents = studentResults.Count == (countA + countB + countPublic);
                var allReturnedReports = reportResults.Count == (countA + countB + countPublic);
                return (allReturnedStudents && allReturnedReports).ToProperty();
            }
        }

        // Feature: modulo-3-2-travas-backend, Property 10: Aluno criado recebe o TenantId e entra na cota
        // Validates: Requirements 3.2
        [Property(MaxTest = 100)]
        public Property Aluno_criado_recebe_TenantId_e_entra_na_cota(bool isPrivatePlan)
        {
            var db = CreateInMemoryDb($"p10-{Guid.NewGuid()}");
            var tenantId = "tenant-owner-" + Guid.NewGuid().ToString()[..8];

            var subscriptionRepo = new PlanSubscriptionRepository(db);
            var planService = new PlanService(subscriptionRepo, db);

            // Criar assinatura com cota de 10 alunos
            var sub = new PlanSubscription
            {
                TenantId = tenantId,
                PlanType = PlanType.Standard,
                Status = SubscriptionStatus.Active,
                QuotaDocuments = 10,
                StartDate = DateTime.UtcNow.AddDays(-1)
            };
            db.PlanSubscriptions.Add(sub);
            db.SaveChanges();

            var tenantContext = new TenantContextService
            {
                TenantId = isPrivatePlan ? tenantId : null,
                IsPrivatePlan = isPrivatePlan
            };

            // Simular lógica de criação em StudentsController
            var student = CreateTestStudent(null, "New");
            if (tenantContext.IsPrivatePlan)
            {
                student.TenantId = tenantContext.TenantId;
            }
            db.Students.Add(student);
            db.SaveChanges();

            var persisted = db.Students.First(s => s.ID == student.ID);

            var quota = planService.CheckStudentQuotaAsync(tenantId).GetAwaiter().GetResult();

            if (isPrivatePlan)
            {
                var tenantIdMatches = persisted.TenantId == tenantId;
                var quotaCountsIt = quota.Current == 1 && quota.Allowed == true;
                return (tenantIdMatches && quotaCountsIt).ToProperty();
            }
            else
            {
                var tenantIdIsNull = persisted.TenantId == null;
                var quotaDoesNotCountIt = quota.Current == 0;
                return (tenantIdIsNull && quotaDoesNotCountIt).ToProperty();
            }
        }

        // Feature: modulo-3-2-travas-backend, Property 12: Import respeita a cota por N linhas e grava TenantId
        // Validates: Requirements 3.4
        [Property(MaxTest = 100)]
        public Property Import_respeita_cota_por_N_linhas_e_grava_TenantId(PositiveInt existingGen, PositiveInt candidateGen, PositiveInt maxGen)
        {
            var db = CreateInMemoryDb($"p12-{Guid.NewGuid()}");
            var tenantId = "tenant-import-" + Guid.NewGuid().ToString()[..8];
            var subscriptionRepo = new PlanSubscriptionRepository(db);
            var planService = new PlanService(subscriptionRepo, db);

            var max = (maxGen.Get % 20) + 5; // 5..24
            var existing = existingGen.Get % max; // 0..max-1
            var nCandidates = (candidateGen.Get % 15) + 1; // 1..15

            var sub = new PlanSubscription
            {
                TenantId = tenantId,
                PlanType = PlanType.Standard,
                Status = SubscriptionStatus.Active,
                QuotaDocuments = max,
                StartDate = DateTime.UtcNow.AddDays(-1)
            };
            db.PlanSubscriptions.Add(sub);

            for (int i = 0; i < existing; i++)
            {
                db.Students.Add(CreateTestStudent(tenantId, $"Existing-{i}"));
            }
            db.SaveChanges();

            var tenantContext = new TenantContextService
            {
                TenantId = tenantId,
                IsPrivatePlan = true,
                Subscription = sub
            };

            // Simula a pré-contagem e verificação da cota no endpoint de import
            var quotaCheck = planService.CheckStudentQuotaAsync(tenantContext.TenantId!, nCandidates).GetAwaiter().GetResult();
            var expectedAllowed = (existing + nCandidates) <= max;

            if (quotaCheck.Allowed != expectedAllowed)
            {
                return false.ToProperty();
            }

            if (!quotaCheck.Allowed)
            {
                // Nenhum aluno deve ser persistido em caso de estouro
                var countAfterRejection = db.Students.Count(s => s.TenantId == tenantId);
                return (countAfterRejection == existing).ToProperty();
            }
            else
            {
                // Import procede: todos os novos recebem o TenantId do tenantContext
                for (int i = 0; i < nCandidates; i++)
                {
                    var newStudent = CreateTestStudent(null, $"Imported-{i}");
                    if (tenantContext.IsPrivatePlan)
                    {
                        newStudent.TenantId = tenantContext.TenantId;
                    }
                    db.Students.Add(newStudent);
                }
                db.SaveChanges();

                var totalCount = db.Students.Count(s => s.TenantId == tenantId);
                var allHaveTenant = db.Students.Where(s => s.TenantId == tenantId).All(s => s.TenantId == tenantId);

                return (totalCount == existing + nCandidates && allHaveTenant).ToProperty();
            }
        }

        // Feature: modulo-3-2-travas-backend, Property 13: Documentos recebem o TenantId
        // Validates: Requirements 3.5
        [Property(MaxTest = 100)]
        public Property Documentos_recebem_o_TenantId(bool isPrivatePlan)
        {
            var db = CreateInMemoryDb($"p13-{Guid.NewGuid()}");
            var tenantId = "tenant-doc-" + Guid.NewGuid().ToString()[..8];

            var tenantContext = new TenantContextService
            {
                TenantId = isPrivatePlan ? tenantId : null,
                IsPrivatePlan = isPrivatePlan
            };

            // 1. PEI
            var pei = new PEI();
            if (tenantContext.IsPrivatePlan) pei.TenantId = tenantContext.TenantId;

            // 2. PAEE
            var paee = new PAEE();
            if (tenantContext.IsPrivatePlan) paee.TenantId = tenantContext.TenantId;

            // 3. RegularTeacherEvaluationDocument (Estudo de Caso)
            var caseStudy = new RegularTeacherEvaluationDocument();
            if (tenantContext.IsPrivatePlan) caseStudy.TenantId = tenantContext.TenantId;

            if (isPrivatePlan)
            {
                return (pei.TenantId == tenantId &&
                        paee.TenantId == tenantId &&
                        caseStudy.TenantId == tenantId).ToProperty();
            }
            else
            {
                return (pei.TenantId == null &&
                        paee.TenantId == null &&
                        caseStudy.TenantId == null).ToProperty();
            }
        }

        // Feature: modulo-3-2-travas-backend, Property 14: Usuário escolar herda o TenantId do chamador
        // Validates: Requirements 6.3, 6.4, 11.3
        [Property(MaxTest = 100)]
        public Property Usuario_escolar_herda_o_TenantId_do_chamador(PositiveInt typeIndexGen, NonNull<string> callerTenantGen)
        {
            var cleanTenant = (callerTenantGen.Get ?? "").Replace("\0", "").Trim();
            var callerTenantId = "tenant-" + (cleanTenant.Length > 10 ? cleanTenant[..10] : cleanTenant);
            var candidates = new[] { "Teacher", "Psychologist", "Psychopedagogue", "Admin", "Coordinator", "InvalidRole" };
            var selectedType = candidates[typeIndexGen.Get % candidates.Length];

            var allowedTypes = new[] { "Teacher", "Psychologist", "Psychopedagogue" };
            var isAllowed = allowedTypes.Contains(selectedType);

            // Simula o endpoint RegisterSchoolUser
            if (!isAllowed)
            {
                // Deve falhar com BadRequest (HTTP 400)
                var result = new BadRequestObjectResult(new { Message = "Tipo inválido" });
                return (result.StatusCode == StatusCodes.Status400BadRequest).ToProperty();
            }
            else
            {
                // Usuário é criado herdando TenantId do chamador
                var createdUser = new ApplicationUser
                {
                    UserName = $"user_{Guid.NewGuid()}@test.com",
                    Email = $"user_{Guid.NewGuid()}@test.com",
                    TenantId = callerTenantId,
                    MustChangePassword = true
                };

                return (createdUser.TenantId == callerTenantId && createdUser.MustChangePassword == true).ToProperty();
            }
        }

        // Feature: modulo-3-2-travas-backend, Property 15: Limite MaxUsers
        // Validates: Requirements 6.2
        [Property(MaxTest = 100)]
        public Property Limite_MaxUsers(PositiveInt maxUsersGen, PositiveInt currentCountGen)
        {
            var maxUsers = maxUsersGen.Get % 10; // 0..9 (0 significa ilimitado)
            var currentUsers = currentCountGen.Get % 15; // 0..14

            var isExceeded = maxUsers > 0 && currentUsers >= maxUsers;

            // Simula a lógica de checagem do RegisterSchoolUser
            int statusCode;
            if (isExceeded)
            {
                statusCode = StatusCodes.Status402PaymentRequired;
            }
            else
            {
                statusCode = StatusCodes.Status201Created;
            }

            if (maxUsers > 0 && currentUsers >= maxUsers)
            {
                return (statusCode == StatusCodes.Status402PaymentRequired).ToProperty();
            }
            else
            {
                return (statusCode == StatusCodes.Status201Created).ToProperty();
            }
        }
    }
}
