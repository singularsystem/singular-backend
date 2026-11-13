using SingularSystem.DTOs.PsychopedagogueReports.CognitiveAssessments;
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
    [Route("api/v1/cognitive-assessments")]
    [ApiController]
    public class CognitiveAssessmentsController : BaseController
    {
        private readonly ILogger<CognitiveAssessmentsController> _logger;
        private readonly AppDbContext _appDbContext;
        private readonly NotificationService _notificationService;
        private readonly IUserSummaryFactory _userSummaryFactory;
        private readonly UserManager<ApplicationUser> _userManager;

        public CognitiveAssessmentsController(ILogger<CognitiveAssessmentsController> logger, AppDbContext appDbContext, NotificationService notificationService, IUserSummaryFactory userSummaryFactory, UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _appDbContext = appDbContext;
            _notificationService = notificationService;
            _userSummaryFactory = userSummaryFactory;
            _userManager = userManager;
        }

        #region CREATE
        [Authorize(Policy = "AdminOrPsychopedagogue")]
        [HttpPost]
        public async Task<IActionResult> CreateCognitiveAssessment([FromBody] CognitiveAssessmentRequest model)
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

                var questionnaire = CognitiveAssessmentMapper.ToEntity(model, CurrentUserId);
                await _appDbContext.CognitiveAssessments.AddAsync(questionnaire);

                await _appDbContext.Logs.AddAsync(new Log
                {
                    LogLevel = Models.Enums.LogLevel.Info.ToString(),
                    Message = $"Questionário criado com sucesso com ID {questionnaire.ID}",
                    Source = nameof(CognitiveAssessmentsController),
                    UserID = CurrentUserGuid,
                    EntityType = nameof(CognitiveAssessment),
                    EntityID = questionnaire.ID,
                    Details = "",
                    Action = "Create"
                });

                var psychopedagogueReport = await _appDbContext.PsychopedagogueReports
                                            .Include(r => r.Student)
                                            .Include(r => r.PsychopedagogueQuestionnaire)
                                            .Include(r => r.CARSQuestionnaire)
                                            .Include(r => r.MCHATQuestionnaire)
                                            .FirstOrDefaultAsync(r => r.ID == questionnaire.PsychopedagogueReportID);


                if (psychopedagogueReport == null)
                {
                    return NotFound(new ApiResponse<object>()
                    {
                        Success = false,
                        Message = $"Relatório com ID {questionnaire.PsychopedagogueReportID} não encontrado"
                    });
                }

                bool shouldUnify = psychopedagogueReport.PsychopedagogueQuestionnaire != null &&
                    (psychopedagogueReport.CARSQuestionnaire != null || psychopedagogueReport.MCHATQuestionnaire != null);

                if (shouldUnify)
                {
                    psychopedagogueReport.UnifiedAt = DateTime.UtcNow;
                }

                psychopedagogueReport.UserID = CurrentUserId;

                var report = await _appDbContext.Reports.Include(s => s.Student).Include(r => r.SocialWorkerReport)
                                                        .Include(r => r.PsychologistReport).ThenInclude(pr => pr!.AdaptiveBehaviorScaleDI)
                                                        .Include(r => r.PsychologistReport).ThenInclude(pr => pr!.SnapIVQuestionnaire)
                                                        .Include(r => r.PsychologistReport).ThenInclude(pr => pr!.PsychologistQuestionnaire)
                                                        .Include(r => r.PsychopedagogueReport).ThenInclude(pp => pp!.PsychopedagogueQuestionnaire).ThenInclude(pq => pq.TypeOfDisability)
                                                        .Include(r => r.PsychopedagogueReport).ThenInclude(pp => pp!.CARSQuestionnaire)
                                                        .Include(r => r.PsychopedagogueReport).ThenInclude(pp => pp!.MCHATQuestionnaire)
                                                        .Include(r => r.PsychopedagogueReport).ThenInclude(pp => pp!.CognitiveAssessment)
                                                        .FirstOrDefaultAsync(r => r.PsychopedagogueReportID == psychopedagogueReport.ID);


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
                    actionURL: "api/v1/cognitive-assessments"
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
        public async Task<IActionResult> GetCognitiveAssessments([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchTerm = "")
        {
            try
            {
                const int maxPageSize = 50;
                pageSize = Math.Clamp(pageSize, 1, maxPageSize);
                page = Math.Max(page, 1);
                var skip = (page - 1) * pageSize;

                var query = _appDbContext.CognitiveAssessments.IgnoreQueryFilters()
                                         .Include(s => s.Student)
                                         .Include(s => s.Student!.School)
                                         .Include(u => u.User)
                                         .AsNoTracking();

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

                if (!isAdmin)
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
                    return Ok(new ApiResponse<List<CognitiveAssessmentResponse>>()
                    {
                        Success = true,
                        Message = "Nenhum questionário encontrado",
                        Data = new List<CognitiveAssessmentResponse>(),
                        Total = totalQuestionnaires
                    });
                }

                var users = questionnaires.Where(q => q.User != null)
                                          .Select(q => q.User!)
                                          .DistinctBy(u => u.Id)
                                          .ToList();

                var userSummaries = await _userSummaryFactory.CreateManyAsync(users);

                var response = questionnaires.Select(q =>
                    CognitiveAssessmentMapper.ToResponse(
                        q,
                        q.User != null && userSummaries.TryGetValue(q.User.Id, out var us)
                            ? us
                            : null
                    )
                ).ToList();

                return Ok(new ApiResponse<List<CognitiveAssessmentResponse?>>()
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
        public async Task<IActionResult> GetCognitiveAssessment(Guid id)
        {
            try
            {

                var questionnaire = await _appDbContext.CognitiveAssessments.IgnoreQueryFilters()
                                         .Include(s => s.Student)
                                         .Include(s => s.Student!.School)
                                         .Include(r => r.PsychopedagogueReport)
                                         .Include(u => u.User)
                                         .AsNoTracking()
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
                var response = CognitiveAssessmentMapper.ToResponse(questionnaire, userSummary);

                return Ok(new ApiResponse<CognitiveAssessmentResponse?>()
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
        [Authorize(Policy = "AdminOrPsychopedagogue")]
        [HttpPut]
        public async Task<IActionResult> UpdateCognitiveAssessment([FromBody] CognitiveAssessmentUpdateRequest model)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem();
            }

            using var transaction = await _appDbContext.Database.BeginTransactionAsync();

            try
            {
                var questionnaire = await _appDbContext.CognitiveAssessments.FirstOrDefaultAsync(s => s.ID == model.ID);

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

                if (User.IsInRole("Psychopedagogue") && !string.Equals(questionnaire.UserID, CurrentUserId, StringComparison.Ordinal))
                {
                    return Forbid();
                }

                CognitiveAssessmentMapper.UpdateEntity(questionnaire, model);

                await _appDbContext.Logs.AddAsync(new Log
                {
                    LogLevel = Models.Enums.LogLevel.Info.ToString(),
                    Message = $"Questionário atualizado com sucesso com ID {questionnaire.ID}",
                    Source = nameof(CognitiveAssessmentsController),
                    UserID = CurrentUserGuid,
                    EntityType = nameof(CognitiveAssessment),
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
        [Authorize(Policy = "AdminOrPsychopedagogue")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCognitiveAssessment(Guid id)
        {
            try
            {
                var questionnaire = await _appDbContext.CognitiveAssessments.FirstOrDefaultAsync(temp => temp.ID == id);

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

                if (User.IsInRole("Psychopedagogue") && !string.Equals(questionnaire.UserID, CurrentUserId, StringComparison.Ordinal))
                {
                    return Forbid();
                }

                _appDbContext.CognitiveAssessments.Remove(questionnaire);

                await _appDbContext.Logs.AddAsync(new Log
                {
                    LogLevel = Models.Enums.LogLevel.Info.ToString(),
                    Message = $"Questionário excluído com sucesso com ID {questionnaire.ID}",
                    Source = nameof(CognitiveAssessmentsController),
                    UserID = CurrentUserGuid,
                    EntityType = nameof(CognitiveAssessment),
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
