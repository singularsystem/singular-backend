using SingularSystem.Domain.Tenancy;
using SingularSystem.DTOs.ReportAEE;
using SingularSystem.DTOs.Reports;
using SingularSystem.DTOs.Statistics;
using SingularSystem.DTOs.Students;
using SingularSystem.Extensions;
using SingularSystem.Mappers;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Helpers;
using System.Globalization;
using System.Linq;
using System.Security.Claims;

namespace SingularSystem.Controllers
{
    [Authorize]
    [Route("api/v1/report-aee")]
    [ApiController]
    public class ReportAEEController : BaseController
    {
        private readonly AppDbContext _appDbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ReportAEEController> _logger;
        private readonly ITenantContext _tenantContext;

        public ReportAEEController(AppDbContext appDbContext, ILogger<ReportAEEController> logger, UserManager<ApplicationUser> userManager, ITenantContext tenantContext)
        {
            _appDbContext = appDbContext;
            _userManager = userManager;
            _logger = logger;
            _tenantContext = tenantContext;
        }

        private async Task<IQueryable<Student>> BuildStudentQuery(
            ApplicationUser currentUser,
            string state,
            string city,
            string schoolID,
            string searchTerm)
        {
            var query = _appDbContext.Students
                .Include(s => s.School)
                .AsNoTracking()
                .AsSplitQuery()
                .Where(s => s.IsActive);

            if (_tenantContext.IsPrivatePlan)
            {
                query = query.Where(s => s.TenantId == _tenantContext.TenantId);
                return query;
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(s => s.FullName.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(state))
            {
                var cleanState = state.Trim();
                query = query.Where(s => s.State == cleanState || (s.School != null && s.School.State == cleanState));
            }

            if (!string.IsNullOrWhiteSpace(city))
            {
                var cleanCity = city.Trim();
                query = query.Where(s => s.City == cleanCity || (s.School != null && s.School.City == cleanCity));
            }

            if (!string.IsNullOrWhiteSpace(schoolID) && Guid.TryParse(schoolID, out var schoolGuid))
                query = query.Where(s => s.School != null && s.School.ID == schoolGuid);

            var roles = await _userManager.GetRolesAsync(currentUser);

            if (roles.Contains("MunicipalManager") || roles.Contains("Secretary") || roles.Contains("Coordinator"))
            {
                query = query.Where(s => (s.City == currentUser.City && s.State == currentUser.State) || (s.School != null && s.School.City == currentUser.City && s.School.State == currentUser.State));
            }

            return query;
        }

        private static List<string> SplitCsv(string csv)
        {
            return (csv ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        [HttpGet]
        public async Task<IActionResult> GetReportsAEE(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string state = "",
            [FromQuery] string city = "",
            [FromQuery] string schoolID = "",
            [FromQuery] string disability = "",
            [FromQuery] string disorder = "",
            [FromQuery] string searchTerm = "",
            [FromQuery] int? year = null,
            [FromQuery] int? month = null)
        {
            try
            {
                const int maxPageSize = 50;
                pageSize = Math.Clamp(pageSize, 1, maxPageSize);
                page = Math.Max(page, 1);
                var skip = (page - 1) * pageSize;

                DateTime? maxDate = null;
                if (year.HasValue)
                {
                    if (month.HasValue)
                    {
                        maxDate = new DateTime(year.Value, month.Value, 1).AddMonths(1).AddTicks(-1);
                    }
                    else
                    {
                        maxDate = new DateTime(year.Value, 12, 31, 23, 59, 59);
                    }
                }
                else if (month.HasValue)
                {
                    maxDate = new DateTime(DateTime.Now.Year, month.Value, 1).AddMonths(1).AddTicks(-1);
                }

                var user = await _userManager.FindByIdAsync(CurrentUserId);

                if (user == null)
                {
                    return Unauthorized();
                }

                var baseQuery = await BuildStudentQuery(user, state, city, schoolID, searchTerm);
                var disabilityList = SplitCsv(disability);
                var disorderList = SplitCsv(disorder);

                List<TypeOfDisability> disabilityEnums = new();
                if (disabilityList.Count > 0)
                {
                    foreach (var item in disabilityList)
                    {
                        try
                        {
                            disabilityEnums.Add(
                                Helpers.GetEnumValueFromDisplayName<TypeOfDisability>(item)
                            );
                        }
                        catch
                        {
                            return BadRequest(new ApiResponse<object>
                            {
                                Success = false,
                                Message = $"Deficiência inválida: {item}"
                            });
                        }
                    }
                }

                List<TypeOfDisorder> disorderEnums = new();
                List<SnapIVResult> snapIvEnums = new();
                if (disorderList.Count > 0)
                {
                    foreach (var item in disorderList)
                    {
                        try
                        {
                            if (item.StartsWith("TDAH Tipo ") || item == "TDAH Combinado")
                            {
                                snapIvEnums.Add(Helpers.GetEnumValueFromDisplayName<SnapIVResult>(item));
                            }
                            else
                            {
                                disorderEnums.Add(Helpers.GetEnumValueFromDisplayName<TypeOfDisorder>(item));
                            }
                        }
                        catch
                        {
                            return BadRequest(new ApiResponse<object>
                            {
                                Success = false,
                                Message = $"Transtorno inválido: {item}"
                            });
                        }
                    }
                }

                if (disabilityEnums.Count > 0 || disorderEnums.Count > 0 || snapIvEnums.Count > 0)
                {
                    baseQuery = baseQuery.Where(s =>
                        (disabilityEnums.Count > 0 && (
                            (disabilityEnums.Contains(TypeOfDisability.Intellectual) && 
                             s.BaseDocuments.OfType<AdaptiveBehaviorScaleDI>().Any(a => (!maxDate.HasValue || a.CreatedAt <= maxDate.Value) && (a.Score <= 11 || a.Result == ScaleDIResult.SevereDevelopmentalDelay)) &&
                             s.BaseDocuments.OfType<CognitiveAssessment>().Any(c => (!maxDate.HasValue || c.CreatedAt <= maxDate.Value) && (c.Percentage > 0 && c.Percentage < 55 || c.Result == CognitiveResult.ModerateToSevereIntellectualDisability))) ||
                            
                            (disabilityEnums.Contains(TypeOfDisability.TEA) && (
                                s.BaseDocuments.OfType<CARSQuestionnaire>().Any(cars => (!maxDate.HasValue || cars.CreatedAt <= maxDate.Value) && cars.Result != CARSResult.NoAutism && cars.Result != CARSResult.None) ||
                                s.BaseDocuments.OfType<MCHATQuestionnaire>().Any(mchat => (!maxDate.HasValue || mchat.CreatedAt <= maxDate.Value) && mchat.Result != MCHATResult.LowRisk && mchat.Result != MCHATResult.None)
                            )) ||
                            
                            s.BaseDocuments.OfType<PsychopedagogueQuestionnaire>().Any(q => (!maxDate.HasValue || q.CreatedAt <= maxDate.Value) && 
                                q.TypeOfDisability.Any(td => td.Disability != TypeOfDisability.Intellectual && td.Disability != TypeOfDisability.TEA && disabilityEnums.Contains(td.Disability)))
                        )) ||
                        (disorderEnums.Count > 0 && s.BaseDocuments.OfType<PsychopedagogueQuestionnaire>()
                            .Any(q => (!maxDate.HasValue || q.CreatedAt <= maxDate.Value) && q.TypeOfDisorder.Any(td => disorderEnums.Contains(td.Disorder)))) ||
                        (snapIvEnums.Count > 0 && s.BaseDocuments.OfType<SnapIVQuestionnaire>()
                            .Any(q => (!maxDate.HasValue || q.CreatedAt <= maxDate.Value) && snapIvEnums.Contains(q.Result)))
                    );
                }
                else
                {
                    baseQuery = baseQuery.Where(s =>
                        (s.BaseDocuments.OfType<AdaptiveBehaviorScaleDI>().Any(a => (!maxDate.HasValue || a.CreatedAt <= maxDate.Value) && (a.Score <= 11 || a.Result == ScaleDIResult.SevereDevelopmentalDelay)) &&
                         s.BaseDocuments.OfType<CognitiveAssessment>().Any(c => (!maxDate.HasValue || c.CreatedAt <= maxDate.Value) && (c.Percentage > 0 && c.Percentage < 55 || c.Result == CognitiveResult.ModerateToSevereIntellectualDisability))) ||
                        
                        s.BaseDocuments.OfType<CARSQuestionnaire>().Any(cars => (!maxDate.HasValue || cars.CreatedAt <= maxDate.Value) && cars.Result != CARSResult.NoAutism && cars.Result != CARSResult.None) ||
                        s.BaseDocuments.OfType<MCHATQuestionnaire>().Any(mchat => (!maxDate.HasValue || mchat.CreatedAt <= maxDate.Value) && mchat.Result != MCHATResult.LowRisk && mchat.Result != MCHATResult.None) ||
                        
                        s.BaseDocuments.OfType<PsychopedagogueQuestionnaire>().Any(q => (!maxDate.HasValue || q.CreatedAt <= maxDate.Value) && 
                            (q.TypeOfDisability.Any(td => td.Disability != TypeOfDisability.None && td.Disability != TypeOfDisability.Intellectual && td.Disability != TypeOfDisability.TEA) || 
                             q.TypeOfDisorder.Any(td => td.Disorder != TypeOfDisorder.None))) ||
                        
                        s.BaseDocuments.OfType<SnapIVQuestionnaire>().Any(q => (!maxDate.HasValue || q.CreatedAt <= maxDate.Value) && q.Result != SnapIVResult.None)
                    );
                }

                if (maxDate.HasValue)
                {
                    baseQuery = baseQuery.Where(s => s.CreatedAt <= maxDate.Value);
                }

                var totalReports = await baseQuery.CountAsync();

                var pageStudents = await baseQuery
                    .OrderByDescending(s => s.CreatedAt)
                    .Skip(skip)
                    .Take(pageSize)
                    .Select(s => new
                    {
                        Student = s,
                        LastQuestionnaireId = s.BaseDocuments
                            .OfType<PsychopedagogueQuestionnaire>()
                            .Where(q => !maxDate.HasValue || q.CreatedAt <= maxDate.Value)
                            .OrderByDescending(q => q.CreatedAt)
                            .Select(q => (Guid?)q.ID)
                            .FirstOrDefault(),
                        LastSnapIVId = s.BaseDocuments
                            .OfType<SnapIVQuestionnaire>()
                            .Where(q => !maxDate.HasValue || q.CreatedAt <= maxDate.Value)
                            .OrderByDescending(q => q.CreatedAt)
                            .Select(q => (Guid?)q.ID)
                            .FirstOrDefault()
                    })
                    .ToListAsync();

                var ids = pageStudents
                    .Where(x => x.LastQuestionnaireId.HasValue)
                    .Select(x => x.LastQuestionnaireId!.Value)
                    .ToList();
                
                var snapIds = pageStudents
                    .Where(x => x.LastSnapIVId.HasValue)
                    .Select(x => x.LastSnapIVId!.Value)
                    .ToList();

                var questionnaires = await _appDbContext.PsychopedagogueQuestionnaires
                    .IgnoreQueryFilters()
                    .Where(q => ids.Contains(q.ID))
                    .Include(q => q.PsychopedagogueReport)
                    .Include(q => q.Student).ThenInclude(s => s!.School)
                    .Include(q => q.User)
                    .Include(q => q.TypeOfDisability)
                    .Include(q => q.TypeOfDisorder)
                    .Include(q => q.Recommendations)
                    .AsNoTracking()
                    .ToListAsync();

                var snapQuestionnaires = await _appDbContext.SnapIVQuestionnaires
                    .IgnoreQueryFilters()
                    .Where(q => snapIds.Contains(q.ID))
                    .AsNoTracking()
                    .ToListAsync();

                var qById = questionnaires.ToDictionary(q => q.ID);
                var snapQById = snapQuestionnaires.ToDictionary(q => q.ID);

                var studentIds = pageStudents.Select(x => x.Student.ID).ToList();

                var adaptiveScales = await _appDbContext.AdaptiveBehaviorScalesDI
                    .IgnoreQueryFilters()
                    .Where(s => s.PsychologistReport != null && s.PsychologistReport.StudentID != null && studentIds.Contains(s.PsychologistReport.StudentID.Value))
                    .Include(s => s.PsychologistReport)
                    .AsNoTracking()
                    .ToListAsync();

                var cognitiveAssessments = await _appDbContext.CognitiveAssessments
                    .IgnoreQueryFilters()
                    .Where(c => c.PsychopedagogueReport != null && c.PsychopedagogueReport.StudentID != null && studentIds.Contains(c.PsychopedagogueReport.StudentID.Value))
                    .Include(c => c.PsychopedagogueReport)
                    .AsNoTracking()
                    .ToListAsync();

                var carsQuestionnaires = await _appDbContext.CARSQuestionnaires
                    .IgnoreQueryFilters()
                    .Where(c => c.PsychopedagogueReport != null && c.PsychopedagogueReport.StudentID != null && studentIds.Contains(c.PsychopedagogueReport.StudentID.Value))
                    .Include(c => c.PsychopedagogueReport)
                    .AsNoTracking()
                    .ToListAsync();

                var mchatQuestionnaires = await _appDbContext.MCHATQuestionnaires
                    .IgnoreQueryFilters()
                    .Where(m => m.PsychopedagogueReport != null && m.PsychopedagogueReport.StudentID != null && studentIds.Contains(m.PsychopedagogueReport.StudentID.Value))
                    .Include(m => m.PsychopedagogueReport)
                    .AsNoTracking()
                    .ToListAsync();

                var adaptiveByStudent = adaptiveScales
                    .Where(s => s.PsychologistReport?.StudentID != null)
                    .GroupBy(s => s.PsychologistReport!.StudentID!.Value)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CreatedAt).First());

                var cognitiveByStudent = cognitiveAssessments
                    .Where(c => c.PsychopedagogueReport?.StudentID != null)
                    .GroupBy(c => c.PsychopedagogueReport!.StudentID!.Value)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CreatedAt).First());

                var carsByStudent = carsQuestionnaires
                    .Where(c => c.PsychopedagogueReport?.StudentID != null)
                    .GroupBy(c => c.PsychopedagogueReport!.StudentID!.Value)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CreatedAt).First());

                var mchatByStudent = mchatQuestionnaires
                    .Where(m => m.PsychopedagogueReport?.StudentID != null)
                    .GroupBy(m => m.PsychopedagogueReport!.StudentID!.Value)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CreatedAt).First());

                bool IsDIPositive(Guid studentId)
                {
                    if (!adaptiveByStudent.TryGetValue(studentId, out var adap) || adap == null) return false;
                    if (!cognitiveByStudent.TryGetValue(studentId, out var cog) || cog == null) return false;

                    bool adapPositive = adap.Score <= 11 || adap.Result == ScaleDIResult.SevereDevelopmentalDelay;
                    bool cogPositive = (cog.Percentage > 0 && cog.Percentage < 55) || cog.Result == CognitiveResult.ModerateToSevereIntellectualDisability;

                    return adapPositive && cogPositive;
                }

                bool IsTEAPositive(Guid studentId)
                {
                    if (carsByStudent.TryGetValue(studentId, out var cars) && cars != null)
                    {
                        if (cars.Result != CARSResult.NoAutism && cars.Result != CARSResult.None) return true;
                    }
                    if (mchatByStudent.TryGetValue(studentId, out var mchat) && mchat != null)
                    {
                        if (mchat.Result != MCHATResult.LowRisk && mchat.Result != MCHATResult.None) return true;
                    }
                    return false;
                }

                var response = pageStudents.Select(x =>
                {
                    PsychopedagogueQuestionnaire? lastQ = null;
                    if (x.LastQuestionnaireId.HasValue)
                        qById.TryGetValue(x.LastQuestionnaireId.Value, out lastQ);

                    SnapIVQuestionnaire? lastSnapQ = null;
                    if (x.LastSnapIVId.HasValue)
                        snapQById.TryGetValue(x.LastSnapIVId.Value, out lastSnapQ);

                    var disorders = lastQ?.TypeOfDisorder?
                            .Select(td => td.Disorder.GetDisplayName())
                            .ToList() ?? new List<string>();

                    if (lastSnapQ != null && lastSnapQ.Result != SnapIVResult.None)
                    {
                        disorders.Add(lastSnapQ.Result.GetDisplayName());
                    }

                    var disabilities = lastQ?.TypeOfDisability?
                            .Where(td => {
                                if (td.Disability == TypeOfDisability.Intellectual) return IsDIPositive(x.Student.ID);
                                if (td.Disability == TypeOfDisability.TEA) return IsTEAPositive(x.Student.ID);
                                return td.Disability != TypeOfDisability.None;
                            })
                            .Select(td => td.Disability.GetDisplayName())
                            .ToList()
                            ?? new List<string>();

                    if (!disabilities.Contains(TypeOfDisability.Intellectual.GetDisplayName()) && IsDIPositive(x.Student.ID))
                    {
                        disabilities.Add(TypeOfDisability.Intellectual.GetDisplayName());
                    }

                    if (!disabilities.Contains(TypeOfDisability.TEA.GetDisplayName()) && IsTEAPositive(x.Student.ID))
                    {
                        disabilities.Add(TypeOfDisability.TEA.GetDisplayName());
                    }

                    return new ReportAEEResponse
                    {
                        Student = StudentMapper.ToSummaryResponse(x.Student),
                        Disorders = disorders,
                        Disabilities = disabilities
                    };
                }).ToList();

                return Ok(new ApiResponse<List<ReportAEEResponse>>
                {
                    Success = true,
                    Message = "Relatório geral AEE retornado com sucesso",
                    Data = response,
                    Total = totalReports,
                    UniqueTotal = totalReports
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Erro ao retornar o relatório AEE geral. {ex.Message}"
                });
            }
        }

        [Authorize]
        [HttpGet("all")]
        public async Task<IActionResult> GetAllReportsAEENoPagination(
            [FromQuery] string state = "", 
            [FromQuery] string city = "", 
            [FromQuery] string schoolID = "", 
            [FromQuery] string searchTerm = "",
            [FromQuery] int? year = null,
            [FromQuery] int? month = null)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(CurrentUserId);

                if (user == null)
                {
                    return Unauthorized();
                }

                var query = await BuildStudentQuery(user, state, city, schoolID, searchTerm);

                if (!query.Any())
                {
                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = "Nenhum estudante foi cadastrado ainda"
                    });
                }

                DateTime? maxDate = null;
                if (year.HasValue)
                {
                    if (month.HasValue)
                    {
                        maxDate = new DateTime(year.Value, month.Value, 1).AddMonths(1).AddTicks(-1);
                    }
                    else
                    {
                        maxDate = new DateTime(year.Value, 12, 31, 23, 59, 59);
                    }
                }
                else if (month.HasValue)
                {
                    maxDate = new DateTime(DateTime.Now.Year, month.Value, 1).AddMonths(1).AddTicks(-1);
                }

                // Filtrar somente alunos AEE na listagem geral
                query = query.Where(s =>
                    (s.BaseDocuments.OfType<AdaptiveBehaviorScaleDI>().Any(a => (!maxDate.HasValue || a.CreatedAt <= maxDate.Value) && (a.Score <= 11 || a.Result == ScaleDIResult.SevereDevelopmentalDelay)) &&
                     s.BaseDocuments.OfType<CognitiveAssessment>().Any(c => (!maxDate.HasValue || c.CreatedAt <= maxDate.Value) && (c.Percentage > 0 && c.Percentage < 55 || c.Result == CognitiveResult.ModerateToSevereIntellectualDisability))) ||
                    
                    s.BaseDocuments.OfType<CARSQuestionnaire>().Any(cars => (!maxDate.HasValue || cars.CreatedAt <= maxDate.Value) && cars.Result != CARSResult.NoAutism && cars.Result != CARSResult.None) ||
                    s.BaseDocuments.OfType<MCHATQuestionnaire>().Any(mchat => (!maxDate.HasValue || mchat.CreatedAt <= maxDate.Value) && mchat.Result != MCHATResult.LowRisk && mchat.Result != MCHATResult.None) ||
                    
                    s.BaseDocuments.OfType<PsychopedagogueQuestionnaire>().Any(q => (!maxDate.HasValue || q.CreatedAt <= maxDate.Value) && 
                        (q.TypeOfDisability.Any(td => td.Disability != TypeOfDisability.None && td.Disability != TypeOfDisability.Intellectual && td.Disability != TypeOfDisability.TEA) || 
                         q.TypeOfDisorder.Any(td => td.Disorder != TypeOfDisorder.None))) ||
                    
                    s.BaseDocuments.OfType<SnapIVQuestionnaire>().Any(q => (!maxDate.HasValue || q.CreatedAt <= maxDate.Value) && q.Result != SnapIVResult.None)
                );

                if (maxDate.HasValue)
                {
                    query = query.Where(s => s.CreatedAt <= maxDate.Value);
                }

                var totalReports = await query.CountAsync();
                var students = await query.OrderByDescending(s => s.CreatedAt)
                                          .Select(s => new
                                          {
                                              Student = s,

                                              LastQuestionnaire = s.BaseDocuments
                                                    .OfType<PsychopedagogueQuestionnaire>()
                                                    .Where(q => !maxDate.HasValue || q.CreatedAt <= maxDate.Value)
                                                    .OrderByDescending(q => q.CreatedAt)
                                                    .Select(q => new
                                                    {
                                                        q.ID,
                                                        q.CreatedAt,
                                                        q.TypeOfDisability,
                                                        q.TypeOfDisorder,
                                                        q.Recommendations
                                                    })
                                                    .FirstOrDefault(),
                                              
                                              LastSnapIV = s.BaseDocuments
                                                    .OfType<SnapIVQuestionnaire>()
                                                    .Where(q => !maxDate.HasValue || q.CreatedAt <= maxDate.Value)
                                                    .OrderByDescending(q => q.CreatedAt)
                                                    .Select(q => new
                                                    {
                                                        q.ID,
                                                        q.CreatedAt,
                                                        q.Result
                                                    })
                                                    .FirstOrDefault()
                                          })
                                          .AsNoTracking()
                                          .ToListAsync();

                var studentIds = students.Select(x => x.Student.ID).ToList();

                var adaptiveScales = await _appDbContext.AdaptiveBehaviorScalesDI
                    .IgnoreQueryFilters()
                    .Where(s => s.PsychologistReport != null && s.PsychologistReport.StudentID != null && studentIds.Contains(s.PsychologistReport.StudentID.Value))
                    .Include(s => s.PsychologistReport)
                    .AsNoTracking()
                    .ToListAsync();

                var cognitiveAssessments = await _appDbContext.CognitiveAssessments
                    .IgnoreQueryFilters()
                    .Where(c => c.PsychopedagogueReport != null && c.PsychopedagogueReport.StudentID != null && studentIds.Contains(c.PsychopedagogueReport.StudentID.Value))
                    .Include(c => c.PsychopedagogueReport)
                    .AsNoTracking()
                    .ToListAsync();

                var carsQuestionnaires = await _appDbContext.CARSQuestionnaires
                    .IgnoreQueryFilters()
                    .Where(c => c.PsychopedagogueReport != null && c.PsychopedagogueReport.StudentID != null && studentIds.Contains(c.PsychopedagogueReport.StudentID.Value))
                    .Include(c => c.PsychopedagogueReport)
                    .AsNoTracking()
                    .ToListAsync();

                var mchatQuestionnaires = await _appDbContext.MCHATQuestionnaires
                    .IgnoreQueryFilters()
                    .Where(m => m.PsychopedagogueReport != null && m.PsychopedagogueReport.StudentID != null && studentIds.Contains(m.PsychopedagogueReport.StudentID.Value))
                    .Include(m => m.PsychopedagogueReport)
                    .AsNoTracking()
                    .ToListAsync();

                var adaptiveByStudent = adaptiveScales
                    .Where(s => s.PsychologistReport?.StudentID != null)
                    .GroupBy(s => s.PsychologistReport!.StudentID!.Value)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CreatedAt).First());

                var cognitiveByStudent = cognitiveAssessments
                    .Where(c => c.PsychopedagogueReport?.StudentID != null)
                    .GroupBy(c => c.PsychopedagogueReport!.StudentID!.Value)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CreatedAt).First());

                var carsByStudent = carsQuestionnaires
                    .Where(c => c.PsychopedagogueReport?.StudentID != null)
                    .GroupBy(c => c.PsychopedagogueReport!.StudentID!.Value)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CreatedAt).First());

                var mchatByStudent = mchatQuestionnaires
                    .Where(m => m.PsychopedagogueReport?.StudentID != null)
                    .GroupBy(m => m.PsychopedagogueReport!.StudentID!.Value)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CreatedAt).First());

                bool IsDIPositive(Guid studentId)
                {
                    if (!adaptiveByStudent.TryGetValue(studentId, out var adap) || adap == null) return false;
                    if (!cognitiveByStudent.TryGetValue(studentId, out var cog) || cog == null) return false;

                    bool adapPositive = adap.Score <= 11 || adap.Result == ScaleDIResult.SevereDevelopmentalDelay;
                    bool cogPositive = (cog.Percentage > 0 && cog.Percentage < 55) || cog.Result == CognitiveResult.ModerateToSevereIntellectualDisability;

                    return adapPositive && cogPositive;
                }

                bool IsTEAPositive(Guid studentId)
                {
                    if (carsByStudent.TryGetValue(studentId, out var cars) && cars != null)
                    {
                        if (cars.Result != CARSResult.NoAutism && cars.Result != CARSResult.None) return true;
                    }
                    if (mchatByStudent.TryGetValue(studentId, out var mchat) && mchat != null)
                    {
                        if (mchat.Result != MCHATResult.LowRisk && mchat.Result != MCHATResult.None) return true;
                    }
                    return false;
                }

                var response = students.Select(s =>
                {
                    var disorders = s.LastQuestionnaire?.TypeOfDisorder?
                            .Select(td => td.Disorder.GetDisplayName())
                            .ToList() ?? new List<string>();

                    if (s.LastSnapIV != null && s.LastSnapIV.Result != SnapIVResult.None)
                    {
                        disorders.Add(s.LastSnapIV.Result.GetDisplayName());
                    }

                    var disabilities = s.LastQuestionnaire?.TypeOfDisability?
                            .Where(td => {
                                if (td.Disability == TypeOfDisability.Intellectual) return IsDIPositive(s.Student.ID);
                                if (td.Disability == TypeOfDisability.TEA) return IsTEAPositive(s.Student.ID);
                                return td.Disability != TypeOfDisability.None;
                            })
                            .Select(td => td.Disability.GetDisplayName())
                            .ToList()
                            ?? new List<string>();

                    if (!disabilities.Contains(TypeOfDisability.Intellectual.GetDisplayName()) && IsDIPositive(s.Student.ID))
                    {
                        disabilities.Add(TypeOfDisability.Intellectual.GetDisplayName());
                    }

                    if (!disabilities.Contains(TypeOfDisability.TEA.GetDisplayName()) && IsTEAPositive(s.Student.ID))
                    {
                        disabilities.Add(TypeOfDisability.TEA.GetDisplayName());
                    }

                    return new ReportAEEResponse
                    {
                        Student = StudentMapper.ToSummaryResponse(s.Student),
                        Disorders = disorders,
                        Disabilities = disabilities
                    };
                }).ToList();

                return Ok(new ApiResponse<List<ReportAEEResponse>>()
                {
                    Success = true,
                    Message = "Relatório geral AEE retornado com sucesso",
                    Data = response,
                    Total = totalReports,
                    UniqueTotal = totalReports
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return BadRequest(new ApiResponse<object>()
                {
                    Success = false,
                    Message = $"Erro ao retornar o relatório AEE geral. {ex.Message}"
                });
            }
        }

    }
}
