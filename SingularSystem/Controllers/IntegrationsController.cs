using SingularSystem.DTOs.Integrations;
using SingularSystem.Extensions;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace SingularSystem.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class IntegrationsController : ControllerBase
    {
        private readonly AppDbContext _appDbContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<IntegrationsController> _logger;

        public IntegrationsController(
            AppDbContext appDbContext,
            IConfiguration configuration,
            ILogger<IntegrationsController> logger)
        {
            _appDbContext = appDbContext;
            _configuration = configuration;
            _logger = logger;
        }

        private bool ValidateApiKey()
        {
            var expectedKey = _configuration["IntegrationSettings:ApiKey"] ?? "ipaags_aee_integration_key_2026";

            if (!Request.Headers.TryGetValue("X-Api-Key", out var extractedKey))
            {
                return false;
            }

            return string.Equals(expectedKey, extractedKey.ToString(), StringComparison.Ordinal);
        }

        [HttpGet("activities-summary")]
        public async Task<IActionResult> GetActivitiesSummary(
            [FromQuery] string? state,
            [FromQuery] string? city,
            [FromQuery] int? year,
            [FromQuery] int? month)
        {
            try
            {
                if (!ValidateApiKey())
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Chave de integração (X-Api-Key) inválida ou ausente."
                    });
                }

                if (string.IsNullOrWhiteSpace(state) || string.IsNullOrWhiteSpace(city) || !year.HasValue || !month.HasValue)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Estado, município, ano e mês são obrigatórios."
                    });
                }

                if (month.Value < 1 || month.Value > 12)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Mês inválido. Deve ser entre 1 e 12."
                    });
                }

                var cleanState = state.Trim();
                var cleanCity = city.Trim();
                var targetYear = year.Value;
                var targetMonth = month.Value;

                var referenceDate = new DateTime(targetYear, targetMonth, 1).AddMonths(1).AddTicks(-1);

                // Carregar dados filtrados por estado e município
                // Carregar escolas e filtrar em memória com Helpers
                var allSchools = await _appDbContext.Schools.ToListAsync();
                var schools = allSchools
                    .Where(s => Helpers.StateMatches(s.State, cleanState) && Helpers.CityMatches(s.City, cleanCity))
                    .ToList();

                // Carregar alunos ativos até a data de referência e filtrar em memória por município/estado
                var allActiveStudents = await _appDbContext.Students
                    .Include(s => s.School)
                    .Where(s => s.IsActive && s.CreatedAt <= referenceDate)
                    .ToListAsync();

                var students = allActiveStudents
                    .Where(s => Helpers.StateMatches(s.School != null ? s.School.State : s.State, cleanState) &&
                                Helpers.CityMatches(s.School != null ? s.School.City : s.City, cleanCity))
                    .ToList();

                var studentIds = students.Select(s => s.ID).ToHashSet();

                var reports = await _appDbContext.Reports
                    .AsSplitQuery()
                    .Include(s => s.Student).ThenInclude(s => s!.School)
                    .Include(r => r.SocialWorkerReport)
                    .Include(r => r.PsychologistReport).ThenInclude(pr => pr!.PsychologistQuestionnaire)
                    .Include(r => r.PsychopedagogueReport).ThenInclude(p => p!.PsychopedagogueQuestionnaire).ThenInclude(q => q.TypeOfDisability)
                    .Include(r => r.PsychopedagogueReport).ThenInclude(p => p!.PsychopedagogueQuestionnaire).ThenInclude(q => q.TypeOfDisorder)
                    .Where(r => r.StudentID != null && studentIds.Contains(r.StudentID.Value) && r.CreatedAt <= referenceDate)
                    .ToListAsync();

                var allPaees = await _appDbContext.PAEEs
                    .Include(s => s.Student).ThenInclude(s => s!.School)
                    .Where(p => p.StudentID != null && studentIds.Contains(p.StudentID.Value))
                    .ToListAsync();
                var paees = allPaees
                    .Where(p => (p.SignedAt ?? p.UnifiedAt ?? p.UpdatedAt ?? p.CreatedAt) <= referenceDate)
                    .ToList();

                var allPeis = await _appDbContext.PEIs
                    .Include(s => s.Student).ThenInclude(s => s!.School)
                    .Where(p => p.StudentID != null && studentIds.Contains(p.StudentID.Value))
                    .ToListAsync();
                var peis = allPeis
                    .Where(p => (p.SignedAt ?? p.UnifiedAt ?? p.UpdatedAt ?? p.CreatedAt) <= referenceDate)
                    .ToList();

                var regularTeacherCaseStudies = await _appDbContext.RegularTeacherEvaluationDocuments
                    .Include(s => s.Student).ThenInclude(s => s!.School)
                    .Where(cs => cs.StudentID != null && studentIds.Contains(cs.StudentID.Value) && cs.CreatedAt <= referenceDate)
                    .ToListAsync();

                // Questionários e avaliações dos alunos para detecção de deficiência e transtorno
                var allQuestionnaires = await _appDbContext.PsychopedagogueQuestionnaires
                    .Include(q => q.TypeOfDisability)
                    .Include(q => q.TypeOfDisorder)
                    .Where(q => q.StudentID != null && studentIds.Contains(q.StudentID.Value) && q.CreatedAt <= referenceDate)
                    .ToListAsync();

                var allSnapQuestionnaires = await _appDbContext.SnapIVQuestionnaires
                    .Where(q => q.StudentID != null && studentIds.Contains(q.StudentID.Value) && q.CreatedAt <= referenceDate)
                    .ToListAsync();

                var allAdaptiveScales = await _appDbContext.AdaptiveBehaviorScalesDI
                    .Include(s => s.PsychologistReport)
                    .Where(s => s.PsychologistReport != null && s.PsychologistReport.StudentID != null &&
                                studentIds.Contains(s.PsychologistReport.StudentID.Value) && s.CreatedAt <= referenceDate)
                    .ToListAsync();

                var allCognitiveAssessments = await _appDbContext.CognitiveAssessments
                    .Include(c => c.PsychopedagogueReport)
                    .Where(c => c.PsychopedagogueReport != null && c.PsychopedagogueReport.StudentID != null &&
                                studentIds.Contains(c.PsychopedagogueReport.StudentID.Value) && c.CreatedAt <= referenceDate)
                    .ToListAsync();

                var allCars = await _appDbContext.CARSQuestionnaires
                    .Include(c => c.PsychopedagogueReport)
                    .Where(c => c.PsychopedagogueReport != null && c.PsychopedagogueReport.StudentID != null &&
                                studentIds.Contains(c.PsychopedagogueReport.StudentID.Value) && c.CreatedAt <= referenceDate)
                    .ToListAsync();

                var allMchat = await _appDbContext.MCHATQuestionnaires
                    .Include(m => m.PsychopedagogueReport)
                    .Where(m => m.PsychopedagogueReport != null && m.PsychopedagogueReport.StudentID != null &&
                                studentIds.Contains(m.PsychopedagogueReport.StudentID.Value) && m.CreatedAt <= referenceDate)
                    .ToListAsync();

                var adaptiveByStudent = allAdaptiveScales
                    .Where(s => s.PsychologistReport?.StudentID != null)
                    .GroupBy(s => s.PsychologistReport!.StudentID!.Value)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CreatedAt).First());

                var cognitiveByStudent = allCognitiveAssessments
                    .Where(c => c.PsychopedagogueReport?.StudentID != null)
                    .GroupBy(c => c.PsychopedagogueReport!.StudentID!.Value)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CreatedAt).First());

                var carsByStudent = allCars
                    .Where(c => c.PsychopedagogueReport?.StudentID != null)
                    .GroupBy(c => c.PsychopedagogueReport!.StudentID!.Value)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CreatedAt).First());

                var mchatByStudent = allMchat
                    .Where(m => m.PsychopedagogueReport?.StudentID != null)
                    .GroupBy(m => m.PsychopedagogueReport!.StudentID!.Value)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CreatedAt).First());

                bool IsDIPositive(Guid sId)
                {
                    if (!adaptiveByStudent.TryGetValue(sId, out var adap) || adap == null) return false;
                    if (!cognitiveByStudent.TryGetValue(sId, out var cog) || cog == null) return false;

                    bool adapPositive = adap.Score <= 11 || adap.Result == ScaleDIResult.SevereDevelopmentalDelay;
                    bool cogPositive = (cog.Percentage > 0 && cog.Percentage < 55) || cog.Result == CognitiveResult.ModerateToSevereIntellectualDisability;

                    return adapPositive && cogPositive;
                }

                bool IsTEAPositive(Guid sId)
                {
                    if (carsByStudent.TryGetValue(sId, out var cars) && cars != null)
                    {
                        if (cars.Result != CARSResult.NoAutism && cars.Result != CARSResult.None) return true;
                    }
                    if (mchatByStudent.TryGetValue(sId, out var mchat) && mchat != null)
                    {
                        if (mchat.Result != MCHATResult.LowRisk && mchat.Result != MCHATResult.None) return true;
                    }
                    return false;
                }

                var questionnairesByStudent = allQuestionnaires
                    .GroupBy(q => q.StudentID!.Value)
                    .ToDictionary(g => g.Key, g => g.ToList());

                var latestQuestionnaireByStudent = allQuestionnaires
                    .GroupBy(q => q.StudentID!.Value)
                    .ToDictionary(
                        g => g.Key,
                        g => g.OrderByDescending(q => q.CreatedAt).First()
                    );

                var snapQuestionnairesByStudent = allSnapQuestionnaires
                    .GroupBy(q => q.StudentID!.Value)
                    .ToDictionary(g => g.Key, g => g.ToList());

                // Identificar alunos com deficiência (AEE)
                var studentsWithDisabilityIds = new HashSet<Guid>();
                foreach (var student in students)
                {
                    bool hasDisability = false;
                    if (questionnairesByStudent.TryGetValue(student.ID, out var qs))
                    {
                        hasDisability = qs.Any(q => q.TypeOfDisability?.Any(d => {
                            if (d.Disability == TypeOfDisability.None) return false;
                            if (d.Disability == TypeOfDisability.Intellectual || (int)d.Disability == 1)
                            {
                                return IsDIPositive(student.ID);
                            }
                            return true;
                        }) == true);
                    }

                    if (!hasDisability && IsDIPositive(student.ID)) hasDisability = true;
                    if (!hasDisability && IsTEAPositive(student.ID)) hasDisability = true;

                    if (hasDisability)
                        studentsWithDisabilityIds.Add(student.ID);
                }

                // Identificar alunos com transtornos
                var studentsWithDisorderOnlyIds = new HashSet<Guid>();
                var studentsWithDisorderAndDisabilityIds = new HashSet<Guid>();
                foreach (var student in students)
                {
                    bool hasDisorder = false;
                    if (questionnairesByStudent.TryGetValue(student.ID, out var qs))
                    {
                        hasDisorder = qs.Any(q => q.TypeOfDisorder?.Any(d => d.Disorder != TypeOfDisorder.None) == true);
                    }
                    if (!hasDisorder && snapQuestionnairesByStudent.TryGetValue(student.ID, out var sqs))
                    {
                        hasDisorder = sqs.Any(q => q.Result != SnapIVResult.None);
                    }

                    if (hasDisorder)
                    {
                        if (studentsWithDisabilityIds.Contains(student.ID))
                            studentsWithDisorderAndDisabilityIds.Add(student.ID);
                        else
                            studentsWithDisorderOnlyIds.Add(student.ID);
                    }
                }

                // Estudos de caso
                var caseStudyCount = regularTeacherCaseStudies.Count;
                var caseStudyCompletedCount = regularTeacherCaseStudies.Count(cs => cs.DocumentStatus == Status.Complete || cs.TeacherSign);
                var caseStudyInProgressCount = Math.Max(0, caseStudyCount - caseStudyCompletedCount);

                // PAEE e PEI
                var paeesCompletedCount = paees.Count(p => p.DocumentStatus == Status.Complete || p.TeacherSign);
                var peisCompletedCount = peis.Count(p => p.DocumentStatus == Status.Complete || p.TeacherSign);

                // Relatórios biopsicossociais
                static bool IsSignedHistorical(bool signFlag, DateTime? signedAt, DateTime refDate) =>
                    signFlag && signedAt != null && signedAt.Value <= refDate;

                bool IsReportCompleted(Report r, DateTime refDate) =>
                    (r.DocumentStatus == Status.Complete && (r.UpdatedAt ?? r.CreatedAt) <= refDate) ||
                    (IsSignedHistorical(r.PsychologistSign, r.PsychologistSignedAt, refDate) &&
                     IsSignedHistorical(r.PsychopedagogueSign, r.PsychopedagogueSignedAt, refDate) &&
                     IsSignedHistorical(r.SocialWorkerSign, r.SocialWorkerSignedAt, refDate));

                bool IsReportUnified(Report r, DateTime refDate) =>
                    (r.ReportStatus == Status.Complete && (r.UpdatedAt ?? r.CreatedAt) <= refDate) ||
                    (r.UnifiedAt != null && r.UnifiedAt.Value <= refDate);

                var reportsCompletedCount = reports.Count(r => IsReportCompleted(r, referenceDate) || r.SignatureBlocked);
                var reportsPendingSignatureCount = reports.Count(r => IsReportUnified(r, referenceDate) && !IsReportCompleted(r, referenceDate) && !r.SignatureBlocked);
                var reportsInProgressCount = Math.Max(0, reports.Count - reportsCompletedCount - reportsPendingSignatureCount);

                // Contagem por Tipo de Deficiência
                var disabilityCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                {
                    { "Deficiência Física", 0 },
                    { "Deficiência Intelectual", 0 },
                    { "Visual", 0 },
                    { "Auditiva", 0 },
                    { "Deficiência Múltipla", 0 },
                    { "Surdocegueira", 0 },
                    { "TEA", 0 },
                    { "AH/SD", 0 }
                };

                foreach (var student in students)
                {
                    List<TypeOfDisability>? disabilities = null;

                    if (latestQuestionnaireByStudent.TryGetValue(student.ID, out var latestQ))
                    {
                        disabilities = latestQ.TypeOfDisability?
                            .Where(d => d.Disability != TypeOfDisability.None)
                            .Where(d => {
                                if (d.Disability == TypeOfDisability.Intellectual || (int)d.Disability == 1)
                                {
                                    return IsDIPositive(student.ID);
                                }
                                return true;
                            })
                            .Select(d => d.Disability)
                            .ToList();
                    }

                    if (disabilities == null) disabilities = new List<TypeOfDisability>();

                    if (!disabilities.Contains(TypeOfDisability.Intellectual) && IsDIPositive(student.ID))
                        disabilities.Add(TypeOfDisability.Intellectual);

                    if (!disabilities.Contains(TypeOfDisability.TEA) && IsTEAPositive(student.ID))
                        disabilities.Add(TypeOfDisability.TEA);

                    if (disabilities.Count == 0) continue;

                    if (disabilities.Count > 1)
                    {
                        disabilityCounts["Deficiência Múltipla"]++;
                    }
                    else
                    {
                        var single = disabilities[0];
                        switch (single)
                        {
                            case TypeOfDisability.TEA:
                                disabilityCounts["TEA"]++;
                                break;
                            case TypeOfDisability.Intellectual:
                                disabilityCounts["Deficiência Intelectual"]++;
                                break;
                            case TypeOfDisability.Physical:
                                disabilityCounts["Deficiência Física"]++;
                                break;
                            case TypeOfDisability.Deafness:
                                disabilityCounts["Auditiva"]++;
                                break;
                            case TypeOfDisability.LowVision:
                                disabilityCounts["Visual"]++;
                                break;
                            case TypeOfDisability.Multiple:
                                disabilityCounts["Deficiência Múltipla"]++;
                                break;
                        }
                    }
                }

                // Contagem por Tipo de Transtorno
                var disorderCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                {
                    { "Dislexia", 0 },
                    { "Apraxia de fala", 0 },
                    { "Discalculia", 0 },
                    { "TDAH", 0 },
                    { "Disortografia", 0 },
                    { "Disgrafia", 0 }
                };

                foreach (var student in students)
                {
                    var studentDisorders = new HashSet<TypeOfDisorder>();

                    if (questionnairesByStudent.TryGetValue(student.ID, out var qs))
                    {
                        foreach (var q in qs)
                        {
                            if (q.TypeOfDisorder != null)
                            {
                                foreach (var d in q.TypeOfDisorder)
                                {
                                    if (d.Disorder != TypeOfDisorder.None)
                                        studentDisorders.Add(d.Disorder);
                                }
                            }
                        }
                    }

                    if (snapQuestionnairesByStudent.TryGetValue(student.ID, out var sqs))
                    {
                        if (sqs.Any(q => q.Result != SnapIVResult.None))
                            studentDisorders.Add(TypeOfDisorder.ADHD);
                    }

                    foreach (var disorder in studentDisorders)
                    {
                        switch (disorder)
                        {
                            case TypeOfDisorder.Dyslexia:
                                disorderCounts["Dislexia"]++;
                                break;
                            case TypeOfDisorder.SpeechApraxia:
                                disorderCounts["Apraxia de fala"]++;
                                break;
                            case TypeOfDisorder.Dyscalculia:
                                disorderCounts["Discalculia"]++;
                                break;
                            case TypeOfDisorder.ADHD:
                                disorderCounts["TDAH"]++;
                                break;
                            case TypeOfDisorder.Dysorthographia:
                                disorderCounts["Disortografia"]++;
                                break;
                            case TypeOfDisorder.Dysgraphia:
                                disorderCounts["Disgrafia"]++;
                                break;
                        }
                    }
                }

                var response = new ActivitiesSummaryIntegrationResponse
                {
                    State = cleanState,
                    City = cleanCity,
                    Year = targetYear,
                    Month = targetMonth,
                    TeacherScreening = caseStudyCount,
                    CaseStudiesInProgress = caseStudyInProgressCount,
                    CaseStudiesCompleted = caseStudyCompletedCount,
                    Paee = paeesCompletedCount,
                    Pei = peisCompletedCount,
                    StudentsWithDisabilitiesAee = studentsWithDisabilityIds.Count,
                    DisordersWithDisability = studentsWithDisorderAndDisabilityIds.Count,
                    DisordersWithoutDisability = studentsWithDisorderOnlyIds.Count,
                    ReportsInProgress = reportsInProgressCount,
                    ReportsCompleted = reportsCompletedCount,
                    TotalStudentsWithDisorders = studentsWithDisorderAndDisabilityIds.Count + studentsWithDisorderOnlyIds.Count,
                    TotalStudentsWithDisabilities = studentsWithDisabilityIds.Count,
                    DisabilityTypes = disabilityCounts.Select(kv => new ItemCountDto { Type = kv.Key, Count = kv.Value }).ToList(),
                    DisorderTypes = disorderCounts.Select(kv => new ItemCountDto { Type = kv.Key, Count = kv.Value }).ToList()
                };

                return Ok(new ApiResponse<ActivitiesSummaryIntegrationResponse>
                {
                    Success = true,
                    Message = "Resumo de atividades municipais retornado com sucesso.",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter resumo de atividades para integração: {Message}", ex.Message);
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Erro ao obter dados de integração: {ex.Message}"
                });
            }
        }
    }
}
