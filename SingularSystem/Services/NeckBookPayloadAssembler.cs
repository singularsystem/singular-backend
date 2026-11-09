using System.Globalization;
using System.Reflection;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SingularSystem.DTOs.NeckBook;
using SingularSystem.Extensions;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace SingularSystem.Services
{
    /// <summary>
    /// Exceção lançada quando os parâmetros de entrada para geração de livro violam o contrato
    /// do NeckBook (ex.: disciplina ausente/não-mapeável a <see cref="Subject"/> ou
    /// <c>exercicios_por_capitulo</c> fora do intervalo <c>1..3</c>) (Req 13.1, 13.3, 13.5).
    /// </summary>
    public sealed class NeckBookContractValidationException : Exception
    {
        public string Code { get; }

        public NeckBookContractValidationException(string message, string code = "CONTRACT_VALIDATION_ERROR")
            : base(message)
        {
            Code = code;
        }
    }

    /// <summary>
    /// Monta o payload estruturado <c>generation_params</c> (<c>book_params</c>,
    /// <c>student_profile</c> e <c>reports</c>) a partir dos dados cadastrais e clínicos do aluno
    /// no banco do iPAAGS combinados com o formulário recebido (Req 13, 14, 15).
    /// </summary>
    public class NeckBookPayloadAssembler
    {
        /// <summary>
        /// Opções de serialização JSON padronizadas (<c>camelCase</c> + enums como <c>string</c> +
        /// omissão de nulos), espelhando <c>NeckSignService.JsonOptions</c> (Req 15.4).
        /// </summary>
        public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() },
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly AppDbContext? _db;

        public NeckBookPayloadAssembler()
        {
        }

        public NeckBookPayloadAssembler(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Serializa o <see cref="GenerationParams"/> com <see cref="JsonOptions"/>
        /// (<c>JsonSerializerDefaults.Web</c> + <c>JsonStringEnumConverter</c> + <c>WhenWritingNull</c>) (Req 15.4).
        /// </summary>
        public static string Serialize(GenerationParams payload)
        {
            ArgumentNullException.ThrowIfNull(payload);
            return JsonSerializer.Serialize(payload, JsonOptions);
        }

        /// <summary>
        /// Tenta resolver uma string de disciplina para um membro definido (<c>!= Subject.None</c>)
        /// do enum <see cref="Subject"/> (por nome do membro, por <c>[Display(Name)]</c> PT-BR ou
        /// por apelido sem acento).
        /// </summary>
        public static bool TryResolveSubject(string? rawDisciplina, out Subject subject)
        {
            subject = Subject.None;
            if (string.IsNullOrWhiteSpace(rawDisciplina))
            {
                return false;
            }

            var trimmed = rawDisciplina.Trim();

            // Rejeita inteiros puros (ex.: "1", "99") para que apenas nomes/display válidos de Subject sejam aceitos
            if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
            {
                return false;
            }

            foreach (var candidate in Enum.GetValues<Subject>())
            {
                if (candidate == Subject.None)
                {
                    continue;
                }

                var memberName = candidate.ToString();
                var displayName = candidate.GetDisplayName();

                if (string.Equals(memberName, trimmed, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(displayName, trimmed, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(RemoveDiacritics(displayName), RemoveDiacritics(trimmed), StringComparison.OrdinalIgnoreCase))
                {
                    subject = candidate;
                    return true;
                }
            }

            // Apelidos curtos sem acento comuns em formulários (ex.: "portugues" -> Subject.Portuguese)
            var normalizedSlug = RemoveDiacritics(trimmed).ToLowerInvariant();
            switch (normalizedSlug)
            {
                case "portugues":
                case "lingua portuguesa":
                    subject = Subject.Portuguese;
                    return true;
                case "matematica":
                    subject = Subject.Mathematics;
                    return true;
                case "ciencias":
                    subject = Subject.Science;
                    return true;
                case "historia":
                    subject = Subject.History;
                    return true;
                case "geografia":
                    subject = Subject.Geography;
                    return true;
                case "ingles":
                    subject = Subject.English;
                    return true;
                case "educacao infantil":
                case "educacao_infantil":
                    subject = Subject.EarlyChildhoodEducation;
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Valida e monta o bloco <c>book_params</c> a partir do <see cref="BookForm"/> recebido (Req 13).
        /// Lança <see cref="NeckBookContractValidationException"/> se <c>disciplina</c> for ausente/vazia
        /// ou não corresponder a um membro definido de <see cref="Subject"/>, ou se
        /// <c>exercicios_por_capitulo</c> estiver fora de <c>[1, 3]</c>.
        /// </summary>
        public static BookParams AssembleBookParams(BookForm form)
        {
            ArgumentNullException.ThrowIfNull(form);

            if (!TryResolveSubject(form.Disciplina, out var subject))
            {
                throw new NeckBookContractValidationException(
                    $"O campo 'disciplina' é obrigatório e deve corresponder a uma disciplina válida do enum Subject (valor recebido: '{form.Disciplina}').");
            }

            if (form.ExerciciosPorCapitulo.HasValue)
            {
                var ex = form.ExerciciosPorCapitulo.Value;
                if (ex < 1 || ex > 3)
                {
                    throw new NeckBookContractValidationException(
                        $"O campo 'exercicios_por_capitulo' deve ser um inteiro entre 1 e 3 (valor recebido: {ex}).");
                }
            }

            string[]? interesses = null;
            if (form.Interesses is not null)
            {
                var cleaned = form.Interesses
                    .Where(i => !string.IsNullOrWhiteSpace(i))
                    .Select(i => i.Trim())
                    .ToArray();
                if (cleaned.Length > 0)
                {
                    interesses = cleaned;
                }
            }

            return new BookParams
            {
                Disciplina = subject.GetDisplayName(),
                ExerciciosPorCapitulo = form.ExerciciosPorCapitulo,
                NumCapitulos = form.NumCapitulos,
                SuporteCaa = form.SuporteCaa,
                EstiloVisual = NullIfWhiteSpace(form.EstiloVisual),
                Interesses = interesses,
                Tema = NullIfWhiteSpace(form.Tema),
                Observacoes = NullIfWhiteSpace(form.Observacoes)
            };
        }

        /// <summary>
        /// Monta de forma determinística e síncrona o <see cref="GenerationParams"/> a partir de
        /// <see cref="BookForm"/>, <see cref="Student"/> e <see cref="ReportsBundle"/> (Req 13, 14, 15).
        /// </summary>
        public GenerationParams Assemble(
            BookForm form,
            Student student,
            ReportsBundle? reports = null,
            ApplicationUser? teacher = null)
        {
            ArgumentNullException.ThrowIfNull(form);
            ArgumentNullException.ThrowIfNull(student);

            var bundle = reports ?? new ReportsBundle();
            var effectiveTeacher = teacher ?? bundle.Teacher;

            var bookParams = AssembleBookParams(form);
            var studentProfile = AssembleStudentProfile(student, bundle, effectiveTeacher);
            var reportsSummary = AssembleReports(bundle);

            return new GenerationParams
            {
                BookParams = bookParams,
                StudentProfile = studentProfile,
                Reports = reportsSummary
            };
        }

        /// <summary>
        /// Monta o bloco <c>student_profile</c> a partir das entidades reais confirmadas
        /// (<see cref="Student"/>, <see cref="ApplicationUser"/>, <see cref="School"/> e
        /// relatórios/instrumentos em <see cref="ReportsBundle"/>) (Req 14.1–14.7).
        /// </summary>
        public static StudentProfile AssembleStudentProfile(
            Student student,
            ReportsBundle? bundle = null,
            ApplicationUser? teacher = null,
            string? fallbackSchoolName = null)
        {
            ArgumentNullException.ThrowIfNull(student);
            bundle ??= new ReportsBundle();
            var effectiveTeacher = teacher ?? bundle.Teacher;

            // Req 14.2 / 14.7: Serie apenas no intervalo inclusivo EarlyChildhoodEducation (1) .. FifthYear (6)
            string? serieDisplay = null;
            if (Enum.IsDefined(typeof(Serie), student.Serie) &&
                student.Serie >= Serie.EarlyChildhoodEducation &&
                student.Serie <= Serie.FifthYear)
            {
                serieDisplay = student.Serie.GetDisplayName();
            }

            // Req 14.3: TypeOfDisability via PsychopedagogueReport.PsychopedagogueQuestionnaire.TypeOfDisability
            string? disabilityDisplay = null;
            var disabilityItems = bundle.PsychopedagogueReport?.PsychopedagogueQuestionnaire?.TypeOfDisability;
            if (disabilityItems is not null)
            {
                var firstDefined = disabilityItems
                    .Select(d => d.Disability)
                    .FirstOrDefault(d => Enum.IsDefined(typeof(TypeOfDisability), d) && d != TypeOfDisability.None);

                if (Enum.IsDefined(typeof(TypeOfDisability), firstDefined) && firstDefined != TypeOfDisability.None)
                {
                    disabilityDisplay = firstDefined.GetDisplayName();
                }
            }

            // Req 14.4: resultados de escalas (CARSResult, SnapIVResult, CognitiveResult, ScaleDIResult)
            string? carsResultado = null;
            var cars = bundle.PsychopedagogueReport?.CARSQuestionnaire;
            if (cars is not null && Enum.IsDefined(typeof(CARSResult), cars.Result) && cars.Result != CARSResult.None)
            {
                carsResultado = cars.Result.GetDisplayName();
            }

            string? snapIvResultado = null;
            var snapIv = bundle.PsychologistReport?.SnapIVQuestionnaire;
            if (snapIv is not null && Enum.IsDefined(typeof(SnapIVResult), snapIv.Result) && snapIv.Result != SnapIVResult.None)
            {
                snapIvResultado = snapIv.Result.GetDisplayName();
            }

            string? cognitivoResultado = null;
            var cognitive = bundle.PsychopedagogueReport?.CognitiveAssessment;
            if (cognitive is not null && Enum.IsDefined(typeof(CognitiveResult), cognitive.Result) && cognitive.Result != CognitiveResult.None)
            {
                cognitivoResultado = cognitive.Result.GetDisplayName();
            }

            string? escalaDiResultado = null;
            var scaleDi = bundle.PsychologistReport?.AdaptiveBehaviorScaleDI;
            if (scaleDi is not null && Enum.IsDefined(typeof(ScaleDIResult), scaleDi.Result) && scaleDi.Result != ScaleDIResult.None)
            {
                escalaDiResultado = scaleDi.Result.GetDisplayName();
            }

            // Req 14.5 / 14.7: escores numéricos >= 0 (scores_portage, cars_score, snap_iv)
            double? carsScore = ResolveValidScore(bundle.CarsScore);
            if (!carsScore.HasValue && cars is not null && !string.IsNullOrWhiteSpace(cars.TotalScore))
            {
                var rawScore = cars.TotalScore.Trim().Replace(',', '.');
                if (double.TryParse(rawScore, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedCars) &&
                    !double.IsNaN(parsedCars) && !double.IsInfinity(parsedCars) && parsedCars >= 0)
                {
                    carsScore = parsedCars;
                }
            }

            double? snapIvScore = ResolveValidScore(bundle.SnapIvScore);
            if (!snapIvScore.HasValue && snapIv is not null)
            {
                if (snapIv.DesattentionCount >= 0 && snapIv.HyperactivityCount >= 0)
                {
                    var totalSnap = (double)snapIv.DesattentionCount + snapIv.HyperactivityCount;
                    if (totalSnap >= 0)
                    {
                        snapIvScore = totalSnap;
                    }
                }
            }

            double? portageScore = ResolveValidScore(bundle.PortageScore);
            if (!portageScore.HasValue)
            {
                var portage = bundle.PortageGuide ?? bundle.PAEE?.Portage;
                if (portage?.Responses is not null && portage.Responses.Count > 0)
                {
                    var aloneCount = portage.Responses.Count(r => r.Value is not null && r.Value.Response == PlanResponse.Alone);
                    portageScore = aloneCount;
                }
            }

            var schoolName = NullIfWhiteSpace(student.School?.Name) ?? NullIfWhiteSpace(fallbackSchoolName);

            return new StudentProfile
            {
                StudentName = NullIfWhiteSpace(student.FullName),
                Cpf = NullIfWhiteSpace(student.CPF),
                ParentName = NullIfWhiteSpace(student.NameOfPersonResponsible),
                TeacherName = NullIfWhiteSpace(effectiveTeacher?.FullName),
                Address = NullIfWhiteSpace(student.Address),
                SchoolName = schoolName,
                Serie = serieDisplay,
                DiagnosticoPrincipal = disabilityDisplay,
                TypeDisability = disabilityDisplay,
                CarsResultado = carsResultado,
                SnapIvResultado = snapIvResultado,
                CognitivoResultado = cognitivoResultado,
                EscalaDiResultado = escalaDiResultado,
                ScoresPortage = portageScore,
                CarsScore = carsScore,
                SnapIv = snapIvScore
            };
        }

        /// <summary>
        /// Monta o bloco <c>reports</c> com resumos textuais determinísticos separados por <c>" | "</c>
        /// e omitindo chaves sem campos preenchidos (Req 15.1–15.3).
        /// </summary>
        public static ReportsSummary AssembleReports(ReportsBundle? bundle)
        {
            if (bundle is null)
            {
                return new ReportsSummary();
            }

            // 1. psicologo: Conclusion; SnapIVQuestionnaire.Result (Display); AdaptiveBehaviorScaleDI.Result (Display)
            string? psicologo = null;
            if (bundle.PsychologistReport is not null)
            {
                var pr = bundle.PsychologistReport;
                var snapDisplay = pr.SnapIVQuestionnaire is not null &&
                                  Enum.IsDefined(typeof(SnapIVResult), pr.SnapIVQuestionnaire.Result) &&
                                  pr.SnapIVQuestionnaire.Result != SnapIVResult.None
                    ? pr.SnapIVQuestionnaire.Result.GetDisplayName()
                    : null;

                var scaleDiDisplay = pr.AdaptiveBehaviorScaleDI is not null &&
                                     Enum.IsDefined(typeof(ScaleDIResult), pr.AdaptiveBehaviorScaleDI.Result) &&
                                     pr.AdaptiveBehaviorScaleDI.Result != ScaleDIResult.None
                    ? pr.AdaptiveBehaviorScaleDI.Result.GetDisplayName()
                    : null;

                psicologo = FormatDeterministicSummary(
                    ("Conclusão", pr.Conclusion),
                    ("SNAP-IV", snapDisplay),
                    ("Escala DI", scaleDiDisplay));
            }

            // 2. psicopedagogo: Conclusion; CARSQuestionnaire.Result (Display); CognitiveAssessment.Result (Display)
            string? psicopedagogo = null;
            if (bundle.PsychopedagogueReport is not null)
            {
                var pp = bundle.PsychopedagogueReport;
                var carsDisplay = pp.CARSQuestionnaire is not null &&
                                  Enum.IsDefined(typeof(CARSResult), pp.CARSQuestionnaire.Result) &&
                                  pp.CARSQuestionnaire.Result != CARSResult.None
                    ? pp.CARSQuestionnaire.Result.GetDisplayName()
                    : null;

                var cogDisplay = pp.CognitiveAssessment is not null &&
                                 Enum.IsDefined(typeof(CognitiveResult), pp.CognitiveAssessment.Result) &&
                                 pp.CognitiveAssessment.Result != CognitiveResult.None
                    ? pp.CognitiveAssessment.Result.GetDisplayName()
                    : null;

                psicopedagogo = FormatDeterministicSummary(
                    ("Conclusão", pp.Conclusion),
                    ("CARS", carsDisplay),
                    ("Avaliação Cognitiva", cogDisplay));
            }

            // 3. assistente_social: SocioeconomicContext; Conclusion
            string? assistenteSocial = null;
            if (bundle.SocialWorkerReport is not null)
            {
                var sw = bundle.SocialWorkerReport;
                assistenteSocial = FormatDeterministicSummary(
                    ("Contexto Socioeconômico", sw.SocioeconomicContext),
                    ("Conclusão", sw.Conclusion));
            }

            // 4. professor_regular: ClassroomDevelopment; ClassroomBehavior; MainDifficulties; LearningCharacteristics; BehaviorObservations; AdditionalComments
            string? professorRegular = null;
            if (bundle.RegularTeacherQuestionnaire is not null)
            {
                var rt = bundle.RegularTeacherQuestionnaire;
                professorRegular = FormatDeterministicSummary(
                    ("Desenvolvimento em Sala", rt.ClassroomDevelopment),
                    ("Comportamento em Sala", rt.ClassroomBehavior),
                    ("Principais Dificuldades", rt.MainDifficulties),
                    ("Características de Aprendizagem", rt.LearningCharacteristics),
                    ("Observações Comportamentais", rt.BehaviorObservations),
                    ("Comentários Adicionais", rt.AdditionalComments));
            }

            // 5. pei_resumo: AcademicRecord; MedicalReport; Medications; FollowUp; AdditionalObservations
            string? peiResumo = null;
            if (bundle.PEI is not null)
            {
                var pei = bundle.PEI;
                peiResumo = FormatDeterministicSummary(
                    ("Histórico Escolar", pei.AcademicRecord),
                    ("Laudo Médico", pei.MedicalReport),
                    ("Medicações", pei.Medications),
                    ("Acompanhamento", pei.FollowUp),
                    ("Observações Adicionais", pei.AdditionalObservations));
            }

            // 6. paee_resumo: AcademicRecord; MedicalReport; Medications; FollowUp; AdditionalObservations
            string? paeeResumo = null;
            if (bundle.PAEE is not null)
            {
                var paee = bundle.PAEE;
                paeeResumo = FormatDeterministicSummary(
                    ("Histórico Escolar", paee.AcademicRecord),
                    ("Laudo Médico", paee.MedicalReport),
                    ("Medicações", paee.Medications),
                    ("Acompanhamento", paee.FollowUp),
                    ("Observações Adicionais", paee.AdditionalObservations));
            }

            return new ReportsSummary
            {
                Psicologo = psicologo,
                Psicopedagogo = psicopedagogo,
                AssistenteSocial = assistenteSocial,
                ProfessorRegular = professorRegular,
                PeiResumo = peiResumo,
                PaeeResumo = paeeResumo
            };
        }

        private static string? PsychopedagogueOrNull(string? v) => v;

        /// <summary>
        /// Carrega os relatórios mais recentes do aluno no banco (<see cref="AppDbContext"/>) e monta
        /// o <see cref="GenerationParams"/> completo.
        /// </summary>
        public virtual async Task<GenerationParams> AssembleAsync(
            Student student,
            ApplicationUser requestingUser,
            BookForm form,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(student);
            ArgumentNullException.ThrowIfNull(requestingUser);
            ArgumentNullException.ThrowIfNull(form);

            var bookParams = AssembleBookParams(form);

            if (_db is null)
            {
                var emptyBundle = new ReportsBundle { Teacher = requestingUser };
                return new GenerationParams
                {
                    BookParams = bookParams,
                    StudentProfile = AssembleStudentProfile(student, emptyBundle, requestingUser),
                    Reports = AssembleReports(emptyBundle)
                };
            }

            var schoolName = student.School?.Name;
            if (string.IsNullOrWhiteSpace(schoolName) && student.SchoolID.HasValue)
            {
                schoolName = await _db.Schools
                    .AsNoTracking()
                    .Where(s => s.ID == student.SchoolID.Value)
                    .Select(s => s.Name)
                    .FirstOrDefaultAsync(ct);
            }

            var latestReport = await _db.Reports
                .AsNoTracking()
                .Include(r => r.SocialWorkerReport)
                .Include(r => r.PsychologistReport).ThenInclude(pr => pr!.SnapIVQuestionnaire)
                .Include(r => r.PsychologistReport).ThenInclude(pr => pr!.AdaptiveBehaviorScaleDI)
                .Include(r => r.PsychopedagogueReport).ThenInclude(pp => pp!.PsychopedagogueQuestionnaire).ThenInclude(pq => pq.TypeOfDisability)
                .Include(r => r.PsychopedagogueReport).ThenInclude(pp => pp!.CARSQuestionnaire)
                .Include(r => r.PsychopedagogueReport).ThenInclude(pp => pp!.CognitiveAssessment)
                .Where(r => r.StudentID == student.ID)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync(ct);

            var psychologistReport = latestReport?.PsychologistReport
                ?? await _db.PsychologistReports
                    .AsNoTracking()
                    .Include(pr => pr.SnapIVQuestionnaire)
                    .Include(pr => pr.AdaptiveBehaviorScaleDI)
                    .Where(pr => pr.StudentID == student.ID)
                    .OrderByDescending(pr => pr.CreatedAt)
                    .FirstOrDefaultAsync(ct);

            var psychopedagogueReport = latestReport?.PsychopedagogueReport
                ?? await _db.PsychopedagogueReports
                    .AsNoTracking()
                    .Include(pp => pp.PsychopedagogueQuestionnaire).ThenInclude(pq => pq.TypeOfDisability)
                    .Include(pp => pp.CARSQuestionnaire)
                    .Include(pp => pp.CognitiveAssessment)
                    .Where(pp => pp.StudentID == student.ID)
                    .OrderByDescending(pp => pp.CreatedAt)
                    .FirstOrDefaultAsync(ct);

            var socialWorkerReport = latestReport?.SocialWorkerReport
                ?? await _db.SocialWorkerReports
                    .AsNoTracking()
                    .Where(sw => sw.StudentID == student.ID)
                    .OrderByDescending(sw => sw.CreatedAt)
                    .FirstOrDefaultAsync(ct);

            var regularTeacher = await _db.RegularTeacherQuestionnaires
                .AsNoTracking()
                .Where(rt => rt.StudentID == student.ID)
                .OrderByDescending(rt => rt.CreatedAt)
                .FirstOrDefaultAsync(ct);

            var pei = await _db.PEIs
                .AsNoTracking()
                .Where(p => p.StudentID == student.ID)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync(ct);

            var paee = await _db.PAEEs
                .AsNoTracking()
                .Include(p => p.Portage).ThenInclude(pg => pg.Responses)
                .Where(p => p.StudentID == student.ID)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync(ct);

            var portageGuide = paee?.Portage
                ?? await _db.PortageGuides
                    .AsNoTracking()
                    .Include(pg => pg.Responses)
                    .Where(pg => pg.StudentID == student.ID)
                    .OrderByDescending(pg => pg.CreatedAt)
                    .FirstOrDefaultAsync(ct);

            var bundle = new ReportsBundle
            {
                Teacher = requestingUser,
                PsychologistReport = psychologistReport,
                PsychopedagogueReport = psychopedagogueReport,
                SocialWorkerReport = socialWorkerReport,
                RegularTeacherQuestionnaire = regularTeacher,
                PEI = pei,
                PAEE = paee,
                PortageGuide = portageGuide
            };

            return new GenerationParams
            {
                BookParams = bookParams,
                StudentProfile = AssembleStudentProfile(student, bundle, requestingUser, schoolName),
                Reports = AssembleReports(bundle)
            };
        }

        private static string? FormatDeterministicSummary(params (string Label, string? Value)[] fields)
        {
            var parts = new List<string>(fields.Length);
            foreach (var (label, raw) in fields)
            {
                var trimmed = NullIfWhiteSpace(raw);
                if (trimmed is not null)
                {
                    parts.Add($"{label}: {trimmed}");
                }
            }

            return parts.Count > 0 ? string.Join(" | ", parts) : null;
        }

        private static double? ResolveValidScore(double? candidate)
        {
            if (!candidate.HasValue)
            {
                return null;
            }

            var val = candidate.Value;
            if (double.IsNaN(val) || double.IsInfinity(val) || val < 0)
            {
                return null;
            }

            return val;
        }

        private static string? NullIfWhiteSpace(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static string RemoveDiacritics(string text)
        {
            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);
            foreach (var c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
