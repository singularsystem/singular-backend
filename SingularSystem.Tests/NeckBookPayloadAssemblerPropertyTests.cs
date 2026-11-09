using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using FsCheck;
using FsCheck.Xunit;
using SingularSystem.DTOs.NeckBook;
using SingularSystem.Extensions;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;
using SingularSystem.Services;
using Xunit;

namespace SingularSystem.Tests
{
    // Feature: neckbook-books-proxy — Properties 4, 5, 6, 7 (Tasks 7.4, 7.5, 7.6, 7.7)
    // Validates: Requirements 13.1, 13.2, 13.3, 13.4, 13.5, 13.6, 13.7, 14.1, 14.2, 14.3, 14.4, 14.5, 14.6, 14.7, 15.3
    public sealed class NeckBookPayloadAssemblerPropertyTests
    {
        public sealed record BookFormValidationCase(
            string? Disciplina,
            int? ExerciciosPorCapitulo,
            int? NumCapitulos,
            bool? SuporteCaa,
            string? EstiloVisual,
            string[]? Interesses,
            bool ExpectedValid);

        public sealed record EnumDisplayMappingCase(
            Subject Subject,
            Serie Serie,
            TypeOfDisability Disability,
            CARSResult CarsResult,
            SnapIVResult SnapIvResult,
            CognitiveResult CognitiveResult,
            ScaleDIResult ScaleDiResult);

        public sealed record AbsentSourcesCase(
            string? StudentFullName,
            string? Cpf,
            string? ParentName,
            string? TeacherName,
            string? Address,
            string? SchoolName,
            Serie Serie,
            TypeOfDisability Disability,
            CARSResult CarsResult,
            SnapIVResult SnapIvResult,
            CognitiveResult CognitiveResult,
            ScaleDIResult ScaleDiResult,
            double? PortageScore,
            double? CarsScore,
            double? SnapIvScore,
            int? NumCapitulos,
            bool? SuporteCaa,
            string? EstiloVisual,
            string[]? Interesses,
            bool IncludePsychologistConclusion,
            bool IncludeSocialWorkerConclusion);

        public sealed record NumericScoreCase(
            double PortageScore,
            double CarsScore,
            int SnapDesattention,
            int SnapHyperactivity);

        public static class AssemblerArbContainer
        {
            private static readonly Subject[] DefinedSubjects =
                Enum.GetValues<Subject>().Where(s => s != Subject.None).ToArray();

            private static readonly Serie[] ValidSeries =
            {
                Serie.EarlyChildhoodEducation,
                Serie.FirstYear,
                Serie.SecondYear,
                Serie.ThirdYear,
                Serie.FourthYear,
                Serie.FifthYear
            };

            private static readonly Serie[] InvalidSeries =
            {
                Serie.None,
                Serie.SixthYear,
                Serie.SeventhYear,
                Serie.EighthYear,
                Serie.NinthYear,
                Serie.FirstYearHighSchool,
                Serie.SecondYearHighSchool,
                Serie.ThirdYearHighSchool,
                (Serie)999
            };

            private static readonly TypeOfDisability[] DefinedDisabilities =
                Enum.GetValues<TypeOfDisability>().Where(d => d != TypeOfDisability.None).ToArray();

            private static readonly CARSResult[] DefinedCarsResults =
                Enum.GetValues<CARSResult>().Where(r => r != CARSResult.None).ToArray();

            private static readonly SnapIVResult[] DefinedSnapResults =
                Enum.GetValues<SnapIVResult>().Where(r => r != SnapIVResult.None).ToArray();

            private static readonly CognitiveResult[] DefinedCognitiveResults =
                Enum.GetValues<CognitiveResult>().Where(r => r != CognitiveResult.None).ToArray();

            private static readonly ScaleDIResult[] DefinedScaleDiResults =
                Enum.GetValues<ScaleDIResult>().Where(r => r != ScaleDIResult.None).ToArray();

            public static Arbitrary<BookFormValidationCase> BookFormCase()
            {
                var validDisciplineGen =
                    from s in Gen.Elements(DefinedSubjects)
                    from useDisplay in Arb.Generate<bool>()
                    select (string?)(useDisplay ? s.GetDisplayName() : s.ToString());

                var invalidDisciplineGen = Gen.Elements<string?>(
                    null,
                    "",
                    "   ",
                    "None",
                    "Nenhum(a)",
                    "Fisica Quantica",
                    "Biologia Marinha",
                    "999");

                var validExGen = Gen.Elements<int?>(null, 1, 2, 3);
                var invalidExGen = Gen.Elements<int?>(-10, -1, 0, 4, 5, 10, 100);

                var gen =
                    from dValid in Arb.Generate<bool>()
                    from d in dValid ? validDisciplineGen : invalidDisciplineGen
                    from exValid in Arb.Generate<bool>()
                    from ex in exValid ? validExGen : invalidExGen
                    from numCap in Gen.Elements<int?>(null, 3, 5, 8)
                    from caa in Gen.Elements<bool?>(null, true, false)
                    from estilo in Gen.Elements<string?>(null, "ilustrado", "realista")
                    from interesses in Gen.Elements<string[]?>(null, new[] { "futebol", "dinossauros" })
                    select new BookFormValidationCase(
                        d,
                        ex,
                        numCap,
                        caa,
                        estilo,
                        interesses,
                        dValid && exValid);

                return Arb.From(gen);
            }

            public static Arbitrary<EnumDisplayMappingCase> EnumMappingCase()
            {
                var gen =
                    from subj in Gen.Elements(DefinedSubjects)
                    from serie in Gen.Elements(ValidSeries)
                    from dis in Gen.Elements(DefinedDisabilities)
                    from cars in Gen.Elements(DefinedCarsResults)
                    from snap in Gen.Elements(DefinedSnapResults)
                    from cog in Gen.Elements(DefinedCognitiveResults)
                    from scale in Gen.Elements(DefinedScaleDiResults)
                    select new EnumDisplayMappingCase(subj, serie, dis, cars, snap, cog, scale);

                return Arb.From(gen);
            }

            public static Arbitrary<AbsentSourcesCase> AbsentCase()
            {
                Gen<string?> OptionalText(string sample) =>
                    Gen.Elements<string?>(null, "", "   ", sample);

                var gen =
                    from studentName in OptionalText("Aluno Teste")
                    from cpf in OptionalText("12345678901")
                    from parent in OptionalText("Responsável Teste")
                    from teacher in OptionalText("Professor Teste")
                    from address in OptionalText("Rua A, 123")
                    from school in OptionalText("Escola Municipal")
                    from serieValid in Arb.Generate<bool>()
                    from serie in serieValid ? Gen.Elements(ValidSeries) : Gen.Elements(InvalidSeries)
                    from disValid in Arb.Generate<bool>()
                    from dis in disValid ? Gen.Elements(DefinedDisabilities) : Gen.Elements(TypeOfDisability.None, (TypeOfDisability)999)
                    from carsValid in Arb.Generate<bool>()
                    from cars in carsValid ? Gen.Elements(DefinedCarsResults) : Gen.Elements(CARSResult.None, (CARSResult)999)
                    from snapValid in Arb.Generate<bool>()
                    from snap in snapValid ? Gen.Elements(DefinedSnapResults) : Gen.Elements(SnapIVResult.None, (SnapIVResult)999)
                    from cogValid in Arb.Generate<bool>()
                    from cog in cogValid ? Gen.Elements(DefinedCognitiveResults) : Gen.Elements(CognitiveResult.None, (CognitiveResult)999)
                    from scaleValid in Arb.Generate<bool>()
                    from scale in scaleValid ? Gen.Elements(DefinedScaleDiResults) : Gen.Elements(ScaleDIResult.None, (ScaleDIResult)999)
                    from portageScore in Gen.Elements<double?>(null, -5.0, -0.1, 0.0, 12.5)
                    from carsScore in Gen.Elements<double?>(null, -10.0, -1.0, 0.0, 32.5)
                    from snapScore in Gen.Elements<double?>(null, -3.0, -1.0, 0.0, 8.0)
                    from numCap in Gen.Elements<int?>(null, 4)
                    from caa in Gen.Elements<bool?>(null, true)
                    from estilo in OptionalText("ilustrado")
                    from interesses in Gen.Elements<string[]?>(null, Array.Empty<string>(), new[] { "  " }, new[] { "música" })
                    from incPsy in Arb.Generate<bool>()
                    from incSw in Arb.Generate<bool>()
                    select new AbsentSourcesCase(
                        studentName, cpf, parent, teacher, address, school,
                        serie, dis, cars, snap, cog, scale,
                        portageScore, carsScore, snapScore,
                        numCap, caa, estilo, interesses,
                        incPsy, incSw);

                return Arb.From(gen);
            }

            public static Arbitrary<NumericScoreCase> NumericCase()
            {
                var gen =
                    from portage in Gen.Choose(0, 200).Select(x => (double)x)
                    from carsWhole in Gen.Choose(0, 60)
                    from carsHalf in Gen.Elements(0.0, 0.5)
                    from des in Gen.Choose(0, 9)
                    from hyp in Gen.Choose(0, 9)
                    select new NumericScoreCase(portage, carsWhole + carsHalf, des, hyp);

                return Arb.From(gen);
            }
        }

        // Feature: neckbook-books-proxy, Property 4: Validação de disciplina e exercicios_por_capitulo
        [Property(MaxTest = 100, Arbitrary = new[] { typeof(AssemblerArbContainer) })]
        public Property Discipline_and_exercises_validation(BookFormValidationCase testCase)
        {
            var assembler = new NeckBookPayloadAssembler();
            var student = new Student { ID = Guid.NewGuid(), FullName = "Aluno" };
            var form = new BookForm
            {
                StudentId = student.ID,
                Disciplina = testCase.Disciplina,
                ExerciciosPorCapitulo = testCase.ExerciciosPorCapitulo,
                NumCapitulos = testCase.NumCapitulos,
                SuporteCaa = testCase.SuporteCaa,
                EstiloVisual = testCase.EstiloVisual,
                Interesses = testCase.Interesses
            };

            if (testCase.ExpectedValid)
            {
                var result = assembler.Assemble(form, student);
                var exMatch = result.BookParams.ExerciciosPorCapitulo == testCase.ExerciciosPorCapitulo;
                return (!string.IsNullOrWhiteSpace(result.BookParams.Disciplina) && exMatch)
                    .Label($"Expected valid BookParams for Disciplina='{testCase.Disciplina}', Exercicios={testCase.ExerciciosPorCapitulo}");
            }

            try
            {
                assembler.Assemble(form, student);
                return false.Label($"Expected NeckBookContractValidationException for Disciplina='{testCase.Disciplina}', Exercicios={testCase.ExerciciosPorCapitulo}");
            }
            catch (NeckBookContractValidationException)
            {
                return true.ToProperty();
            }
        }

        // Feature: neckbook-books-proxy, Property 5: Mapeamento de enum para Display name em book_params e student_profile
        [Property(MaxTest = 100, Arbitrary = new[] { typeof(AssemblerArbContainer) })]
        public Property Enum_maps_to_display_name(EnumDisplayMappingCase testCase)
        {
            var assembler = new NeckBookPayloadAssembler();
            var student = new Student
            {
                ID = Guid.NewGuid(),
                FullName = "Aluno",
                Serie = testCase.Serie
            };

            var bundle = new ReportsBundle
            {
                PsychologistReport = new PsychologistReport
                {
                    SnapIVQuestionnaire = new SnapIVQuestionnaire { Result = testCase.SnapIvResult },
                    AdaptiveBehaviorScaleDI = new AdaptiveBehaviorScaleDI { Result = testCase.ScaleDiResult }
                },
                PsychopedagogueReport = new PsychopedagogueReport
                {
                    CARSQuestionnaire = new CARSQuestionnaire { Result = testCase.CarsResult },
                    CognitiveAssessment = new CognitiveAssessment { Result = testCase.CognitiveResult },
                    PsychopedagogueQuestionnaire = new PsychopedagogueQuestionnaire
                    {
                        TypeOfDisability = new List<PsychopedagogueQuestionnaire_TypeOfDisability>
                        {
                            new() { Disability = testCase.Disability }
                        }
                    }
                }
            };

            var form = new BookForm
            {
                StudentId = student.ID,
                Disciplina = testCase.Subject.ToString()
            };

            var payload = assembler.Assemble(form, student, bundle);
            var sp = payload.StudentProfile;

            var ok =
                payload.BookParams.Disciplina == testCase.Subject.GetDisplayName() &&
                sp.Serie == testCase.Serie.GetDisplayName() &&
                sp.DiagnosticoPrincipal == testCase.Disability.GetDisplayName() &&
                sp.TypeDisability == testCase.Disability.GetDisplayName() &&
                sp.CarsResultado == testCase.CarsResult.GetDisplayName() &&
                sp.SnapIvResultado == testCase.SnapIvResult.GetDisplayName() &&
                sp.CognitivoResultado == testCase.CognitiveResult.GetDisplayName() &&
                sp.EscalaDiResultado == testCase.ScaleDiResult.GetDisplayName();

            return ok.Label("All defined enum values must map to their exact PT-BR Display(Name)");
        }

        // Feature: neckbook-books-proxy, Property 6: Omissão de campos de origem ausentes (contrato aditivo)
        [Property(MaxTest = 100, Arbitrary = new[] { typeof(AssemblerArbContainer) })]
        public Property Absent_sources_are_omitted(AbsentSourcesCase c)
        {
            var assembler = new NeckBookPayloadAssembler();
            var student = new Student
            {
                ID = Guid.NewGuid(),
                FullName = c.StudentFullName ?? string.Empty,
                CPF = c.Cpf,
                NameOfPersonResponsible = c.ParentName ?? string.Empty,
                Address = c.Address ?? string.Empty,
                Serie = c.Serie,
                School = c.SchoolName is null ? null : new School { Name = c.SchoolName }
            };

            var teacher = c.TeacherName is null ? null : new ApplicationUser { FullName = c.TeacherName };

            var bundle = new ReportsBundle
            {
                Teacher = teacher,
                PortageScore = c.PortageScore,
                CarsScore = c.CarsScore,
                SnapIvScore = c.SnapIvScore,
                PsychologistReport = new PsychologistReport
                {
                    Conclusion = c.IncludePsychologistConclusion ? "Conclusão psicólogo" : "   ",
                    SnapIVQuestionnaire = new SnapIVQuestionnaire
                    {
                        Result = c.SnapIvResult,
                        DesattentionCount = -1,
                        HyperactivityCount = -1
                    },
                    AdaptiveBehaviorScaleDI = new AdaptiveBehaviorScaleDI { Result = c.ScaleDiResult }
                },
                PsychopedagogueReport = new PsychopedagogueReport
                {
                    Conclusion = null,
                    CARSQuestionnaire = new CARSQuestionnaire
                    {
                        Result = c.CarsResult,
                        TotalScore = c.CarsScore.HasValue ? c.CarsScore.Value.ToString(CultureInfo.InvariantCulture) : null!
                    },
                    CognitiveAssessment = new CognitiveAssessment { Result = c.CognitiveResult },
                    PsychopedagogueQuestionnaire = new PsychopedagogueQuestionnaire
                    {
                        TypeOfDisability = new List<PsychopedagogueQuestionnaire_TypeOfDisability>
                        {
                            new() { Disability = c.Disability }
                        }
                    }
                },
                SocialWorkerReport = c.IncludeSocialWorkerConclusion
                    ? new SocialWorkerReport { Conclusion = "Conclusão assistente social" }
                    : new SocialWorkerReport { Conclusion = "  ", SocioeconomicContext = null }
            };

            var form = new BookForm
            {
                StudentId = student.ID,
                Disciplina = Subject.Portuguese.ToString(),
                NumCapitulos = c.NumCapitulos,
                SuporteCaa = c.SuporteCaa,
                EstiloVisual = c.EstiloVisual,
                Interesses = c.Interesses
            };

            var payload = assembler.Assemble(form, student, bundle, teacher);
            var json = NeckBookPayloadAssembler.Serialize(payload);
            using var doc = JsonDocument.Parse(json);

            var bpEl = doc.RootElement.GetProperty("book_params");
            var spEl = doc.RootElement.GetProperty("student_profile");
            var repEl = doc.RootElement.GetProperty("reports");

            static bool HasProp(JsonElement el, string name) => el.TryGetProperty(name, out _);
            static bool HasText(string? s) => !string.IsNullOrWhiteSpace(s);

            bool serieShouldExist = Enum.IsDefined(typeof(Serie), c.Serie) &&
                                    c.Serie >= Serie.EarlyChildhoodEducation &&
                                    c.Serie <= Serie.FifthYear;
            bool disShouldExist = Enum.IsDefined(typeof(TypeOfDisability), c.Disability) &&
                                  c.Disability != TypeOfDisability.None;
            bool carsResShouldExist = Enum.IsDefined(typeof(CARSResult), c.CarsResult) &&
                                      c.CarsResult != CARSResult.None;
            bool snapResShouldExist = Enum.IsDefined(typeof(SnapIVResult), c.SnapIvResult) &&
                                      c.SnapIvResult != SnapIVResult.None;
            bool cogResShouldExist = Enum.IsDefined(typeof(CognitiveResult), c.CognitiveResult) &&
                                     c.CognitiveResult != CognitiveResult.None;
            bool scaleResShouldExist = Enum.IsDefined(typeof(ScaleDIResult), c.ScaleDiResult) &&
                                       c.ScaleDiResult != ScaleDIResult.None;

            bool portageShouldExist = c.PortageScore.HasValue && c.PortageScore.Value >= 0;
            bool carsScoreShouldExist = c.CarsScore.HasValue && c.CarsScore.Value >= 0;
            bool snapScoreShouldExist = c.SnapIvScore.HasValue && c.SnapIvScore.Value >= 0;

            bool interessesShouldExist = c.Interesses is not null && c.Interesses.Any(HasText);
            bool psyReportShouldExist = c.IncludePsychologistConclusion || snapResShouldExist || scaleResShouldExist;
            bool ppReportShouldExist = carsResShouldExist || cogResShouldExist;

            var checks =
                HasProp(bpEl, "num_capitulos") == c.NumCapitulos.HasValue &&
                HasProp(bpEl, "suporte_caa") == c.SuporteCaa.HasValue &&
                HasProp(bpEl, "estilo_visual") == HasText(c.EstiloVisual) &&
                HasProp(bpEl, "interesses") == interessesShouldExist &&
                HasProp(spEl, "student_name") == HasText(c.StudentFullName) &&
                HasProp(spEl, "cpf") == HasText(c.Cpf) &&
                HasProp(spEl, "parent_name") == HasText(c.ParentName) &&
                HasProp(spEl, "teacher_name") == HasText(c.TeacherName) &&
                HasProp(spEl, "address") == HasText(c.Address) &&
                HasProp(spEl, "school_name") == HasText(c.SchoolName) &&
                HasProp(spEl, "serie") == serieShouldExist &&
                HasProp(spEl, "diagnostico_principal") == disShouldExist &&
                HasProp(spEl, "type_disability") == disShouldExist &&
                HasProp(spEl, "cars_resultado") == carsResShouldExist &&
                HasProp(spEl, "snap_iv_resultado") == snapResShouldExist &&
                HasProp(spEl, "cognitivo_resultado") == cogResShouldExist &&
                HasProp(spEl, "escala_di_resultado") == scaleResShouldExist &&
                HasProp(spEl, "scores_portage") == portageShouldExist &&
                HasProp(spEl, "cars_score") == carsScoreShouldExist &&
                HasProp(spEl, "snap_iv") == snapScoreShouldExist &&
                HasProp(repEl, "psicologo") == psyReportShouldExist &&
                HasProp(repEl, "psicopedagogo") == ppReportShouldExist &&
                HasProp(repEl, "assistente_social") == c.IncludeSocialWorkerConclusion &&
                !HasProp(repEl, "professor_regular") &&
                !HasProp(repEl, "pei_resumo") &&
                !HasProp(repEl, "paee_resumo");

            return checks.Label($"Additive contract check failed for serialized JSON: {json}");
        }

        // Feature: neckbook-books-proxy, Property 7: Escores numéricos válidos são preenchidos
        [Property(MaxTest = 100, Arbitrary = new[] { typeof(AssemblerArbContainer) })]
        public Property Numeric_scores_are_filled(NumericScoreCase c)
        {
            var assembler = new NeckBookPayloadAssembler();
            var student = new Student { ID = Guid.NewGuid(), FullName = "Aluno" };
            var bundle = new ReportsBundle
            {
                PortageScore = c.PortageScore,
                PsychologistReport = new PsychologistReport
                {
                    SnapIVQuestionnaire = new SnapIVQuestionnaire
                    {
                        DesattentionCount = c.SnapDesattention,
                        HyperactivityCount = c.SnapHyperactivity
                    }
                },
                PsychopedagogueReport = new PsychopedagogueReport
                {
                    CARSQuestionnaire = new CARSQuestionnaire
                    {
                        TotalScore = c.CarsScore.ToString(CultureInfo.InvariantCulture)
                    }
                }
            };

            var form = new BookForm
            {
                StudentId = student.ID,
                Disciplina = Subject.Mathematics.ToString()
            };

            var payload = assembler.Assemble(form, student, bundle);
            var sp = payload.StudentProfile;
            var expectedSnap = (double)(c.SnapDesattention + c.SnapHyperactivity);

            var ok =
                sp.ScoresPortage == c.PortageScore &&
                sp.CarsScore == c.CarsScore &&
                sp.SnapIv == expectedSnap;

            return ok.Label($"Expected scores ({c.PortageScore}, {c.CarsScore}, {expectedSnap}), got ({sp.ScoresPortage}, {sp.CarsScore}, {sp.SnapIv})");
        }
    }
}
