using SingularSystem.DTOs.RegularTeachers.NeuropsychopedagogicalQuestionnaires;
using SingularSystem.DTOs.ReportAEE;
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
using SingularSystem.Domain.Tenancy;

namespace SingularSystem.Controllers
{
    [Authorize]
    [NoSecretary]
    [Route("api/v1/regular-teacher-questionnaires")]
    [ApiController]
    public class RegularTeacherQuestionnairesController : BaseController
    {
        private readonly ILogger<RegularTeacherQuestionnairesController> _logger;
        private readonly AppDbContext _appDbContext;
        private readonly NotificationService _notificationService;
        private readonly StudentEvaluationDocumentService _studentEvaluationDocumentService;
        private readonly IUserSummaryFactory _userSummaryFactory;
        private readonly ISignatureService _zapSignService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITenantContext _tenantContext;

        public RegularTeacherQuestionnairesController(ILogger<RegularTeacherQuestionnairesController> logger, AppDbContext appDbContext, NotificationService notificationService, StudentEvaluationDocumentService studentEvaluationDocumentService, IUserSummaryFactory userSummaryFactory, ISignatureService zapSignService, UserManager<ApplicationUser> userManager, ITenantContext tenantContext)
        {
            _logger = logger;
            _appDbContext = appDbContext;
            _notificationService = notificationService;
            _studentEvaluationDocumentService = studentEvaluationDocumentService;
            _userSummaryFactory = userSummaryFactory;
            _zapSignService = zapSignService;
            _userManager = userManager;
            _tenantContext = tenantContext;
        }

        private IQueryable<RegularTeacherQuestionnaire> BuildRegularTeacherQuestionnaireQuery(
            ApplicationUser currentUser,
            string state,
            string city,
            string schoolID,
            string searchTerm)
        {
            var isTeacher = User.IsInRole("Teacher");
            var isCoordinator = User.IsInRole("Coordinator");
            var isPrincipal = User.IsInRole("Principal");
            var isMunicipalManager = User.IsInRole("MunicipalManager");
            var isSocialWorker = User.IsInRole("SocialWork");
            var isPsychologist = User.IsInRole("Psychologist");
            var isPsychopedagogue = User.IsInRole("Psychopedagogue");
            var isSecretary = User.IsInRole("Secretary");
            var isAdmin = User.IsInRole("Admin");

            var query = _appDbContext.RegularTeacherQuestionnaires
                                     .Include(s => s.Student).Include(st => st.Student!.School)
                                     .Include(q => q.BehaviorTraits)
                                     .AsNoTracking()
                                     .AsSplitQuery();

            if (_tenantContext.IsPrivatePlan)
            {
                query = query.Where(s => s.Student != null && s.Student.TenantId == _tenantContext.TenantId);
                return query;
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(s => s.Student != null &&
                    s.Student.FullName.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(state))
            {
                query = query.Where(s =>
                    s.Student != null && (s.Student.State == state || (s.Student.School != null && s.Student.School.State == state)));
            }

            if (!string.IsNullOrWhiteSpace(city))
            {
                query = query.Where(s =>
                    s.Student != null && (s.Student.City == city || (s.Student.School != null && s.Student.School.City == city)));
            }

            if (!string.IsNullOrWhiteSpace(schoolID))
            {
                query = query.Where(s =>
                    s.Student != null && s.Student.SchoolID == Guid.Parse(schoolID));
            }

            if (!isAdmin)
            {
                if (isTeacher)
                {
                    query = query.Where(s => s.UserID == currentUser.Id ||
                                             (s.Student != null &&
                                              ((s.Student.City == currentUser.City && s.Student.State == currentUser.State) ||
                                               (s.Student.School != null && s.Student.School.City == currentUser.City && s.Student.School.State == currentUser.State))));
                }
                else if (isCoordinator || isPrincipal || isMunicipalManager || isSecretary || isSocialWorker || isPsychologist || isPsychopedagogue)
                {
                    query = query.Where(s => s.Student != null &&
                                             ((s.Student.City == currentUser.City && s.Student.State == currentUser.State) ||
                                              (s.Student.School != null && s.Student.School.City == currentUser.City && s.Student.School.State == currentUser.State)));
                }
                else
                {
                    query = query.Where(s => s.Student != null &&
                                             ((s.Student.City == currentUser.City && s.Student.State == currentUser.State) ||
                                              (s.Student.School != null && s.Student.School.City == currentUser.City && s.Student.School.State == currentUser.State)));
                }
            }

            return query;
        }

        #region CREATE
        [Authorize(Roles = "Admin,Teacher")]
        [HttpPost]
        public async Task<IActionResult> CreateNeuropsychopedagogicalQuestionnaire([FromBody] RegularTeacherQuestionnaireRequest model)
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

                var questionnaire = RegularTeacherQuestionnaireMapper.ToEntity(model, CurrentUserId);
                await _appDbContext.RegularTeacherQuestionnaires.AddAsync(questionnaire);

                await _appDbContext.Logs.AddAsync(new Log
                {
                    LogLevel = Models.Enums.LogLevel.Info.ToString(),
                    Message = $"Questionário criado com sucesso com ID {questionnaire.ID}",
                    Source = "QuestionnairesController",
                    UserID = CurrentUserGuid,
                    EntityType = "RegularTeacherQuestionnaire",
                    EntityID = questionnaire.ID,
                    Details = "",
                    Action = "Create"
                });

                await _appDbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                var screening = await _appDbContext.RegularTeacherScreenings
                                                   .AsNoTracking()
                                                   .Where(s =>
                                                       s.StudentID == questionnaire.StudentID &&
                                                       !_appDbContext.RegularTeacherEvaluationDocuments
                                                       .Any(d => d.RegularTeacherScreeningID == s.ID)
                                                   )
                                                   .OrderByDescending(s => s.CreatedAt)
                                                   .FirstOrDefaultAsync();

                if (screening != null)
                {
                    var evaluationDocId = await _studentEvaluationDocumentService.TryCreateUnifiedDocumentAsync(screening.ID, questionnaire.ID, CurrentUserId);

                    if (evaluationDocId != null)
                    {
                        var resultDocCreation = await _zapSignService.CreateDoc(evaluationDocId.Value, "caseStudy");

                        if (!resultDocCreation)
                        {
                            _logger.LogWarning($"Documento no ZapSign não foi criado para o Estudo de Caso ID {evaluationDocId}.");

                            return StatusCode(207, new ApiResponse<object>()
                            {
                                Success = true,
                                Message = "Estudo de Caso salvo com sucesso, mas não foi possível criar o documento para assinatura.",
                                Data = new { questionnaire.ID }
                            });
                        }
                    }
                }

                await _notificationService.CreateNotificationAdmins(
                    type: TypeNotification.System,
                    title: "Questionário Neuropsicopedagógico criado!",
                    message: $"Um novo questionário foi criado.",
                    actionURL: "api/v1/neuropsychopedagogical-questionnaires"
                );

                return StatusCode(201, new ApiResponse<object>()
                {
                    Success = true,
                    Message = "Questionário Neuropsicopedagógico criado com sucesso",
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
        public async Task<IActionResult> GetNeuropsychopedagogicalQuestionnaires([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string state = "", [FromQuery] string city = "", [FromQuery] string schoolID = "", [FromQuery] string searchTerm = "")
        {
            try
            {
                const int maxPageSize = 50;
                pageSize = Math.Clamp(pageSize, 1, maxPageSize);
                page = Math.Max(page, 1);
                var skip = (page - 1) * pageSize;

                var currentUser = await _userManager.FindByIdAsync(CurrentUserId);
                if (currentUser == null) return Unauthorized();

                var query = BuildRegularTeacherQuestionnaireQuery(currentUser, state, city, schoolID, searchTerm)
                    .Where(q => q.StudentID.HasValue && q.Student != null);

                if (!await query.AnyAsync())
                {
                    return Ok(new ApiResponse<List<RegularTeacherQuestionnaireResponse>>
                    {
                        Success = true,
                        Message = "Nenhum questionário foi cadastrado ainda"
                    });
                }

                var uniqueTotal = await query
                    .Select(d => d.StudentID!.Value)
                    .Distinct()
                    .CountAsync();

                var pagedStudentIds = await query
                    .GroupBy(s => s.StudentID!.Value)
                    .Select(g => new
                    {
                        StudentId = g.Key,
                        LatestCreatedAt = g.Max(x => x.CreatedAt)
                    })
                    .OrderByDescending(x => x.LatestCreatedAt)
                    .Skip(skip)
                    .Take(pageSize)
                    .Select(x => x.StudentId)
                    .ToListAsync();

                if (pagedStudentIds.Count == 0)
                {
                    return Ok(new ApiResponse<List<RegularTeacherQuestionnaireResponse>>()
                    {
                        Success = true,
                        Message = "Nenhum questionário encontrado",
                        Data = new List<RegularTeacherQuestionnaireResponse>(),
                        Total = uniqueTotal,
                        UniqueTotal = uniqueTotal
                    });
                }

                var questionnairesForStudents = await query
                    .Where(s => pagedStudentIds.Contains(s.StudentID!.Value))
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync();

                var pageIndexByStudentId = pagedStudentIds
                    .Select((id, index) => new { id, index })
                    .ToDictionary(x => x.id, x => x.index);

                var questionnaires = questionnairesForStudents
                    .GroupBy(s => s.StudentID!.Value)
                    .Select(g => g.First())
                    .OrderBy(s => pageIndexByStudentId[s.StudentID!.Value])
                    .ToList();

                if (questionnaires == null || questionnaires.Count == 0)
                {
                    return Ok(new ApiResponse<List<RegularTeacherQuestionnaireResponse>>()
                    {
                        Success = true,
                        Message = "Nenhum questionário encontrado",
                        Data = new List<RegularTeacherQuestionnaireResponse>(),
                        Total = uniqueTotal,
                        UniqueTotal = uniqueTotal
                    });
                }

                var users = questionnaires.Where(q => q.User != null)
                                          .Select(q => q.User!)
                                          .DistinctBy(u => u.Id)
                                          .ToList();

                var userSummaries = await _userSummaryFactory.CreateManyAsync(users);

                var response = questionnaires.Select(q =>
                    RegularTeacherQuestionnaireMapper.ToResponse(
                        q,
                        q.User != null && userSummaries.TryGetValue(q.User.Id, out var us)
                            ? us
                            : null
                    )
                ).ToList();

                return Ok(new ApiResponse<List<RegularTeacherQuestionnaireResponse>>()
                {
                    Success = true,
                    Message = "Lista de questionários retornada com sucesso",
                    Data = response,
                    Total = uniqueTotal,
                    UniqueTotal = uniqueTotal
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message}");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Ocorreu um erro ao retornar questionários"
                });
            }
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllNeuropsychopedagogicalQuestionnairesNoPagination([FromQuery] string state = "", [FromQuery] string city = "", [FromQuery] string schoolID = "", [FromQuery] string searchTerm = "")
        {
            try
            {
                var currentUser = await _userManager.FindByIdAsync(CurrentUserId);
                if (currentUser == null) return Unauthorized();

                var query = BuildRegularTeacherQuestionnaireQuery(currentUser, state, city, schoolID, searchTerm);

                if (!query.Any())
                {
                    return Ok(new ApiResponse<List<RegularTeacherQuestionnaireResponse>>
                    {
                        Success = true,
                        Message = "Nenhum questionário foi cadastrado ainda"
                    });
                }

                var totalQuestionnaires = await query.CountAsync();
                var uniqueTotal = await query
                    .Where(d => d.StudentID.HasValue)
                    .Select(d => d.StudentID!.Value)
                    .Distinct()
                    .CountAsync();

                var questionnaires = await query.OrderByDescending(f => f.CreatedAt)
                                                .ToListAsync();

                if (questionnaires == null || questionnaires.Count == 0)
                {
                    return Ok(new ApiResponse<List<RegularTeacherQuestionnaireResponse>>()
                    {
                        Success = true,
                        Message = "Nenhum questionário encontrado",
                        Data = new List<RegularTeacherQuestionnaireResponse>(),
                        Total = totalQuestionnaires,
                        UniqueTotal = uniqueTotal
                    });
                }

                var users = questionnaires.Where(q => q.User != null)
                                          .Select(q => q.User!)
                                          .DistinctBy(u => u.Id)
                                          .ToList();

                var userSummaries = await _userSummaryFactory.CreateManyAsync(users);

                var response = questionnaires.Select(q =>
                    RegularTeacherQuestionnaireMapper.ToResponse(
                        q,
                        q.User != null && userSummaries.TryGetValue(q.User.Id, out var us)
                            ? us
                            : null
                    )
                ).ToList();

                return Ok(new ApiResponse<List<RegularTeacherQuestionnaireResponse>>()
                {
                    Success = true,
                    Message = "Lista de questionários retornada com sucesso",
                    Data = response,
                    Total = totalQuestionnaires,
                    UniqueTotal = uniqueTotal
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message}");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Ocorreu um erro ao retornar questionários"
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetNeuropsychopedagogicalQuestionnaire(Guid id)
        {
            try
            {
                var questionnaire = await _appDbContext.RegularTeacherQuestionnaires.IgnoreQueryFilters()
                                         .Include(s => s.Student)
                                         .Include(s => s.Student!.School)
                                         .Include(s => s.BehaviorTraits)
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
                var response = RegularTeacherQuestionnaireMapper.ToResponse(questionnaire, userSummary);

                return Ok(new ApiResponse<RegularTeacherQuestionnaireResponse>()
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
        [Authorize(Roles = "Admin,Teacher")]
        [HttpPut]
        public async Task<IActionResult> UpdateNeuropsychopedagogicalQuestionnaire([FromBody] RegularTeacherQuestionnaireUpdateRequest model)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem();
            }

            using var transaction = await _appDbContext.Database.BeginTransactionAsync();

            try
            {

                var questionnaire = await _appDbContext.RegularTeacherQuestionnaires.Include(s => s.BehaviorTraits).FirstOrDefaultAsync(temp => temp.ID == model.ID);

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

                if (User.IsInRole("Teacher") && !string.Equals(questionnaire.UserID, CurrentUserId, StringComparison.Ordinal))
                {
                    return StatusCode(403, new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Sem permissão para atualizar este questionário."
                    });
                }

                RegularTeacherQuestionnaireMapper.UpdateEntity(questionnaire, model);
                _appDbContext.RegularTeacherQuestionnaires.Update(questionnaire);

                await _appDbContext.Logs.AddAsync(new Log
                {
                    LogLevel = Models.Enums.LogLevel.Info.ToString(),
                    Message = $"Questionário Neuropsicopedagógico atualizado com sucesso com ID {questionnaire.ID}",
                    Source = "QuestionnairesController",
                    UserID = CurrentUserGuid,
                    EntityType = "RegularTeacherQuestionnaire",
                    EntityID = questionnaire.ID,
                    Details = "",
                    Action = "Update"
                });

                await _studentEvaluationDocumentService.MarkAsOutdatedByQuestionnaireAsync(questionnaire.ID);
                await _appDbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                await _notificationService.CreateNotificationAdmins(
                    type: TypeNotification.System,
                    title: "Questionário Neuropsicopedagógico atualizado",
                    message: $"Questionário Neuropsicopedagógico atualizado com sucesso.",
                    actionURL: "api/v1/neuropsychopedagogical-questionnaires"
                );

                if (questionnaire.RegularTeacherEvaluationDocument != null)
                {
                    await _zapSignService.DeleteDoc(questionnaire.RegularTeacherEvaluationDocument.ID, "caseStudy");

                    _logger.LogInformation($"Documento no ZapSign excluído com sucesso para o Estudo de Caso ID {questionnaire.RegularTeacherEvaluationDocument.ID}.");

                    var resultDocCreation = await _zapSignService.CreateDoc(questionnaire.RegularTeacherEvaluationDocument.ID, "caseStudy");

                    if (!resultDocCreation)
                    {
                        _logger.LogWarning($"Documento no ZapSign não foi criado para o Estudo de Caso ID {questionnaire.RegularTeacherEvaluationDocument.ID}.");

                        return StatusCode(207, new ApiResponse<object>()
                        {
                            Success = true,
                            Message = "Estudo de Caso atualizado com sucesso, mas não foi possível criar o documento para assinatura.",
                            Data = new
                            {
                                questionnaire.ID
                            }
                        });
                    }
                }

                return Ok(new ApiResponse<object>()
                {
                    Success = true,
                    Message = "Questionário Neuropsicopedagógico atualizado com sucesso",
                    Data = new
                    {
                        questionnaire.ID,
                        questionnaire.UserID,
                        questionnaire.StudentID
                    }
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError($"{ex.Message}");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Ocorreu um erro ao atualizar o questionário"
                });
            }
        }
        #endregion

        #region DELETE
        [Authorize(Roles = "Admin,Teacher")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNeuropsychopedagogicalQuestionnaire(Guid id)
        {
            try
            {
                var questionnaire = await _appDbContext.RegularTeacherQuestionnaires.Include(s => s.BehaviorTraits).FirstOrDefaultAsync(temp => temp.ID == id);

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

                if (User.IsInRole("Teacher") && questionnaire.UserID != CurrentUserId)
                {
                    return StatusCode(403, new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Sem permissão para excluir este questionário."
                    });
                }

                var doc = await _appDbContext.RegularTeacherEvaluationDocuments
                                .FirstOrDefaultAsync(d => d.RegularTeacherQuestionnaireID == id);

                if (doc != null)
                    _appDbContext.RegularTeacherEvaluationDocuments.Remove(doc);

                _appDbContext.RegularTeacherQuestionnaires.Remove(questionnaire);

                await _appDbContext.Logs.AddAsync(new Log
                {
                    LogLevel = Models.Enums.LogLevel.Info.ToString(),
                    Message = $"Questionário excluído com sucesso com ID {questionnaire.ID}",
                    Source = nameof(RegularTeacherQuestionnairesController),
                    UserID = CurrentUserGuid,
                    EntityType = nameof(RegularTeacherQuestionnaire),
                    EntityID = questionnaire.ID,
                    Details = "",
                    Action = "Delete"
                });

                await _appDbContext.SaveChangesAsync();

                if (questionnaire.RegularTeacherEvaluationDocument != null)
                {
                    var resultDocDelete = await _zapSignService.DeleteDoc(questionnaire.RegularTeacherEvaluationDocument.ID, "caseStudy");

                    if (!resultDocDelete)
                    {
                        _logger.LogWarning($"Documento no ZapSign não foi excluído para o Estudo de Caso ID {questionnaire.RegularTeacherEvaluationDocument.ID}.");

                        return StatusCode(207, new ApiResponse<object>()
                        {
                            Success = true,
                            Message = "Estudo de Caso excluído com sucesso, mas não foi possível excluir o documento.",
                            Data = new
                            {
                                questionnaire.RegularTeacherEvaluationDocument.ID
                            }
                        });
                    }

                    _logger.LogInformation($"Documento no ZapSign excluído com sucesso para o Estudo de Caso ID {questionnaire.RegularTeacherEvaluationDocument.ID}.");
                }

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
