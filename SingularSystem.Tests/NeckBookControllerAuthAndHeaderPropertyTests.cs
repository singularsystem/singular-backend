using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using FsCheck;
using FsCheck.Xunit;
using SingularSystem.Controllers;
using SingularSystem.DTOs.NeckBook;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace SingularSystem.Tests
{
    // Feature: neckbook-books-proxy — Properties 12, 13, 14 (Tasks 10.2, 10.3, 10.4)
    // Validates: Requirements 3.6, 5.4, 5.5, 5.6, 7.1, 7.2, 7.3, 7.4, 8.3, 9.2, 10.2, 10.3, 11.2, 12.2
    public sealed class NeckBookControllerAuthAndHeaderPropertyTests
    {
        public sealed record StudentAccessScenario(
            string Role,
            string UserState,
            string UserCity,
            Guid? UserSchoolId,
            string StudentState,
            string StudentCity,
            Guid? StudentSchoolId,
            string? SchoolState,
            string? SchoolCity);

        public sealed record DisabledMunicipalityScenario(
            string? UserState,
            string? UserCity,
            bool HasConfigRow,
            bool ConfigIsActive,
            string EndpointKind);

        public sealed record HeaderScenario(
            string ApiKey,
            string EndpointKind,
            string Role);

        private sealed class BaseControllerProbe : BaseController
        {
            public bool Evaluate(ApplicationUser user, Student student, IList<string> roles) =>
                HasStudentAccess(user, student, roles);
        }

        public static class ControllerArbContainer
        {
            private static readonly (string State, string City)[] Places =
            {
                ("Piauí", "Piripiri"),
                ("PI", "Piripiri"),
                ("Piauí", "Teresina"),
                ("Ceará", "Fortaleza"),
                ("São Paulo", "Campinas")
            };

            private static readonly string[] EndpointKinds =
            {
                "generate",
                "get_job",
                "get_draft",
                "put_draft",
                "approve"
            };

            public static Arbitrary<StudentAccessScenario> AccessScenario()
            {
                var schoolA = Guid.Parse("11111111-1111-1111-1111-111111111111");
                var schoolB = Guid.Parse("22222222-2222-2222-2222-222222222222");

                var gen =
                    from role in Gen.Elements("Teacher", "Admin")
                    from uPlace in Gen.Elements(Places)
                    from sPlace in Gen.Elements(Places)
                    from uSchool in Gen.Elements<Guid?>(null, schoolA, schoolB)
                    from sSchool in Gen.Elements<Guid?>(null, schoolA, schoolB)
                    from schPlace in Gen.Elements(Places)
                    select new StudentAccessScenario(
                        role,
                        uPlace.State,
                        uPlace.City,
                        uSchool,
                        sPlace.State,
                        sPlace.City,
                        sSchool,
                        sSchool.HasValue ? schPlace.State : null,
                        sSchool.HasValue ? schPlace.City : null);

                return Arb.From(gen);
            }

            public static Arbitrary<DisabledMunicipalityScenario> DisabledScenario()
            {
                var gen =
                    from mode in Gen.Elements("missing_state", "missing_city", "no_config", "inactive_config")
                    from endpoint in Gen.Elements(EndpointKinds)
                    select mode switch
                    {
                        "missing_state" => new DisabledMunicipalityScenario("", "Piripiri", true, true, endpoint),
                        "missing_city" => new DisabledMunicipalityScenario("Piauí", "   ", true, true, endpoint),
                        "no_config" => new DisabledMunicipalityScenario("Ceará", "Sobral", false, false, endpoint),
                        _ => new DisabledMunicipalityScenario("Piauí", "Piripiri", true, false, endpoint)
                    };

                return Arb.From(gen);
            }

            public static Arbitrary<HeaderScenario> HeaderArb()
            {
                var gen =
                    from suffix in Gen.Choose(10000, 999999)
                    from ep in Gen.Elements(EndpointKinds)
                    from role in Gen.Elements("Teacher", "Admin")
                    select new HeaderScenario($"nk_live_secret_{suffix}", ep, role);

                return Arb.From(gen);
            }
        }

        // Feature: neckbook-books-proxy, Property 12: Autorização por aluno reproduz HasStudentAccess
        [Property(MaxTest = 100, Arbitrary = new[] { typeof(ControllerArbContainer) })]
        public Property Student_access_matches_base_controller(StudentAccessScenario s)
        {
            using var db = NeckBookControllerTestInfra.CreateContext();

            School? studentSchool = null;
            if (s.StudentSchoolId.HasValue)
            {
                studentSchool = new School
                {
                    ID = s.StudentSchoolId.Value,
                    Name = "Escola Aluno",
                    State = s.SchoolState ?? s.StudentState,
                    City = s.SchoolCity ?? s.StudentCity
                };
                db.Schools.Add(studentSchool);
            }

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                FullName = "Docente Teste",
                State = s.UserState,
                City = s.UserCity,
                SchoolID = s.UserSchoolId
            };
            db.Users.Add(user);

            var student = new Student
            {
                ID = Guid.NewGuid(),
                FullName = "Estudante Teste",
                State = s.StudentState,
                City = s.StudentCity,
                SchoolID = s.StudentSchoolId,
                School = studentSchool,
                Address = "Rua 1",
                NameOfPersonResponsible = "Mãe"
            };
            db.Students.Add(student);

            db.MunicipalityNeckBookConfigs.Add(new MunicipalityNeckBookConfig
            {
                Id = Guid.NewGuid(),
                State = s.UserState,
                City = s.UserCity,
                EncryptedApiKey = NeckBookControllerTestInfra.Protector.Protect("nk_live_key_p12"),
                IsActive = true
            });
            db.SaveChanges();

            var roles = new List<string> { s.Role };
            var probe = new BaseControllerProbe();
            var expectedAccess = probe.Evaluate(user, student, roles);

            var handler = ProgrammableHttpMessageHandler.Json(
                HttpStatusCode.Accepted,
                """{"job_id":"job-1","book_id":"book-1"}""");

            var controller = NeckBookControllerTestInfra.CreateBooksController(db, user, roles, handler);
            var form = new BookForm
            {
                StudentId = student.ID,
                Disciplina = Subject.Portuguese.ToString(),
                ExerciciosPorCapitulo = 2
            };

            var actionResult = controller.GenerateBook(form).GetAwaiter().GetResult();

            if (expectedAccess)
            {
                var passthrough = actionResult as NeckBookPassthroughResult;
                return (passthrough is not null &&
                        passthrough.StatusCode == StatusCodes.Status202Accepted &&
                        handler.CallCount == 1)
                    .Label($"Expected 202 and 1 NeckBook call when HasStudentAccess=true, got {actionResult?.GetType().Name} / calls={handler.CallCount}");
            }
            else
            {
                var obj = actionResult as ObjectResult;
                return (obj is not null &&
                        obj.StatusCode == StatusCodes.Status403Forbidden &&
                        handler.CallCount == 0)
                    .Label($"Expected 403 and 0 NeckBook calls when HasStudentAccess=false, got status={obj?.StatusCode} / calls={handler.CallCount}");
            }
        }

        // Feature: neckbook-books-proxy, Property 13: Município não habilitado bloqueia antes do NeckBook
        [Property(MaxTest = 100, Arbitrary = new[] { typeof(ControllerArbContainer) })]
        public Property Disabled_municipality_blocks_before_neckbook(DisabledMunicipalityScenario s)
        {
            using var db = NeckBookControllerTestInfra.CreateContext();

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                FullName = "Professor",
                State = s.UserState,
                City = s.UserCity ?? string.Empty
            };
            db.Users.Add(user);

            if (s.HasConfigRow)
            {
                db.MunicipalityNeckBookConfigs.Add(new MunicipalityNeckBookConfig
                {
                    Id = Guid.NewGuid(),
                    State = "Piauí",
                    City = "Piripiri",
                    EncryptedApiKey = NeckBookControllerTestInfra.Protector.Protect("nk_live_inactive"),
                    IsActive = s.ConfigIsActive
                });
            }
            db.SaveChanges();

            var handler = ProgrammableHttpMessageHandler.Json(HttpStatusCode.OK, """{"ok":true}""");
            var controller = NeckBookControllerTestInfra.CreateBooksController(
                db, user, new[] { "Teacher" }, handler);

            var result = InvokeEndpoint(controller, s.EndpointKind, Guid.NewGuid());
            var objResult = result as ObjectResult;
            var json = objResult?.Value is not null ? JsonSerializer.Serialize(objResult.Value) : "{}";
            using var doc = JsonDocument.Parse(json);
            var code = doc.RootElement.TryGetProperty("code", out var cProp) ? cProp.GetString() : null;
            var message = doc.RootElement.TryGetProperty("message", out var mProp) ? mProp.GetString() : null;

            var ok =
                objResult is not null &&
                objResult.StatusCode == StatusCodes.Status403Forbidden &&
                handler.CallCount == 0 &&
                code == NeckBookBooksController.MunicipalityNotEnabledCode &&
                message == NeckBookBooksController.MunicipalityNotEnabledMessage;

            return ok.Label($"Expected 403 MUNICIPALITY_NOT_ENABLED and 0 HTTP calls for {s.EndpointKind}, got status={objResult?.StatusCode}, calls={handler.CallCount}, body={json}");
        }

        // Feature: neckbook-books-proxy, Property 14: Header Authorization por requisição
        [Property(MaxTest = 100, Arbitrary = new[] { typeof(ControllerArbContainer) })]
        public Property Authorization_header_per_request(HeaderScenario s)
        {
            using var db = NeckBookControllerTestInfra.CreateContext();

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                FullName = "Professor",
                State = "Piauí",
                City = "Piripiri"
            };
            db.Users.Add(user);

            var student = new Student
            {
                ID = Guid.NewGuid(),
                FullName = "Aluno",
                State = "Piauí",
                City = "Piripiri",
                Address = "Rua 1",
                NameOfPersonResponsible = "Mãe"
            };
            db.Students.Add(student);

            db.MunicipalityNeckBookConfigs.Add(new MunicipalityNeckBookConfig
            {
                Id = Guid.NewGuid(),
                State = "PI",
                City = "Piripiri",
                EncryptedApiKey = NeckBookControllerTestInfra.Protector.Protect(s.ApiKey),
                IsActive = true
            });
            db.SaveChanges();

            string? capturedAuthHeader = null;
            string? capturedBody = null;
            var handler = new ProgrammableHttpMessageHandler(req =>
            {
                if (req.Headers.TryGetValues("Authorization", out var values))
                {
                    capturedAuthHeader = values.SingleOrDefault();
                }
                if (req.Content is not null)
                {
                    capturedBody = req.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                }
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""{"status":"ok"}""", Encoding.UTF8, "application/json")
                };
            });

            var controller = NeckBookControllerTestInfra.CreateBooksController(
                db, user, new[] { s.Role }, handler);

            _ = InvokeEndpoint(controller, s.EndpointKind, student.ID);

            var headerOk = capturedAuthHeader == $"ApiKey {s.ApiKey}";

            // Se for approve, também verifica injeção de reviewer_id e reviewer_role (Req 12.3)
            var approveOk = true;
            if (s.EndpointKind == "approve" && capturedBody is not null)
            {
                using var doc = JsonDocument.Parse(capturedBody);
                approveOk =
                    doc.RootElement.GetProperty("reviewer_id").GetString() == user.Id &&
                    doc.RootElement.GetProperty("reviewer_role").GetString() == s.Role;
            }

            return (headerOk && approveOk && handler.CallCount == 1)
                .Label($"Expected Authorization='ApiKey {s.ApiKey}', got '{capturedAuthHeader}' on {s.EndpointKind}");
        }

        private static IActionResult InvokeEndpoint(NeckBookBooksController controller, string endpointKind, Guid studentId)
        {
            using var emptyJsonDoc = JsonDocument.Parse("""{"title":"Rascunho"}""");
            return endpointKind switch
            {
                "generate" => controller.GenerateBook(new BookForm
                {
                    StudentId = studentId,
                    Disciplina = Subject.Portuguese.ToString(),
                    ExerciciosPorCapitulo = 2
                }).GetAwaiter().GetResult(),
                "get_job" => controller.GetJobStatus("job-123").GetAwaiter().GetResult(),
                "get_draft" => controller.GetDraft("book-123").GetAwaiter().GetResult(),
                "put_draft" => controller.UpdateDraft("book-123", emptyJsonDoc.RootElement.Clone()).GetAwaiter().GetResult(),
                "approve" => controller.ApproveBook("book-123", emptyJsonDoc.RootElement.Clone()).GetAwaiter().GetResult(),
                _ => throw new ArgumentOutOfRangeException(nameof(endpointKind))
            };
        }
    }
}
