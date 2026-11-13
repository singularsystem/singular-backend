using SingularSystem.DTOs.SocialWorkerReports;
using SingularSystem.Interfaces;
using SingularSystem.Mappers;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;
using SingularSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SingularSystem.Authorization;

namespace SingularSystem.Controllers
{
    [Authorize]
    [NoSecretary]
    [Route("api/v1/social-worker-reports")]
    [ApiController]
    public class SocialWorkerReportsController : BaseController
    {
        private readonly ILogger<SocialWorkerReportsController> _logger;
        private readonly AppDbContext _appDbContext;
        private readonly NotificationService _notificationService;
        private readonly IUserSummaryFactory _userSummaryFactory;
        private readonly UserManager<ApplicationUser> _userManager;

        public SocialWorkerReportsController(ILogger<SocialWorkerReportsController> logger, AppDbContext appDbContext, NotificationService notificationService, IUserSummaryFactory userSummaryFactory, UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _appDbContext = appDbContext;
            _notificationService = notificationService;
            _userSummaryFactory = userSummaryFactory;
            _userManager = userManager;
        }

        #region CREATE
        [Authorize(Roles = "Admin,SocialWork")]
        [HttpPost]
        public async Task<IActionResult> CreateSocialWorkerReport([FromBody] SocialWorkerReportRequest model)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem();
            }

            using var transaction = await _appDbContext.Database.BeginTransactionAsync();

            try
            {
                var studentExists = await _appDbContext.Students.AnyAsync(x => x.ID == model.StudentID);

                if (!studentExists)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Aluno não encontrado"
                    });
                }

                var report = await _appDbContext.Reports.Include(s => s.Student).Include(s => s.Student!.School)
                                                        .Include(r => r.SocialWorkerReport)
                                                        .Include(r => r.PsychologistReport).ThenInclude(pr => pr!.AdaptiveBehaviorScaleDI)
                                                        .Include(r => r.PsychologistReport).ThenInclude(pr => pr!.SnapIVQuestionnaire)
                                                        .Include(r => r.PsychologistReport).ThenInclude(pr => pr!.PsychologistQuestionnaire)
                                                        .Include(r => r.PsychopedagogueReport).ThenInclude(pp => pp!.PsychopedagogueQuestionnaire).ThenInclude(pq => pq.TypeOfDisability)
                                                        .Include(r => r.PsychopedagogueReport).ThenInclude(pp => pp!.CARSQuestionnaire)
                                                        .Include(r => r.PsychopedagogueReport).ThenInclude(pp => pp!.MCHATQuestionnaire)
                                                        .Include(r => r.PsychopedagogueReport).ThenInclude(pp => pp!.CognitiveAssessment)
                                                        .FirstOrDefaultAsync(temp => temp.ID == model.ReportID);



                if (report == null)
                {
                    return BadRequest(new ApiResponse<object>()
                    {
                        Success = false,
                        Message = "É necessário ter um relatório vinculado."
                    });
                }

                if (report.SocialWorkerReport != null)
                {
                    return BadRequest(new ApiResponse<object>()
                    {
                        Success = false,
                        Message = "Já existe um relatório de assistente social para este relatório principal."
                    });
                }

                var socialWorkerReport = SocialWorkerReportMapper.ToEntity(model, CurrentUserId);
                await _appDbContext.SocialWorkerReports.AddAsync(socialWorkerReport);
                await _appDbContext.SaveChangesAsync();

                report.SocialWorkerReportID = socialWorkerReport.ID;

                
                if (report != null && report.IsReadyForUnification())
                {
                    report.UnifyReport();

                    await _notificationService.CreateNotificationAdmins(
                        type: TypeNotification.System,
                        title: "Novo relatório unificado finalizado!",
                        message: "Por favor, valide o relatório.",
                        actionURL: $"api/v1/social-worker-reports/{report.ID}"
                    );
                }

                await _appDbContext.Logs.AddAsync(new Log
                {
                    LogLevel = Models.Enums.LogLevel.Info.ToString(),
                    Message = $"Relatório do(a) assistente social criado com sucesso com ID {socialWorkerReport.ID}",
                    Source = nameof(SocialWorkerReportsController),
                    UserID = CurrentUserGuid,
                    EntityType = nameof(SocialWorkerReport),
                    EntityID = socialWorkerReport.ID,
                    Details = "",
                    Action = "Create"
                });

                await _appDbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return StatusCode(201, new ApiResponse<object>()
                {
                    Success = true,
                    Message = "Relatório do(a) assistente social criado com sucesso",
                    Data = new
                    {
                        socialWorkerReport.ID
                    }
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Erro ao criar relatório");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Ocorreu um erro ao criar relatório"
                });
            }
        }
        #endregion

        #region GET
        [HttpGet]
        public async Task<IActionResult> GetSocialWorkerReports([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchTerm = "")
        {
            try
            {
                const int maxPageSize = 50;
                pageSize = Math.Clamp(pageSize, 1, maxPageSize);
                page = Math.Max(page, 1);
                var skip = (page - 1) * pageSize;

                var currentUser = await _userManager.FindByIdAsync(CurrentUserId);
                if (currentUser == null) return Unauthorized();

                var roles = await _userManager.GetRolesAsync(currentUser);
                var isAdmin = roles.Contains("Admin");

                var query = _appDbContext.SocialWorkerReports.IgnoreQueryFilters()
                                         .Include(u => u.User)
                                         .Include(s => s.Student).Include(st => st.Student!.School)
                                         .Include(f => f.FamilyIncomeSource)
                                         .Include(g => g.ReceivedGovernmentBenefits)
                                         .Include(h => h.HealthConditions)
                                         .Include(m => m.MainSchoolDifficulties)
                                         .Include(p => p.ProceduresUsed)
                                         .Include(r => r.Recommendations)
                                         .AsNoTracking()
                                         .WhereStudentInTenant(TenantContext);

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var term = searchTerm.Trim().ToLower();
                    query = query.Where(s => s.Student != null &&
                        s.Student.FullName.ToLower().Contains(term));
                }

                if (TenantContext?.IsPrivatePlan != true && !isAdmin)
                {
                    query = query.Where(s => s.Student != null &&
                                             ((s.Student.City == currentUser.City && s.Student.State == currentUser.State) ||
                                              (s.Student.School != null && s.Student.School.City == currentUser.City && s.Student.School.State == currentUser.State)));
                }

                var totalReports = await query.CountAsync();
                var uniqueTotal = await query
                    .Select(d => d.StudentID)
                    .Distinct()
                    .CountAsync();

                var reports = await query.OrderByDescending(f => f.CreatedAt)
                                         .Skip(skip)
                                         .Take(pageSize)
                                         .ToListAsync();

                if (reports == null || reports.Count == 0)
                {
                    return Ok(new ApiResponse<List<SocialWorkerReportResponse>>()
                    {
                        Success = true,
                        Message = "Nenhuma relatório encontrado",
                        Data = new List<SocialWorkerReportResponse>(),
                        Total = totalReports,
                        UniqueTotal = uniqueTotal
                    });
                }

                var users = reports.Where(q => q.User != null)
                                          .Select(q => q.User!)
                                          .DistinctBy(u => u.Id)
                                          .ToList();

                var userSummaries = await _userSummaryFactory.CreateManyAsync(users);

                var response = reports.Select(q =>
                    SocialWorkerReportMapper.ToResponse(
                        q,
                        q.User != null && userSummaries.TryGetValue(q.User.Id, out var us)
                            ? us
                            : null
                    )
                ).ToList();

                return Ok(new ApiResponse<List<SocialWorkerReportResponse>>()
                {
                    Success = true,
                    Message = "Lista de relatórios do assistente social retornada com sucesso",
                    Data = response,
                    Total = totalReports,
                    UniqueTotal = uniqueTotal
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao retornar relatórios");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Ocorreu um erro ao retornar relatórios."
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetSocialWorkerReport(Guid id)
        {
            try
            {

                var report = await _appDbContext.SocialWorkerReports
                                         .Include(s => s.Student).ThenInclude(st => st!.School)
                                         .Include(u => u.User)
                                         .Include(f => f.FamilyIncomeSource)
                                         .Include(g => g.ReceivedGovernmentBenefits)
                                         .Include(h => h.HealthConditions)
                                         .Include(m => m.MainSchoolDifficulties)
                                         .Include(p => p.ProceduresUsed)
                                         .Include(r => r.Recommendations)
                                         .AsNoTracking()
                                         .FirstOrDefaultAsync(s => s.ID == id);

                if (report == null)
                {
                    _logger.LogWarning(
                        "Usuário {UserId} tentou acessar relatório {ReportId} não encontrado",
                        CurrentUserId,
                        id
                    );
                    return NotFound(new ApiResponse<object>()
                    {
                        Success = false,
                        Message = "Relatório não encontrado"
                    });
                }

                var user = await _userManager.FindByIdAsync(CurrentUserId);
                if (user == null) return Unauthorized();

                var roles = await _userManager.GetRolesAsync(user);

                if (report.Student != null && !HasStudentAccess(user, report.Student, roles))
                {
                    return StatusCode(403, new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Sem permissão para acessar este relatório."
                    });
                }

                var userSummary = await _userSummaryFactory.CreateAsync(report.User);
                var response = SocialWorkerReportMapper.ToResponse(report, userSummary);

                return Ok(new ApiResponse<SocialWorkerReportResponse>()
                {
                    Success = true,
                    Message = "Relatório do assistente social retornado com sucesso",
                    Data = response,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao retornar relatório {ReportId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Ocorreu um erro ao retornar relatório"
                });
            }
        }
        #endregion

        #region UPDATE
        [Authorize(Roles = "Admin,SocialWork")]
        [HttpPut]
        public async Task<IActionResult> UpdateSocialWorkerReport([FromBody] SocialWorkerReportUpdateRequest model)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem();
            }

            using var transaction = await _appDbContext.Database.BeginTransactionAsync();

            try
            {
                var report = await _appDbContext.SocialWorkerReports
                                                .Include(s => s.Student).Include(s => s.Student!.School)
                                                .Include(f => f.FamilyIncomeSource)
                                                .Include(g => g.ReceivedGovernmentBenefits)
                                                .Include(h => h.HealthConditions)
                                                .Include(m => m.MainSchoolDifficulties)
                                                .Include(p => p.ProceduresUsed)
                                                .Include(r => r.Recommendations)
                                                .FirstOrDefaultAsync(s => s.ID == model.ID);

                if (report == null)
                {
                    _logger.LogWarning(
                        "Usuário {UserId} tentou acessar relatório {ReportId} não encontrado",
                        CurrentUserId,
                        model.ID
                    );
                    return NotFound(new ApiResponse<object>()
                    {
                        Success = false,
                        Message = "Relatório do assistente social não encontrado"
                    });
                }

                if (User.IsInRole("SocialWork") && !string.Equals(report.UserID, CurrentUserId, StringComparison.Ordinal))
                {
                    return Forbid();
                }

                _appDbContext.SocialWorkerReport_FamilyIncomeSources.RemoveRange(report.FamilyIncomeSource);
                _appDbContext.SocialWorkerReport_GovernmentBenefits.RemoveRange(report.ReceivedGovernmentBenefits);
                _appDbContext.SocialWorkerReport_HealthConditions.RemoveRange(report.HealthConditions);
                _appDbContext.SocialWorkerReport_MainSchoolDifficulties.RemoveRange(report.MainSchoolDifficulties);
                _appDbContext.SocialWorkerReport_Procedures.RemoveRange(report.ProceduresUsed);
                _appDbContext.SocialWorkerReport_Recommendations.RemoveRange(report.Recommendations);
                await _appDbContext.SaveChangesAsync();

                SocialWorkerReportMapper.UpdateEntity(report, model);

                await _appDbContext.Logs.AddAsync(new Log
                {
                    LogLevel = Models.Enums.LogLevel.Info.ToString(),
                    Message = $"Relatório do assistente social atualizado com sucesso com ID {report.ID}",
                    Source = nameof(SocialWorkerReportsController),
                    UserID = CurrentUserGuid,
                    EntityType = nameof(SocialWorkerReport),
                    EntityID = report.ID,
                    Details = "",
                    Action = "Update"
                });

                await _appDbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new ApiResponse<object>()
                {
                    Success = true,
                    Message = "Relatório do assistente social atualizado com sucesso",
                    Data = new
                    {
                        report.ID,
                    }
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Erro ao atualizar relatório com ID {Id}", model.ID);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Ocorreu um erro interno ao atualizar o relatório"
                });
            }
        }
        #endregion

        #region DELETE
        [Authorize(Roles = "Admin,SocialWork")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSocialWorkerReport(Guid id)
        {
            try
            {
                var report = await _appDbContext.SocialWorkerReports.FirstOrDefaultAsync(temp => temp.ID == id);

                if (report == null)
                {
                    _logger.LogWarning(
                        "Usuário {UserId} tentou acessar relatório {ReportId} não encontrado",
                        CurrentUserId,
                        id
                    );
                    return NotFound(new ApiResponse<object>()
                    {
                        Success = false,
                        Message = "Relatório do assistente social não encontrado"
                    });
                }

                if (User.IsInRole("SocialWork") && !string.Equals(report.UserID, CurrentUserId, StringComparison.Ordinal))
                {
                    return Forbid();
                }

                _appDbContext.SocialWorkerReports.Remove(report);

                await _appDbContext.Logs.AddAsync(new Log
                {
                    LogLevel = Models.Enums.LogLevel.Info.ToString(),
                    Message = $"Relatório do assistente social excluído com sucesso com ID {report.ID}",
                    Source = nameof(SocialWorkerReportsController),
                    UserID = CurrentUserGuid,
                    EntityType = nameof(SocialWorkerReport),
                    EntityID = report.ID,
                    Details = "",
                    Action = "Delete"
                });

                await _appDbContext.SaveChangesAsync();

                return Ok(new ApiResponse<object>()
                {
                    Success = true,
                    Message = "Relatório do assistente social excluído com sucesso"
                });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao excluir relatório com id {id}");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Ocorreu um erro ao excluir o relatório com id {id}"
                });
            }
        }

        #endregion
    }
}
