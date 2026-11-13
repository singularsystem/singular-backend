using SingularSystem.DTOs.PsychologistReports.AdaptiveBehaviorScaleDI;
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
using System.Security.Claims;
using SingularSystem.Authorization;

namespace SingularSystem.Controllers
{
    [Authorize]
    [NoSecretary]
    [PlanFeatureAuthorize("Empresa", "Publico")]
    [Route("api/v1/adaptative-scale-di")]
    [ApiController]
    public class AdaptiveBehaviorScaleDIController : BaseController
    {
        private readonly ILogger<AdaptiveBehaviorScaleDIController> _logger;
        private readonly AppDbContext _appDbContext;
        private readonly NotificationService _notificationService;
        private readonly IUserSummaryFactory _userSummaryFactory;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdaptiveBehaviorScaleDIController(ILogger<AdaptiveBehaviorScaleDIController> logger, AppDbContext appDbContext, NotificationService notificationService, IUserSummaryFactory userSummaryFactory, UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _appDbContext = appDbContext;
            _notificationService = notificationService;
            _userSummaryFactory = userSummaryFactory;
            _userManager = userManager;
        }

        #region CREATE
        [Authorize(Policy = "AdminOrPsychologist")]
        [HttpPost]
        public async Task<IActionResult> CreateAdaptiveBehaviorScaleDI([FromBody] AdaptiveBehaviorScaleDIRequest model)
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

                var questionnaire = AdaptiveBehaviorScaleDIMapper.ToEntity(model, CurrentUserId);
                await _appDbContext.AdaptiveBehaviorScalesDI.AddAsync(questionnaire);

                await _appDbContext.Logs.AddAsync(new Log
                {
                    LogLevel = Models.Enums.LogLevel.Info.ToString(),
                    Message = $"Escala Adaptativa-DI criada com sucesso com ID {questionnaire.ID}",
                    Source = nameof(AdaptiveBehaviorScaleDIController),
                    UserID = CurrentUserGuid,
                    EntityType = nameof(AdaptiveBehaviorScaleDI),
                    EntityID = questionnaire.ID,
                    Details = "",
                    Action = "Create"
                });

                var psychologistReport = await _appDbContext.PsychologistReports
                                        .Include(p => p.Student)
                                        .Include(p => p.PsychologistQuestionnaire)
                                        .Include(p => p.SnapIVQuestionnaire)
                                        .FirstOrDefaultAsync(temp => temp.ID == questionnaire.PsychologistReportID);

                if (psychologistReport == null)
                {
                    return NotFound(new ApiResponse<object>()
                    {
                        Success = false,
                        Message = $"Relatório com ID {questionnaire.PsychologistReportID} não encontrado"
                    });
                }

                bool hasAdaptiveScale = true; // Acabou de ser salva.

                bool shouldUnify = psychologistReport.PsychologistQuestionnaire != null &&
                    hasAdaptiveScale && psychologistReport.SnapIVQuestionnaire != null;

                if (shouldUnify)
                {
                    psychologistReport.UnifiedAt = DateTime.UtcNow;
                }

                psychologistReport.UserID = CurrentUserId;

                await _appDbContext.SaveChangesAsync();

                var report = await _appDbContext.Reports.Include(s => s.Student).Include(r => r.SocialWorkerReport)
                                                        .Include(r => r.PsychologistReport).ThenInclude(pr => pr!.AdaptiveBehaviorScaleDI)
                                                        .Include(r => r.PsychologistReport).ThenInclude(pr => pr!.SnapIVQuestionnaire)
                                                        .Include(r => r.PsychologistReport).ThenInclude(pr => pr!.PsychologistQuestionnaire)
                                                        .Include(r => r.PsychopedagogueReport).ThenInclude(pp => pp!.PsychopedagogueQuestionnaire).ThenInclude(pq => pq.TypeOfDisability)
                                                        .Include(r => r.PsychopedagogueReport).ThenInclude(pp => pp!.CARSQuestionnaire)
                                                        .Include(r => r.PsychopedagogueReport).ThenInclude(pp => pp!.MCHATQuestionnaire)
                                                        .Include(r => r.PsychopedagogueReport).ThenInclude(pp => pp!.CognitiveAssessment)
                                                        .FirstOrDefaultAsync(r => r.PsychologistReportID == psychologistReport.ID);


                if (report != null && report.IsReadyForUnification())
                {
                    report.UnifyReport();
                }

                await _appDbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                await _notificationService.CreateNotificationAdmins(
                    type: TypeNotification.System,
                    title: "Nova Escala Adaptativa-DI criada!",
                    message: $"Uma nova Escala Adaptativa-DI foi criada.",
                    actionURL: "api/v1/adaptative-scale-di"
                );

                return StatusCode(201, new ApiResponse<object>()
                {
                    Success = true,
                    Message = "Escala Adaptativa-DI criada com sucesso",
                    Data = new
                    {
                        questionnaire.ID
                    }
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Erro ao criar Escala Adaptativa-DI");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Ocorreu um erro ao criar Escala Adaptativa-DI"
                });
            }
        }
        #endregion

        #region GET
        [HttpGet]
        public async Task<IActionResult> GetAdaptiveBehaviorScalesDI([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchTerm = "")
        {
            try
            {
                const int maxPageSize = 50;
                pageSize = Math.Clamp(pageSize, 1, maxPageSize);
                page = Math.Max(page, 1);
                var skip = (page - 1) * pageSize;

                var query = _appDbContext.AdaptiveBehaviorScalesDI.Include(s => s.Student)
                                                                  .Include(s => s.Student!.School)
                                                                  .Include(u => u.User)
                                                                  .AsNoTracking()
                                                                  .WhereStudentInTenant(TenantContext);

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var term = searchTerm.Trim().ToLower();
                    query = query.Where(s => s.Student != null &&
                        s.Student.FullName.ToLower().Contains(term));
                }

                var currentUser = await _userManager.FindByIdAsync(CurrentUserId);
                if (currentUser == null) return Unauthorized();

                var roles = await _userManager.GetRolesAsync(currentUser);
                var isAdmin = roles.Contains("Admin");

                if (TenantContext?.IsPrivatePlan != true && !isAdmin)
                {
                    query = query.Where(s => s.Student != null &&
                                             ((s.Student.City == currentUser.City && s.Student.State == currentUser.State) ||
                                              (s.Student.School != null && s.Student.School.City == currentUser.City && s.Student.School.State == currentUser.State)));
                }

                var totalQuestionnaires = await query.CountAsync();

                var questionnaires = await query.OrderByDescending(f => f.CreatedAt)
                                         .Skip(skip)
                                         .Take(pageSize)
                                         .ToListAsync();

                if (questionnaires == null || questionnaires.Count == 0)
                {
                    return Ok(new ApiResponse<List<AdaptiveBehaviorScaleDIResponse>>()
                    {
                        Success = true,
                        Message = "Nenhuma Escala Adaptativa-DI encontrada",
                        Data = new List<AdaptiveBehaviorScaleDIResponse>(),
                        Total = totalQuestionnaires
                    });
                }

                var users = questionnaires.Where(q => q.User != null)
                                          .Select(q => q.User!)
                                          .DistinctBy(u => u.Id)
                                          .ToList();

                var userSummaries = await _userSummaryFactory.CreateManyAsync(users);

                var response = questionnaires.Select(q =>
                    AdaptiveBehaviorScaleDIMapper.ToResponse(
                        q,
                        q.User != null && userSummaries.TryGetValue(q.User.Id, out var us)
                            ? us
                            : null
                    )
                ).ToList();

                return Ok(new ApiResponse<List<AdaptiveBehaviorScaleDIResponse?>>()
                {
                    Success = true,
                    Message = "Lista de escalas retornada com sucesso",
                    Data = response,
                    Total = totalQuestionnaires
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao retornar escalas");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Ocorreu um erro ao retornar escalas."
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAdaptiveBehaviorScaleDI(Guid id)
        {
            try
            {

                var questionnaire = await _appDbContext.AdaptiveBehaviorScalesDI.Include(s => s.Student)
                                                                                .Include(s => s.Student!.School)
                                                                                .Include(u => u.User)
                                                                                .AsNoTracking()
                                                                                .FirstOrDefaultAsync(s => s.ID == id);

                if (questionnaire == null)
                {
                    _logger.LogWarning(
                        "Usuário {UserId} tentou acessar escala {ScaleId} não encontrada",
                        CurrentUserId,
                        id
                    );
                    return NotFound(new ApiResponse<object>()
                    {
                        Success = false,
                        Message = "Escala Adaptativa-DI não encontrada"
                    });
                }

                var user = await _userManager.FindByIdAsync(CurrentUserId);
                if (user == null) return Unauthorized();

                var roles = await _userManager.GetRolesAsync(user);

                if (questionnaire.Student != null && !HasStudentAccess(user, questionnaire.Student, roles))
                {
                    return Forbid();
                }

                var userSummary = await _userSummaryFactory.CreateAsync(questionnaire.User);
                var response = AdaptiveBehaviorScaleDIMapper.ToResponse(questionnaire, userSummary);

                return Ok(new ApiResponse<AdaptiveBehaviorScaleDIResponse?>()
                {
                    Success = true,
                    Message = "Escala Adaptativa-DI retornada com sucesso",
                    Data = response,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao retornar escala {ScaleId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Ocorreu um erro ao retornar escala"
                });
            }
        }
        #endregion

        #region UPDATE
        [Authorize(Policy = "AdminOrPsychologist")]
        [HttpPut]
        public async Task<IActionResult> UpdateAdaptiveBehaviorScalesDI([FromBody] AdaptiveBehaviorScaleDIUpdateRequest model)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem();
            }

            using var transaction = await _appDbContext.Database.BeginTransactionAsync();

            try
            {
                var questionnaire = await _appDbContext.AdaptiveBehaviorScalesDI.FirstOrDefaultAsync(s => s.ID == model.ID);

                if (questionnaire == null)
                {
                    _logger.LogWarning(
                        "Usuário {UserId} tentou acessar escala {ScaleId} não encontrada",
                        CurrentUserId,
                        model.ID
                    );
                    return NotFound(new ApiResponse<object>()
                    {
                        Success = false,
                        Message = "Escala Adaptativa-DI não encontrada"
                    });
                }

                if (User.IsInRole("Psychologist") && !string.Equals(questionnaire.UserID, CurrentUserId, StringComparison.Ordinal))
                {
                    return Forbid();
                }

                AdaptiveBehaviorScaleDIMapper.UpdateEntity(questionnaire, model);

                await _appDbContext.Logs.AddAsync(new Log
                {
                    LogLevel = Models.Enums.LogLevel.Info.ToString(),
                    Message = $"Escala Adaptativa-DI atualizada com sucesso com ID {questionnaire.ID}",
                    Source = nameof(AdaptiveBehaviorScaleDIController),
                    UserID = CurrentUserGuid,
                    EntityType = nameof(AdaptiveBehaviorScaleDI),
                    EntityID = questionnaire.ID,
                    Details = "",
                    Action = "Update"
                });

                await _appDbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new ApiResponse<object>()
                {
                    Success = true,
                    Message = "Escala Adaptativa-DI atualizada com sucesso",
                    Data = new
                    {
                        questionnaire.ID,
                    }
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Erro ao atualizar escala com ID {Id}", model.ID);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Ocorreu um erro interno ao atualizar a escala"
                });
            }
        }
        #endregion

        #region DELETE
        [Authorize(Policy = "AdminOrPsychologist")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAdaptiveBehaviorScalesDI(Guid id)
        {
            try
            {
                var questionnaire = await _appDbContext.AdaptiveBehaviorScalesDI.FirstOrDefaultAsync(temp => temp.ID == id);

                if (questionnaire == null)
                {
                    _logger.LogWarning(
                        "Usuário {UserId} tentou acessar escala {ScaleId} não encontrada",
                        CurrentUserId,
                        id
                    );
                    return NotFound(new ApiResponse<object>()
                    {
                        Success = false,
                        Message = "Escala Adaptativa-DI não encontrada"
                    });
                }

                if (User.IsInRole("Psychologist") && !string.Equals(questionnaire.UserID, CurrentUserId, StringComparison.Ordinal))
                {
                    return Forbid();
                }

                _appDbContext.AdaptiveBehaviorScalesDI.Remove(questionnaire);

                await _appDbContext.Logs.AddAsync(new Log
                {
                    LogLevel = Models.Enums.LogLevel.Info.ToString(),
                    Message = $"Escala Adaptativa-DI excluída com sucesso com ID {questionnaire.ID}",
                    Source = nameof(AdaptiveBehaviorScaleDIController),
                    UserID = CurrentUserGuid,
                    EntityType = nameof(AdaptiveBehaviorScaleDI),
                    EntityID = questionnaire.ID,
                    Details = "",
                    Action = "Delete"
                });

                await _appDbContext.SaveChangesAsync();

                return Ok(new ApiResponse<object>()
                {
                    Success = true,
                    Message = "Escala Adaptativa-DI excluída com sucesso"
                });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao excluir escala com id {id}");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Ocorreu um erro ao excluir a escala com id {id}"
                });
            }
        }

        #endregion
    }
}
