using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using FsCheck;
using FsCheck.Xunit;
using SingularSystem.DTOs.NeckBook;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;
using SingularSystem.Services;
using Xunit;

namespace SingularSystem.Tests
{
    // Feature: neckbook-books-proxy — Properties 8 & 9 (Tasks 8.3 & 8.4)
    // Validates: Requirements 15.1, 15.2, 15.3, 15.4
    public sealed class NeckBookReportsAndSerializationPropertyTests
    {
        public sealed record ReportsScenario(
            string? PsyConclusion,
            SnapIVResult SnapResult,
            ScaleDIResult ScaleDiResult,
            string? PpConclusion,
            CARSResult CarsResult,
            CognitiveResult CognitiveResult,
            string? SwContext,
            string? SwConclusion,
            string? RtDevelopment,
            string? RtBehavior,
            string? RtDifficulties,
            string? PeiAcademic,
            string? PeiMedical,
            string? PaeeAcademic,
            string? PaeeFollowUp);

        public static class ReportsArbContainer
        {
            private static Gen<string?> OptText(string sample) =>
                Gen.Elements<string?>(null, "", "   ", sample, $"  {sample}  ");

            public static Arbitrary<ReportsScenario> Scenario()
            {
                var gen =
                    from psyConc in OptText("Evolução clínica positiva")
                    from snap in Gen.Elements(Enum.GetValues<SnapIVResult>())
                    from scale in Gen.Elements(Enum.GetValues<ScaleDIResult>())
                    from ppConc in OptText("Necessita apoio pedagógico")
                    from cars in Gen.Elements(Enum.GetValues<CARSResult>())
                    from cog in Gen.Elements(Enum.GetValues<CognitiveResult>())
                    from swCtx in OptText("Família participativa")
                    from swConc in OptText("Acompanhamento regular")
                    from rtDev in OptText("Bom desenvolvimento")
                    from rtBeh in OptText("Participativo")
                    from rtDiff in OptText("Leitura inicial")
                    from peiAc in OptText("Matriculado no ensino regular")
                    from peiMed in OptText("CID F84.0")
                    from paeeAc in OptText("Atendimento no contraturno")
                    from paeeFol in OptText("Fonoaudiologia semanal")
                    select new ReportsScenario(
                        psyConc, snap, scale,
                        ppConc, cars, cog,
                        swCtx, swConc,
                        rtDev, rtBeh, rtDiff,
                        peiAc, peiMed,
                        paeeAc, paeeFol);

                return Arb.From(gen);
            }
        }

        private static ReportsBundle BuildBundle(ReportsScenario s) =>
            new()
            {
                PsychologistReport = new PsychologistReport
                {
                    Conclusion = s.PsyConclusion,
                    SnapIVQuestionnaire = new SnapIVQuestionnaire { Result = s.SnapResult },
                    AdaptiveBehaviorScaleDI = new AdaptiveBehaviorScaleDI { Result = s.ScaleDiResult }
                },
                PsychopedagogueReport = new PsychopedagogueReport
                {
                    Conclusion = s.PpConclusion,
                    CARSQuestionnaire = new CARSQuestionnaire { Result = s.CarsResult },
                    CognitiveAssessment = new CognitiveAssessment { Result = s.CognitiveResult }
                },
                SocialWorkerReport = new SocialWorkerReport
                {
                    SocioeconomicContext = s.SwContext,
                    Conclusion = s.SwConclusion
                },
                RegularTeacherQuestionnaire = new RegularTeacherQuestionnaire
                {
                    ClassroomDevelopment = s.RtDevelopment ?? string.Empty,
                    ClassroomBehavior = s.RtBehavior ?? string.Empty,
                    MainDifficulties = s.RtDifficulties ?? string.Empty
                },
                PEI = new PEI
                {
                    AcademicRecord = s.PeiAcademic ?? string.Empty,
                    MedicalReport = s.PeiMedical ?? string.Empty
                },
                PAEE = new PAEE
                {
                    AcademicRecord = s.PaeeAcademic ?? string.Empty,
                    FollowUp = s.PaeeFollowUp ?? string.Empty
                }
            };

        // Feature: neckbook-books-proxy, Property 8: Resumo de reports é determinístico e aditivo
        [Property(MaxTest = 100, Arbitrary = new[] { typeof(ReportsArbContainer) })]
        public Property Reports_summary_is_deterministic(ReportsScenario scenario)
        {
            var bundle1 = BuildBundle(scenario);
            var bundle2 = BuildBundle(scenario);

            var r1 = NeckBookPayloadAssembler.AssembleReports(bundle1);
            var r2 = NeckBookPayloadAssembler.AssembleReports(bundle2);

            var deterministic =
                r1.Psicologo == r2.Psicologo &&
                r1.Psicopedagogo == r2.Psicopedagogo &&
                r1.AssistenteSocial == r2.AssistenteSocial &&
                r1.ProfessorRegular == r2.ProfessorRegular &&
                r1.PeiResumo == r2.PeiResumo &&
                r1.PaeeResumo == r2.PaeeResumo;

            static bool HasText(string? v) => !string.IsNullOrWhiteSpace(v);

            bool psyExpected = HasText(scenario.PsyConclusion) ||
                               scenario.SnapResult != SnapIVResult.None ||
                               scenario.ScaleDiResult != ScaleDIResult.None;

            bool ppExpected = HasText(scenario.PpConclusion) ||
                              scenario.CarsResult != CARSResult.None ||
                              scenario.CognitiveResult != CognitiveResult.None;

            bool swExpected = HasText(scenario.SwContext) || HasText(scenario.SwConclusion);
            bool rtExpected = HasText(scenario.RtDevelopment) || HasText(scenario.RtBehavior) || HasText(scenario.RtDifficulties);
            bool peiExpected = HasText(scenario.PeiAcademic) || HasText(scenario.PeiMedical);
            bool paeeExpected = HasText(scenario.PaeeAcademic) || HasText(scenario.PaeeFollowUp);

            var additive =
                (r1.Psicologo is not null) == psyExpected &&
                (r1.Psicopedagogo is not null) == ppExpected &&
                (r1.AssistenteSocial is not null) == swExpected &&
                (r1.ProfessorRegular is not null) == rtExpected &&
                (r1.PeiResumo is not null) == peiExpected &&
                (r1.PaeeResumo is not null) == paeeExpected;

            return (deterministic && additive)
                .Label("Reports summary must be deterministic and omit keys whose source fields are all empty");
        }

        // Feature: neckbook-books-proxy, Property 9: Serialização em camelCase com enums como string
        [Property(MaxTest = 100, Arbitrary = new[] { typeof(ReportsArbContainer) })]
        public Property Payload_is_camelcase_enums_as_string(ReportsScenario scenario)
        {
            var assembler = new NeckBookPayloadAssembler();
            var student = new Student
            {
                ID = Guid.NewGuid(),
                FullName = "Aluno",
                Serie = Serie.SecondYear
            };
            var bundle = BuildBundle(scenario);
            var form = new BookForm
            {
                StudentId = student.ID,
                Disciplina = Subject.Science.ToString(),
                ExerciciosPorCapitulo = 2,
                NumCapitulos = 4,
                SuporteCaa = true,
                EstiloVisual = "ilustrado",
                Interesses = new[] { "natureza" }
            };

            var payload = assembler.Assemble(form, student, bundle);
            var json = NeckBookPayloadAssembler.Serialize(payload);

            using var doc = JsonDocument.Parse(json);
            var allKeysStartLower = AllPropertyNamesStartWithLowercase(doc.RootElement);

            // Também verifica que qualquer enum serializado com JsonOptions sai como String e nunca como Number
            var subjectJson = JsonSerializer.Serialize(Subject.Portuguese, NeckBookPayloadAssembler.JsonOptions);
            using var enumDoc = JsonDocument.Parse(subjectJson);
            var enumIsString = enumDoc.RootElement.ValueKind == JsonValueKind.String;

            return (allKeysStartLower && enumIsString)
                .Label($"Expected lowercase-initial JSON keys and string enums; JSON={json}");
        }

        private static bool AllPropertyNamesStartWithLowercase(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var prop in element.EnumerateObject())
                    {
                        if (string.IsNullOrEmpty(prop.Name) || char.IsUpper(prop.Name[0]))
                        {
                            return false;
                        }
                        if (!AllPropertyNamesStartWithLowercase(prop.Value))
                        {
                            return false;
                        }
                    }
                    return true;

                case JsonValueKind.Array:
                    foreach (var item in element.EnumerateArray())
                    {
                        if (!AllPropertyNamesStartWithLowercase(item))
                        {
                            return false;
                        }
                    }
                    return true;

                default:
                    return true;
            }
        }
    }
}
