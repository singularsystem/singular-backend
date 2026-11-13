using SingularSystem.DTOs.PsychologistReports.PsychologistQuestionnaire;
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
    [Route("api/v1/psychologist-questionnaires")]
    [ApiController]
    public class PsychologistQuestionnairesController : BaseController
    {
        private readonly ILogger<PsychologistQuestionnairesController> _logger;
        private readonly AppDbContext _appDbContext;
        private readonly NotificationService _notificationService;
        private readonly IUserSummaryFactory _userSummaryFactory;
        private readonly UserManager<ApplicationUser> _userManager;

        public PsychologistQuestionnairesController(ILogger<PsychologistQuestionnairesController> logger, AppDbContext appDbContext, NotificationService notificationService, IUserSummaryFactory userSummaryFactory, UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _appDbContext = appDbContext;
            _notificationService = notificationService;
            _userSummaryFactory = userSummaryFactory;
            _userManager = userManager;
        }

        #region CREATE
        [Authorize(Roles = "Admin,Psychologist")]
        [HttpPost]
        public async Task<IActionResult> CreatePsychologistQuestionnaire([FromBody] PsychologistQuestionnaireRequest model)
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

                var questionnaire = PsychologistQuestionnaireMapper.ToEntity(model, CurrentUserId);
                await _appDbContext.PsychologistQuestionnaires.AddAsync(questionnaire);

                await _appDbContext.Logs.AddAsync(new Log
                {
                    LogLevel = Models.Enums.LogLevel.Info.ToString(),
                    Message = $"Questionário criado com sucesso com ID {questionnaire.ID}",
                    Source = nameof(PsychologistQuestionnairesController),
                    UserID = CurrentUserGuid,
                    EntityType = nameof(PsychologistQuestionnaire),
                    EntityID = questionnaire.ID,
                    Details = "",
                    Action = "Create"
                });

                var psychologistReport = await _appDbContext.PsychologistReports
                                        .Include(p => p.Student)
                                        .Include(p => p.AdaptiveBehaviorScaleDI)
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

                bool hasAdaptiveScale = psychologistReport.AdaptiveBehaviorScaleDI != null;
                if (psychologistReport.Student != null)
                {
                    int age = DateTime.UtcNow.Year - psychologistReport.Student.DateOfBirth.Year;
                    if (psychologistReport.Student.DateOfBirth.Date > DateTime.UtcNow.Date.AddYears(-age)) age--;
                    if (age < 6) hasAdaptiveScale = true; // Ignorar obrigatoriedade
                }

                bool shouldUnify = hasAdaptiveScale && psychologistReport.SnapIVQuestionnaire != null;

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
                    title: "Novo questionário criado!",
                    message: $"Um novo questionário foi criado.",
                    actionURL: "api/v1/psychologist-questionnaires"
                );

                return StatusCode(201, new ApiResponse<object>()
                {
                    Success = true,
                    Message = "Questionário criado com sucesso",
                    Data = new
                    {
                        questionnaire.ID
                    }
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Erro ao criar questionário");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Ocorreu um erro ao criar questionário"
                });
            }
        }
        #endregion

        #region GET
        [HttpGet]
        public async Task<IActionResult> GetPsychologistQuestionnaires([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchTerm = "")
        {
            try
            {
                const int maxPageSize = 50;
                pageSize = Math.Clamp(pageSize, 1, maxPageSize);
                page = Math.Max(page, 1);
                var skip = (page - 1) * pageSize;

                var query = _appDbContext.PsychologistQuestionnaires.IgnoreQueryFilters()
                                         .Include(s => s.Student).Include(st => st.Student!.School)
                                         .Include(u => u.User).AsNoTracking()
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
                    return Ok(new ApiResponse<List<PsychologistQuestionnaireResponse>>()
                    {
                        Success = true,
                        Message = "Nenhum questionário encontrado",
                        Data = new List<PsychologistQuestionnaireResponse>(),
                        Total = totalQuestionnaires
                    });
                }

                var users = questionnaires.Where(q => q.User != null)
                                          .Select(q => q.User!)
                                          .DistinctBy(u => u.Id)
                                          .ToList();

                var userSummaries = await _userSummaryFactory.CreateManyAsync(users);

                var response = questionnaires.Select(q =>
                    PsychologistQuestionnaireMapper.ToResponse(
                        q,
                        q.User != null && userSummaries.TryGetValue(q.User.Id, out var us)
                            ? us
                            : null
                    )
                ).ToList();

                return Ok(new ApiResponse<List<PsychologistQuestionnaireResponse?>>()
                {
                    Success = true,
                    Message = "Lista de questionários retornada com sucesso",
                    Data = response,
                    Total = totalQuestionnaires
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao retornar questionários");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Ocorreu um erro ao retornar questionários."
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPsychologistQuestionnaire(Guid id)
        {
            try
            {

                var questionnaire = await _appDbContext.PsychologistQuestionnaires.IgnoreQueryFilters()
                                         .Include(s => s.Student).Include(st => st.Student!.School)
                                         .Include(u => u.User).AsNoTracking()
                                         .FirstOrDefaultAsync(s => s.ID == id);

                if (questionnaire == null)
                {
                    _logger.LogWarning(
                        "Usuário {UserId} tentou acessar questionário {QuestionnaireId} não encontrado",
                        CurrentUserId,
                        id
                    );
                    return NotFound(new ApiResponse<object>()
                    {
                        Success = false,
                        Message = "Questionário não encontrado"
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
                var response = PsychologistQuestionnaireMapper.ToResponse(questionnaire, userSummary);

                return Ok(new ApiResponse<PsychologistQuestionnaireResponse?>()
                {
                    Success = true,
                    Message = "Questionário retornado com sucesso",
                    Data = response,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao retornar questionário {QuestionnaireId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Ocorreu um erro ao retornar questionário"
                });
            }
        }
        #endregion

        #region UPDATE
        [Authorize(Roles = "Admin,Psychologist")]
        [HttpPut]
        public async Task<IActionResult> UpdatePsychologistQuestionnaire([FromBody] PsychologistQuestionnaireUpdateRequest model)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem();
            }

            using var transaction = await _appDbContext.Database.BeginTransactionAsync();

            try
            {
                var questionnaire = await _appDbContext.PsychologistQuestionnaires.FirstOrDefaultAsync(s => s.ID == model.ID);

                if (questionnaire == null)
                {
                    _logger.LogWarning(
                        "Usuário {UserId} tentou acessar questionário {QuestionnaireId} não encontrado",
                        CurrentUserId,
                        model.ID
                    );
                    return NotFound(new ApiResponse<object>()
                    {
                        Success = false,
                        Message = "Questionário não encontrado"
                    });
                }

                if (User.IsInRole("Psychologist") && !string.Equals(questionnaire.UserID, CurrentUserId, StringComparison.Ordinal))
                {
                    return Forbid();
                }

                PsychologistQuestionnaireMapper.UpdateEntity(questionnaire, model);

                await _appDbContext.Logs.AddAsync(new Log
                {
                    LogLevel = Models.Enums.LogLevel.Info.ToString(),
                    Message = $"Questionário atualizado com sucesso com ID {questionnaire.ID}",
                    Source = nameof(PsychologistQuestionnairesController),
                    UserID = CurrentUserGuid,
                    EntityType = nameof(PsychologistQuestionnaire),
                    EntityID = questionnaire.ID,
                    Details = "",
                    Action = "Update"
                });

                await _appDbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new ApiResponse<object>()
                {
                    Success = true,
                    Message = "Questionário atualizado com sucesso",
                    Data = new
                    {
                        questionnaire.ID,
                    }
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Erro ao atualizar questionário com ID {Id}", model.ID);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Ocorreu um erro interno ao atualizar o questionário"
                });
            }
        }
        #endregion

        #region DELETE
        [Authorize(Roles = "Admin,Psychologist")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePsychologistQuestionnaire(Guid id)
        {
            try
            {
                var questionnaire = await _appDbContext.PsychologistQuestionnaires.FirstOrDefaultAsync(temp => temp.ID == id);

                if (questionnaire == null)
                {
                    _logger.LogWarning(
                        "Usuário {UserId} tentou acessar questionário {QuestionnaireId} não encontrado",
                        CurrentUserId,
                        id
                    );
                    return NotFound(new ApiResponse<object>()
                    {
                        Success = false,
                        Message = "Questionário não encontrado"
                    });
                }

                if (User.IsInRole("Psychologist") && !string.Equals(questionnaire.UserID, CurrentUserId, StringComparison.Ordinal))
                {
                    return Forbid();
                }

                _appDbContext.PsychologistQuestionnaires.Remove(questionnaire);

                await _appDbContext.Logs.AddAsync(new Log
                {
                    LogLevel = Models.Enums.LogLevel.Info.ToString(),
                    Message = $"Questionário excluído com sucesso com ID {questionnaire.ID}",
                    Source = nameof(PsychologistQuestionnairesController),
                    UserID = CurrentUserGuid,
                    EntityType = nameof(PsychologistQuestionnaire),
                    EntityID = questionnaire.ID,
                    Details = "",
                    Action = "Delete"
                });

                await _appDbContext.SaveChangesAsync();

                return Ok(new ApiResponse<object>()
                {
                    Success = true,
                    Message = "Questionário excluído com sucesso"
                });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao excluir questionário com id {id}");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Ocorreu um erro ao excluir o questionário com id {id}"
                });
            }
        }

        #endregion
    }
}
