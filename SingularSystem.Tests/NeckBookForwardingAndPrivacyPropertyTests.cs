using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
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
    // Feature: neckbook-books-proxy — Properties 10, 11, 3 (Tasks 11.3, 11.4, 11.5)
    // Validates: Requirements 4.4, 4.5, 8.5, 9.3, 10.4, 11.3, 12.4, 16.1–16.9, 17.1–17.4
    public sealed class NeckBookForwardingAndPrivacyPropertyTests
    {
        public sealed record ForwardingScenario(
            HttpStatusCode StatusCode,
            string RawBody,
            string ContentType,
            string EndpointKind);

        public sealed record TransportFailureScenario(
            bool IsTimeout,
            string EndpointKind,
            string ErrorDetail);

        public sealed record PrivacyScenario(
            string ApiKey,
            string StudentFullName,
            string Cpf,
            string ParentName,
            string Address,
            string SchoolName,
            string EndpointKind,
            string OutcomeMode);

        public static class ForwardingArbContainer
        {
            private static readonly string[] EndpointKinds =
            {
                "generate",
                "get_job",
                "get_draft",
                "put_draft",
                "approve"
            };

            private static readonly (HttpStatusCode Status, string Body, string ContentType)[] SampleResponses =
            {
                (HttpStatusCode.OK, """{"id":"book-1","status":"ready"}""", "application/json"),
                (HttpStatusCode.Accepted, """{"job_id":"job-99","book_id":"book-99"}""", "application/json"),
                (HttpStatusCode.Unauthorized, """{"code":"INVALID_API_KEY","message":"Chave inválida"}""", "application/json"),
                (HttpStatusCode.Forbidden, """{"code":"MODULE_NOT_ACTIVE","message":"Módulo inativo"}""", "application/json"),
                (HttpStatusCode.Forbidden, """{"code":"MUNICIPALITY_MISMATCH","message":"Município divergente"}""", "application/json"),
                (HttpStatusCode.UnprocessableEntity, """{"code":"CONTRACT_VALIDATION_ERROR","details":{"field":"disciplina"}}""", "application/json"),
                (HttpStatusCode.TooManyRequests, """{"code":"EXCESS_MUNICIPAL_QUOTA","details":{"renewal_date":"2026-11-01T00:00:00Z"}}""", "application/json"),
                (HttpStatusCode.InternalServerError, """{"code":"INTERNAL_ERROR","message":"Erro interno"}""", "application/json"),
                (HttpStatusCode.ServiceUnavailable, """{"code":"UPSTREAM_BUSY","message":"Tente novamente"}""", "application/problem+json")
            };

            public static Arbitrary<ForwardingScenario> ForwardingArb()
            {
                var gen =
                    from sample in Gen.Elements(SampleResponses)
                    from ep in Gen.Elements(EndpointKinds)
                    from nonce in Gen.Choose(100, 99999)
                    let bodyWithNonce = sample.Body.Replace("}", $",\"nonce\":{nonce}}}")
                    select new ForwardingScenario(sample.Status, bodyWithNonce, sample.ContentType, ep);

                return Arb.From(gen);
            }

            public static Arbitrary<TransportFailureScenario> TransportArb()
            {
                var gen =
                    from isTimeout in Arb.Generate<bool>()
                    from ep in Gen.Elements(EndpointKinds)
                    from detail in Gen.Elements("DNS resolution failed", "Connection refused", "Socket hang up", "Operation timed out after 30s")
                    select new TransportFailureScenario(isTimeout, ep, detail);

                return Arb.From(gen);
            }

            public static Arbitrary<PrivacyScenario> PrivacyArb()
            {
                var gen =
                    from id in Gen.Choose(10000, 99999)
                    from ep in Gen.Elements(EndpointKinds)
                    from outcome in Gen.Elements("ok_202", "error_429", "error_500", "transport_timeout", "disabled_municipality")
                    select new PrivacyScenario(
                        $"nk_live_secret_token_{id}",
                        $"Maria Clara Exclusiva {id}",
                        $"987654{id:D5}",
                        $"Responsavel Secreto {id}",
                        $"Avenida Confidencial {id}",
                        $"Escola Protegida {id}",
                        ep,
                        outcome);

                return Arb.From(gen);
            }
        }

        // Feature: neckbook-books-proxy, Property 10: Encaminhamento fiel de status e corpo do NeckBook
        [Property(MaxTest = 100, Arbitrary = new[] { typeof(ForwardingArbContainer) })]
        public Property Forwarding_preserves_status_and_body(ForwardingScenario s)
        {
            using var db = NeckBookControllerTestInfra.CreateContext();
            var (user, student) = SeedEnabledUserAndStudent(db, "nk_live_forwarding_key");

            var expectedBytes = Encoding.UTF8.GetBytes(s.RawBody);
            var handler = new ProgrammableHttpMessageHandler(_ =>
            {
                var resp = new HttpResponseMessage(s.StatusCode)
                {
                    Content = new ByteArrayContent(expectedBytes)
                };
                resp.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(s.ContentType);
                return resp;
            });

            var controller = NeckBookControllerTestInfra.CreateBooksController(
                db, user, new[] { "Teacher" }, handler);

            var result = InvokeEndpoint(controller, s.EndpointKind, student.ID);
            var passthrough = result as NeckBookPassthroughResult;

            var statusMatches = passthrough is not null && passthrough.StatusCode == (int)s.StatusCode;
            var bodyMatches = passthrough is not null && passthrough.RawBytes.SequenceEqual(expectedBytes);
            var contentTypeMatches = passthrough is not null &&
                                     passthrough.ContentType is not null &&
                                     passthrough.ContentType.StartsWith(s.ContentType, StringComparison.OrdinalIgnoreCase);

            return (statusMatches && bodyMatches && contentTypeMatches)
                .Label($"Expected faithful status={(int)s.StatusCode}, body='{s.RawBody}', contentType='{s.ContentType}'; got status={passthrough?.StatusCode}, body='{passthrough?.Content}', contentType='{passthrough?.ContentType}'");
        }

        // Feature: neckbook-books-proxy, Property 11: Falha de transporte/timeout produz 503 padronizado
        [Property(MaxTest = 100, Arbitrary = new[] { typeof(ForwardingArbContainer) })]
        public Property Transport_failure_maps_to_503(TransportFailureScenario s)
        {
            using var db = NeckBookControllerTestInfra.CreateContext();
            var (user, student) = SeedEnabledUserAndStudent(db, "nk_live_transport_key");

            var handler = new ProgrammableHttpMessageHandler(_ =>
            {
                if (s.IsTimeout)
                {
                    throw new TaskCanceledException(s.ErrorDetail);
                }
                throw new HttpRequestException(s.ErrorDetail);
            });

            var controller = NeckBookControllerTestInfra.CreateBooksController(
                db, user, new[] { "Teacher" }, handler);

            var result = InvokeEndpoint(controller, s.EndpointKind, student.ID);
            var objResult = result as ObjectResult;
            var json = objResult?.Value is not null ? JsonSerializer.Serialize(objResult.Value) : string.Empty;

            var ok =
                objResult is not null &&
                objResult.StatusCode == StatusCodes.Status503ServiceUnavailable &&
                json.Contains(NeckBookBooksController.NeckBookUnavailableCode);

            return ok.Label($"Expected 503 NECKBOOK_UNAVAILABLE on {(s.IsTimeout ? "TaskCanceledException" : "HttpRequestException")}, got status={objResult?.StatusCode}, body={json}");
        }

        // Feature: neckbook-books-proxy, Property 3: Segredo e PII nunca vazam em resposta nem em log
        [Property(MaxTest = 100, Arbitrary = new[] { typeof(ForwardingArbContainer) })]
        public Property Secret_and_pii_never_leak(PrivacyScenario s)
        {
            using var db = NeckBookControllerTestInfra.CreateContext();

            var school = new School
            {
                ID = Guid.NewGuid(),
                Name = s.SchoolName,
                State = "Piauí",
                City = "Piripiri"
            };
            db.Schools.Add(school);

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                FullName = "Docente Avaliador",
                State = "Piauí",
                City = "Piripiri",
                SchoolID = school.ID
            };
            db.Users.Add(user);

            var student = new Student
            {
                ID = Guid.NewGuid(),
                FullName = s.StudentFullName,
                CPF = s.Cpf,
                NameOfPersonResponsible = s.ParentName,
                Address = s.Address,
                State = "Piauí",
                City = "Piripiri",
                SchoolID = school.ID,
                School = school
            };
            db.Students.Add(student);

            var encryptedKey = NeckBookControllerTestInfra.Protector.Protect(s.ApiKey);
            db.MunicipalityNeckBookConfigs.Add(new MunicipalityNeckBookConfig
            {
                Id = Guid.NewGuid(),
                State = "Piauí",
                City = "Piripiri",
                EncryptedApiKey = encryptedKey,
                IsActive = s.OutcomeMode != "disabled_municipality"
            });
            db.SaveChanges();

            var handler = new ProgrammableHttpMessageHandler(_ =>
            {
                return s.OutcomeMode switch
                {
                    "ok_202" => new HttpResponseMessage(HttpStatusCode.Accepted)
                    {
                        Content = new StringContent("""{"job_id":"job-1","book_id":"book-1"}""", Encoding.UTF8, "application/json")
                    },
                    "error_429" => new HttpResponseMessage(HttpStatusCode.TooManyRequests)
                    {
                        Content = new StringContent("""{"code":"EXCESS_MUNICIPAL_QUOTA"}""", Encoding.UTF8, "application/json")
                    },
                    "error_500" => new HttpResponseMessage(HttpStatusCode.InternalServerError)
                    {
                        Content = new StringContent("""{"code":"INTERNAL_ERROR"}""", Encoding.UTF8, "application/json")
                    },
                    "transport_timeout" => throw new TaskCanceledException("Request timed out"),
                    _ => new HttpResponseMessage(HttpStatusCode.OK)
                };
            });

            var logger = new CapturingLogger<NeckBookBooksController>();
            var controller = NeckBookControllerTestInfra.CreateBooksController(
                db, user, new[] { "Teacher" }, handler, logger);

            var result = InvokeEndpoint(controller, s.EndpointKind, student.ID);
            var responseText = ExtractResponseText(result);
            var allLogs = string.Join("\n", logger.Entries);

            var forbiddenTokens = new[]
            {
                s.ApiKey,
                encryptedKey,
                s.StudentFullName,
                s.Cpf,
                s.ParentName,
                s.Address,
                s.SchoolName
            };

            var leakedInResponse = forbiddenTokens.Any(tok =>
                responseText.Contains(tok, StringComparison.OrdinalIgnoreCase));
            var leakedInLogs = forbiddenTokens.Any(tok =>
                allLogs.Contains(tok, StringComparison.OrdinalIgnoreCase));

            return (!leakedInResponse && !leakedInLogs)
                .Label($"Secret or PII leaked! leakedInResponse={leakedInResponse}, leakedInLogs={leakedInLogs}");
        }

        private static (ApplicationUser User, Student Student) SeedEnabledUserAndStudent(AppDbContext db, string apiKey)
        {
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                FullName = "Professor",
                State = "Piauí",
                City = "Piripiri"
            };
            var student = new Student
            {
                ID = Guid.NewGuid(),
                FullName = "Aluno",
                State = "Piauí",
                City = "Piripiri",
                Address = "Rua 1",
                NameOfPersonResponsible = "Responsável"
            };
            db.Users.Add(user);
            db.Students.Add(student);
            db.MunicipalityNeckBookConfigs.Add(new MunicipalityNeckBookConfig
            {
                Id = Guid.NewGuid(),
                State = "Piauí",
                City = "Piripiri",
                EncryptedApiKey = NeckBookControllerTestInfra.Protector.Protect(apiKey),
                IsActive = true
            });
            db.SaveChanges();
            return (user, student);
        }

        private static IActionResult InvokeEndpoint(NeckBookBooksController controller, string endpointKind, Guid studentId)
        {
            using var draftDoc = JsonDocument.Parse("""{"chapters":[]}""");
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
                "put_draft" => controller.UpdateDraft("book-123", draftDoc.RootElement.Clone()).GetAwaiter().GetResult(),
                "approve" => controller.ApproveBook("book-123", draftDoc.RootElement.Clone()).GetAwaiter().GetResult(),
                _ => throw new ArgumentOutOfRangeException(nameof(endpointKind))
            };
        }

        private static string ExtractResponseText(IActionResult result) =>
            result switch
            {
                NeckBookPassthroughResult p => p.Content ?? string.Empty,
                ContentResult c => c.Content ?? string.Empty,
                ObjectResult o when o.Value is not null => JsonSerializer.Serialize(o.Value),
                _ => string.Empty
            };
    }
}
