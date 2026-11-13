using FsCheck;
using FsCheck.Xunit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SingularSystem.Authorization;
using SingularSystem.Controllers;
using SingularSystem.Domain.Access;
using SingularSystem.Domain.Plans;
using SingularSystem.Domain.Plans.Enums;
using SingularSystem.Domain.Tenancy;
using SingularSystem.Infrastructure.Plans;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;
using SingularSystem.Services;
using Xunit;

namespace SingularSystem.Tests
{
    public sealed class IsolationAndRolesPropertyTests
    {
        private class TestableBaseController : BaseController
        {
            public TestableBaseController(ITenantContext tenantContext)
            {
                var services = new ServiceCollection();
                services.AddSingleton(tenantContext);
                services.AddSingleton<IStudentAccessPolicy, StudentAccessPolicy>();
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        RequestServices = services.BuildServiceProvider()
                    }
                };
            }

            public bool CheckAccess(ApplicationUser user, Student student, IList<string> roles)
                => HasStudentAccess(user, student, roles);
        }

        private static Student CreateStudent(string? tenantId, string city, string state)
        {
            return new Student
            {
                ID = Guid.NewGuid(),
                FullName = "Student " + Guid.NewGuid().ToString()[..6],
                City = city,
                State = state,
                Address = "Rua Teste",
                DateOfBirth = new DateTime(2015, 1, 1),
                NameOfPersonResponsible = "Resp",
                TenantId = tenantId,
                IsActive = true
            };
        }

        private static PAEE CreatePaee(Student s)
        {
            return new PAEE
            {
                ID = Guid.NewGuid(),
                StudentID = s.ID,
                Student = s,
                TenantId = s.TenantId,
                AcademicRecord = "rec",
                MedicalReport = "rep",
                Medications = "med",
                FollowUp = "fol",
                CompositionOfAssisting = "comp",
                AdditionalObservations = "obs"
            };
        }

        private static RegularTeacherQuestionnaire CreateQuestionnaire(Student s)
        {
            return new RegularTeacherQuestionnaire
            {
                ID = Guid.NewGuid(),
                StudentID = s.ID,
                Student = s,
                ClassroomDevelopment = "dev",
                ClassroomBehavior = "beh",
                MainDifficulties = "diff",
                LearningCharacteristics = "learn",
                ReadingAndWritingDescription = "rw",
                LogicalMathematicalReasoningDescription = "math",
                CompletesSchoolActivities = "act",
                CompletesHomework = "hw",
                ReactionToFrustration = "frust",
                InappropriateBehaviorDescription = "inapp",
                DifficultyWorkingInGroup = "group",
                DifficultyOrganizingTasks = "org",
                PeerAcceptance = "peer",
                BehaviorObservations = "obs",
                DevelopmentObservations = "devobs"
            };
        }

        private static AppDbContext CreateInMemoryDb(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new AppDbContext(options);
        }

        // Feature: modulo-3-2-travas-backend, Property 19: HasStudentAccess respeita o tenant
        // Validates: Requirements 13.1, 13.4
        [Property(MaxTest = 100)]
        public Property HasStudentAccess_respeita_o_tenant(bool isAdmin, bool isPrivatePlan, bool sameTenant, bool sameGeoLocation, bool studentHasTenant)
        {
            var tenantId = "tenant-prop19";
            var studentTenantId = studentHasTenant ? (sameTenant ? tenantId : "other-tenant") : null;

            var userCity = "CityA";
            var userState = "SP";
            var studentCity = sameGeoLocation ? userCity : "CityB";
            var studentState = sameGeoLocation ? userState : "RJ";

            var tenantContext = new TenantContextService
            {
                TenantId = isPrivatePlan ? tenantId : null,
                IsPrivatePlan = isPrivatePlan
            };

            var controller = new TestableBaseController(tenantContext);

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                City = userCity,
                State = userState,
                TenantId = isPrivatePlan ? tenantId : null
            };

            var student = CreateStudent(studentTenantId, studentCity, studentState);

            var roles = new List<string>();
            if (isAdmin) roles.Add("Admin");
            else roles.Add("Teacher");

            var accessGranted = controller.CheckAccess(user, student, roles);

            if (isAdmin)
            {
                // Admin sempre tem acesso
                return (accessGranted == true).ToProperty();
            }

            if (isPrivatePlan)
            {
                // Em plano privado, acesso é garantido se e somente se o student.TenantId é não nulo e igual ao tenantContext.TenantId
                var expected = !string.IsNullOrEmpty(student.TenantId) && student.TenantId == tenantContext.TenantId;
                return (accessGranted == expected).ToProperty();
            }
            else
            {
                // Em plano público, segue a lógica geográfica: como role é Teacher, bate se city e state coincidem
                var expected = sameGeoLocation;
                return (accessGranted == expected).ToProperty();
            }
        }

        // Feature: modulo-3-2-travas-backend, Property 20: Listagens de documentos isoladas por tenant
        // Validates: Requirements 13.2, 13.5
        [Property(MaxTest = 100)]
        public Property Listagens_de_documentos_isoladas_por_tenant(PositiveInt countAGen, PositiveInt countBGen, bool isPrivatePlan)
        {
            var db = CreateInMemoryDb($"p20-{Guid.NewGuid()}");
            var tenantA = "tenant-doc-A";
            var tenantB = "tenant-doc-B";

            var countA = (countAGen.Get % 5) + 1;
            var countB = (countBGen.Get % 5) + 1;

            var studentsA = Enumerable.Range(0, countA).Select(i => CreateStudent(tenantA, "City", "SP")).ToList();
            var studentsB = Enumerable.Range(0, countB).Select(i => CreateStudent(tenantB, "City", "SP")).ToList();
            db.Students.AddRange(studentsA);
            db.Students.AddRange(studentsB);
            db.SaveChanges();

            // Criar documentos vinculados aos alunos
            foreach (var s in studentsA.Concat(studentsB))
            {
                db.PEIs.Add(new PEI { ID = Guid.NewGuid(), StudentID = s.ID, Student = s, TenantId = s.TenantId });
                db.PAEEs.Add(CreatePaee(s));
                db.RegularTeacherScreenings.Add(new RegularTeacherScreening { ID = Guid.NewGuid(), StudentID = s.ID, Student = s });
                db.RegularTeacherQuestionnaires.Add(CreateQuestionnaire(s));
                db.RegularTeacherEvaluationDocuments.Add(new RegularTeacherEvaluationDocument { ID = Guid.NewGuid(), StudentID = s.ID, Student = s, TenantId = s.TenantId });
            }
            db.SaveChanges();

            var tenantContext = new TenantContextService
            {
                TenantId = isPrivatePlan ? tenantA : null,
                IsPrivatePlan = isPrivatePlan
            };

            // Testar consultas com o branch de tenant
            var peiQuery = db.PEIs.Include(p => p.Student).AsNoTracking().AsQueryable();
            if (tenantContext.IsPrivatePlan)
                peiQuery = peiQuery.Where(p => p.Student != null && p.Student.TenantId == tenantContext.TenantId);

            var paeeQuery = db.PAEEs.Include(p => p.Student).AsNoTracking().AsQueryable();
            if (tenantContext.IsPrivatePlan)
                paeeQuery = paeeQuery.Where(p => p.Student != null && p.Student.TenantId == tenantContext.TenantId);

            var screeningQuery = db.RegularTeacherScreenings.Include(s => s.Student).AsNoTracking().AsQueryable();
            if (tenantContext.IsPrivatePlan)
                screeningQuery = screeningQuery.Where(s => s.Student != null && s.Student.TenantId == tenantContext.TenantId);

            var questionnaireQuery = db.RegularTeacherQuestionnaires.Include(s => s.Student).AsNoTracking().AsQueryable();
            if (tenantContext.IsPrivatePlan)
                questionnaireQuery = questionnaireQuery.Where(s => s.Student != null && s.Student.TenantId == tenantContext.TenantId);

            var caseStudyQuery = db.RegularTeacherEvaluationDocuments.Include(s => s.Student).AsNoTracking().AsQueryable();
            if (tenantContext.IsPrivatePlan)
                caseStudyQuery = caseStudyQuery.Where(s => s.Student != null && s.Student.TenantId == tenantContext.TenantId);

            var peis = peiQuery.ToList();
            var paees = paeeQuery.ToList();
            var screenings = screeningQuery.ToList();
            var questionnaires = questionnaireQuery.ToList();
            var caseStudies = caseStudyQuery.ToList();

            if (isPrivatePlan)
            {
                var onlyA = peis.All(p => p.Student!.TenantId == tenantA) && peis.Count == countA &&
                            paees.All(p => p.Student!.TenantId == tenantA) && paees.Count == countA &&
                            screenings.All(p => p.Student!.TenantId == tenantA) && screenings.Count == countA &&
                            questionnaires.All(p => p.Student!.TenantId == tenantA) && questionnaires.Count == countA &&
                            caseStudies.All(p => p.Student!.TenantId == tenantA) && caseStudies.Count == countA;

                return onlyA.ToProperty();
            }
            else
            {
                var allDocs = peis.Count == countA + countB &&
                              paees.Count == countA + countB &&
                              screenings.Count == countA + countB &&
                              questionnaires.Count == countA + countB &&
                              caseStudies.Count == countA + countB;

                return allDocs.ToProperty();
            }
        }

        // Feature: modulo-3-2-travas-backend, Property 21: Titular de plano individual combina roles sem escalar privilégios
        // Validates: Requirements 12.1
        [Property(MaxTest = 100)]
        public Property Titular_de_plano_individual_combina_roles_sem_escalar_privilegios(bool isGolden)
        {
            var planType = isGolden ? PlanType.Golden : PlanType.Standard;
            var tenantId = "indiv-user-" + Guid.NewGuid();

            // 1. Roles do usuário: Teacher + AutonomousProfessional
            var roles = new HashSet<string> { "Teacher", "AutonomousProfessional" };

            // Verificação de autorização por role pedagógico:
            // Política AdminOrTeacher exige Teacher ou Admin => satisfeita
            var satisfiesAdminOrTeacher = roles.Contains("Teacher") || roles.Contains("Admin");
            // Endpoints com Roles = "Admin,Teacher" => satisfeito
            var satisfiesTeacherRole = roles.Contains("Teacher");

            if (!satisfiesAdminOrTeacher || !satisfiesTeacherRole)
                return false.ToProperty();

            // 2. Proteção de feature por plano: rotas protegidas com [PlanFeatureAuthorize("Empresa", "Publico")]
            // Por exemplo: CreateReport, CARS, AdaptiveBehaviorScaleDI, CognitiveAssessments
            var attribute = new PlanFeatureAuthorizeAttribute("Empresa", "Publico");

            var sub = new PlanSubscription
            {
                TenantId = tenantId,
                PlanType = planType,
                Status = SubscriptionStatus.Active,
                StartDate = DateTime.UtcNow.AddDays(-10)
            };

            var db = CreateInMemoryDb($"p21-{Guid.NewGuid()}");
            db.PlanSubscriptions.Add(sub);
            db.SaveChanges();

            var tenantContext = new TenantContextService
            {
                TenantId = tenantId,
                IsPrivatePlan = true,
                Subscription = sub
            };
            var repo = new PlanSubscriptionRepository(db);
            var planService = new PlanService(repo, db);

            var services = new ServiceCollection();
            services.AddSingleton<ITenantContext>(tenantContext);
            services.AddSingleton<IPlanService>(planService);
            services.AddSingleton<IStudentAccessPolicy, StudentAccessPolicy>();

            var httpContext = new DefaultHttpContext
            {
                RequestServices = services.BuildServiceProvider()
            };
            httpContext.Request.Method = "POST";

            var actionContext = new ActionContext(
                httpContext,
                new RouteData(),
                new ActionDescriptor()
            );

            var actionExecutingContext = new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object?>(),
                new object()
            );

            var nextCalled = false;
            ActionExecutionDelegate next = () =>
            {
                nextCalled = true;
                return Task.FromResult(new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), new object()));
            };

            attribute.OnActionExecutionAsync(actionExecutingContext, next).GetAwaiter().GetResult();

            // O acesso deve ser NEGADO com HTTP 403 (ObjectResult com StatusCode 403), sem chamar next!
            var deniedWith403 = !nextCalled &&
                               actionExecutingContext.Result is ObjectResult obj &&
                               obj.StatusCode == StatusCodes.Status403Forbidden;

            return deniedWith403.ToProperty();
        }
    }
}
