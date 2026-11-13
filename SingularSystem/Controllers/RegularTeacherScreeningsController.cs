using SingularSystem.DTOs.RegularTeachers.RegularTeacherScreenings;
using SingularSystem.DTOs.ReportAEE;
using SingularSystem.Interfaces;
using SingularSystem.Mappers;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;
using SingularSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using SingularSystem.Authorization;
using SingularSystem.Domain.Tenancy;

namespace SingularSystem.Controllers
{
    [Authorize]
    [NoSecretary]
    [Route("api/v1/regular-teacher-screenings")]
    [ApiController]
    public class RegularTeacherScreeningsController : BaseController
    {
        private readonly ILogger<RegularTeacherScreeningsController> _logger;
        private readonly AppDbContext _appDbContext;
        private readonly NotificationService _notificationService;
        private readonly StudentEvaluationDocumentService _studentEvaluationDocumentService;
        private readonly IUserSummaryFactory _userSummaryFactory;
        private readonly ISignatureService _zapSignService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITenantContext _tenantContext;

        public RegularTeacherScreeningsController(ILogger<RegularTeacherScreeningsController> logger, AppDbContext appDbContext, NotificationService notificationService, StudentEvaluationDocumentService studentEvaluationDocumentService, IUserSummaryFactory userSummaryFactory, ISignatureService zapSignService, UserManager<ApplicationUser> userManager, ITenantContext tenantContext)
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

        private IQueryable<RegularTeacherScreening> BuildRegularTeacherScreeningQuery(
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

            var query = _appDbContext.RegularTeacherScreenings
                                     .Include(s => s.Student).ThenInclude(st => st!.School)
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
                    query = query.Where(p => p.Student != null && 
                                             ((p.Student.City == currentUser.City && p.Student.State == currentUser.State) ||
                                              (p.Student.School != null && p.Student.School.City == currentUser.City && p.Student.School.State == currentUser.State)));
                }
                else
                {
                    // Default security filter
                    query = query.Where(p => p.Student != null && 
                                             ((p.Student.City == currentUser.City && p.Student.State == currentUser.State) ||
                                              (p.Student.School != null && p.Student.School.City == currentUser.City && p.Student.School.State == currentUser.State)));
                }
            }

            return query;
        }

        #region CREATE
        [Authorize(Roles = "Admin,Teacher")]
        [HttpPost]
        public async Task<IActionResult> CreateRegularTeacherScreening([FromBody] RegularTeacherScreeningRequest model)
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

                var screening = RegularTeacherScreeningMapper.ToEntity(model, CurrentUserId);
                await _appDbContext.RegularTeacherScreenings.AddAsync(screening);

                await _appDbContext.Logs.AddAsync(new Log
                {
                    LogLevel = Models.Enums.LogLevel.Info.ToString(),
                    Message = $"Triagem criada com sucesso com ID {screening.ID}",
                    Source = nameof(RegularTeacherScreeningsController),
                    UserID = CurrentUserGuid,
                    EntityType = nameof(RegularTeacherScreening),
                    EntityID = screening.ID,
                    Details = "",
                    Action = "Create"
                });

                await _appDbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                var questionnaire = await _appDbContext.RegularTeacherQuestionnaires
                                                       .AsNoTracking()
                                                       .Where(s =>
                                                           s.StudentID == screening.StudentID &&
                                                           !_appDbContext.RegularTeacherEvaluationDocuments
                                                           .Any(d => d.RegularTeacherQuestionnaireID == s.ID)
                                                       )
                                                       .OrderByDescending(s => s.CreatedAt)
                                                       .FirstOrDefaultAsync();

                if (questionnaire != null)
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
                    title: "Nova triagem criada!",
                    message: $"Uma nova triagem foi criada.",
                    actionURL: "api/v1/regular-teacher-screenings"
                );

                return StatusCode(201, new ApiResponse<object>()
                {
                    Success = true,
                    Message = "Triagem criada com sucesso",
                    Data = new
                    {
                        screening.ID
                    }
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Erro ao criar triagem");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Ocorreu um erro ao criar triagem"
                });
            }
        }
        #endregion

        #region GET
        [HttpGet]
        public async Task<IActionResult> GetRegularTeacherScreenings([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string state = "", [FromQuery] string city = "", [FromQuery] string schoolID = "", [FromQuery] string searchTerm = "")
        {
            try
            {
                const int maxPageSize = 50;
                pageSize = Math.Clamp(pageSize, 1, maxPageSize);
                page = Math.Max(page, 1);
                var skip = (page - 1) * pageSize;

                var user = await _userManager.FindByIdAsync(CurrentUserId);
                if (user == null) return Unauthorized();

                var query = BuildRegularTeacherScreeningQuery(user, state, city, schoolID, searchTerm)
                    .Where(s => s.StudentID.HasValue && s.Student != null);

                if (!await query.AnyAsync())
                {
                    return Ok(new ApiResponse<List<RegularTeacherScreeningResponse>>
                    {
                        Success = true,
                        Message = "Nenhuma triagem foi cadastrada ainda"
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
                    return Ok(new ApiResponse<List<RegularTeacherScreeningResponse>>()
                    {
                        Success = true,
                        Message = "Nenhuma triagem encontrada",
                        Data = new List<RegularTeacherScreeningResponse>(),
                        Total = uniqueTotal,
                        UniqueTotal = uniqueTotal
                    });
                }

                var screeningsForStudents = await query
                    .Where(s => pagedStudentIds.Contains(s.StudentID!.Value))
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync();

                var pageIndexByStudentId = pagedStudentIds
                    .Select((id, index) => new { id, index })
                    .ToDictionary(x => x.id, x => x.index);

                var screenings = screeningsForStudents
                    .GroupBy(s => s.StudentID!.Value)
                    .Select(g => g.First())
                    .OrderBy(s => pageIndexByStudentId[s.StudentID!.Value])
                    .ToList();

                if (screenings == null || screenings.Count == 0)
                {
                    return Ok(new ApiResponse<List<RegularTeacherScreeningResponse>>()
                    {
                        Success = true,
                        Message = "Nenhuma triagem encontrada",
                        Data = new List<RegularTeacherScreeningResponse>(),
                        Total = uniqueTotal,
                        UniqueTotal = uniqueTotal
                    });
                }

                var users = screenings.Where(q => q.User != null)
                                          .Select(q => q.User!)
                                          .DistinctBy(u => u.Id)
                                          .ToList();

                var userSummaries = await _userSummaryFactory.CreateManyAsync(users);

                var response = screenings.Select(q =>
                    RegularTeacherScreeningMapper.ToResponse(
                        q,
                        q.User != null && userSummaries.TryGetValue(q.User.Id, out var us)
                            ? us
                            : null
                    )
                ).ToList();

                return Ok(new ApiResponse<List<RegularTeacherScreeningResponse>>()
                {
                    Success = true,
                    Message = "Lista de triagens retornada com sucesso",
                    Data = response,
                    Total = uniqueTotal,
                    UniqueTotal = uniqueTotal
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao retornar triagens");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Ocorreu um erro ao retornar triagens."
                });
            }
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllRegularTeacherScreeningsNoPagination([FromQuery] string state = "", [FromQuery] string city = "", [FromQuery] string schoolID = "", [FromQuery] string searchTerm = "")
        {
            try
            {
                var user = await _userManager.FindByIdAsync(CurrentUserId);
                if (user == null) return Unauthorized();

                var query = BuildRegularTeacherScreeningQuery(user, state, city, schoolID, searchTerm);

                if (!query.Any())
                {
                    return Ok(new ApiResponse<List<RegularTeacherScreeningResponse>>
                    {
                        Success = true,
                        Message = "Nenhuma triagem foi cadastrada ainda"
                    });
                }

                var totalScreenings = await query.CountAsync();
                var uniqueTotal = await query
                    .Where(d => d.StudentID.HasValue)
                    .Select(d => d.StudentID!.Value)
                    .Distinct()
                    .CountAsync();

                var screenings = await query.OrderByDescending(f => f.CreatedAt)
                                            .ToListAsync();

                if (screenings == null || screenings.Count == 0)
                {
                    return Ok(new ApiResponse<List<RegularTeacherScreeningResponse>>()
                    {
                        Success = true,
                        Message = "Nenhuma triagem encontrada",
                        Data = new List<RegularTeacherScreeningResponse>(),
                        Total = totalScreenings,
                        UniqueTotal = uniqueTotal
                    });
                }

                var users = screenings.Where(q => q.User != null)
                                          .Select(q => q.User!)
                                          .DistinctBy(u => u.Id)
                                          .ToList();

                var userSummaries = await _userSummaryFactory.CreateManyAsync(users);

                var response = screenings.Select(q =>
                    RegularTeacherScreeningMapper.ToResponse(
                        q,
                        q.User != null && userSummaries.TryGetValue(q.User.Id, out var us)
                            ? us
                            : null
                    )
                ).ToList();

                return Ok(new ApiResponse<List<RegularTeacherScreeningResponse>>()
                {
                    Success = true,
                    Message = "Lista de triagens retornada com sucesso",
                    Data = response,
                    Total = totalScreenings,
                    UniqueTotal = uniqueTotal
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao retornar triagens");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Ocorreu um erro ao retornar triagens."
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRegularTeacherScreening(Guid id)
        {
            try
            {
                var screening = await _appDbContext.RegularTeacherScreenings.IgnoreQueryFilters()
                                         .Include(s => s.Student).Include(st => st.Student!.School)
                                         .AsNoTracking()
                                         .FirstOrDefaultAsync(s => s.ID == id);

                if (screening == null)
                {
                    _logger.LogWarning(
                        "Usuário {UserId} tentou acessar triagem {ScreeningId} não encontrada",
                        CurrentUserId,
                        id
                    );
                    return NotFound(new ApiResponse<object>()
                    {
                        Success = false,
                        Message = "Triagem não encontrada"
                    });
                }

                var user = await _userManager.FindByIdAsync(CurrentUserId);
                if (user == null) return Unauthorized();

                var roles = await _userManager.GetRolesAsync(user);

                if (screening.Student != null && !HasStudentAccess(user, screening.Student, roles))
                {
                    return Forbid();
                }

                var userSummary = await _userSummaryFactory.CreateAsync(screening.User);
                var response = RegularTeacherScreeningMapper.ToResponse(screening, userSummary);

                return Ok(new ApiResponse<RegularTeacherScreeningResponse>()
                {
                    Success = true,
                    Message = "Triagem retornada com sucesso",
                    Data = response,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao retornar triagem {ScreeningId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Ocorreu um erro ao retornar triagem"
                });
            }
        }
        #endregion

        #region UPDATE
        [Authorize(Roles = "Admin,Teacher")]
        [HttpPut]
        public async Task<IActionResult> UpdateRegularTeacherScreening([FromBody] RegularTeacherScreeningUpdateRequest model)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem();
            }

            using var transaction = await _appDbContext.Database.BeginTransactionAsync();

            try
            {
                var screening = await _appDbContext.RegularTeacherScreenings.FirstOrDefaultAsync(s => s.ID == model.ID);

                if (screening == null)
                {
                    _logger.LogWarning(
                        "Usuário {UserId} tentou acessar triagem {ScreeningId} não encontrada",
                        CurrentUserId,
                        model.ID
                    );
                    return NotFound(new ApiResponse<object>()
                    {
                        Success = false,
                        Message = "Triagem não encontrada"
                    });
                }

                if (User.IsInRole("Teacher") && !string.Equals(screening.UserID, CurrentUserId, StringComparison.Ordinal))
                {
                    return StatusCode(403, new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Sem permissão para atualizar esta triagem."
                    });
                }

                RegularTeacherScreeningMapper.UpdateEntity(screening, model);

                await _appDbContext.Logs.AddAsync(new Log
                {
                    LogLevel = Models.Enums.LogLevel.Info.ToString(),
                    Message = $"Triagem atualizada com sucesso com ID {screening.ID}",
                    Source = nameof(RegularTeacherScreeningsController),
                    UserID = CurrentUserGuid,
                    EntityType = nameof(RegularTeacherScreening),
                    EntityID = screening.ID,
                    Details = "",
                    Action = "Update"
                });

                await _studentEvaluationDocumentService.MarkAsOutdatedByScreeningAsync(screening.ID);

                await _appDbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                if (screening.RegularTeacherEvaluationDocument != null)
                {
                    await _zapSignService.DeleteDoc(screening.RegularTeacherEvaluationDocument.ID, "caseStudy");

                    _logger.LogInformation($"Documento no ZapSign excluído com sucesso para o Estudo de Caso ID {screening.RegularTeacherEvaluationDocument.ID}.");

                    var resultDocCreation = await _zapSignService.CreateDoc(screening.RegularTeacherEvaluationDocument.ID, "caseStudy");

                    if (!resultDocCreation)
                    {
                        _logger.LogWarning($"Documento no ZapSign não foi criado para o Estudo de Caso ID {screening.RegularTeacherEvaluationDocument.ID}.");

                        return StatusCode(207, new ApiResponse<object>()
                        {
                            Success = true,
                            Message = "Estudo de Caso atualizado com sucesso, mas não foi possível criar o documento para assinatura.",
                            Data = new
                            {
                                screening.ID
                            }
                        });
                    }
                }

                return Ok(new ApiResponse<object>()
                {
                    Success = true,
                    Message = "Triagem atualizada com sucesso",
                    Data = new
                    {
                        screening.ID,
                    }
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Erro ao atualizar triagem com ID {Id}", model.ID);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Ocorreu um erro interno ao atualizar a triagem"
                });
            }
        }
        #endregion

        #region DELETE
        [Authorize(Roles = "Admin,Teacher")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRegularTeacherScreening(Guid id)
        {
            try
            {
                var screening = await _appDbContext.RegularTeacherScreenings
                    .Include(s => s.RegularTeacherEvaluationDocument)
                    .FirstOrDefaultAsync(temp => temp.ID == id);

                if (screening == null)
                {
                    _logger.LogWarning(
                        "Usuário {UserId} tentou acessar triagem {ScreeningId} não encontrada",
                        CurrentUserId,
                        id
                    );
                    return NotFound(new ApiResponse<object>()
                    {
                        Success = false,
                        Message = "Triagem não encontrada"
                    });
                }

                if (User.IsInRole("Teacher") && !string.Equals(screening.UserID, CurrentUserId, StringComparison.Ordinal))
                {
                    return StatusCode(403, new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Sem permissão para excluir esta triagem."
                    });
                }

                Guid? evaluationDocId = screening.RegularTeacherEvaluationDocument?.ID;

                if (screening.RegularTeacherEvaluationDocument != null)
                {
                    _appDbContext.RegularTeacherEvaluationDocuments.Remove(screening.RegularTeacherEvaluationDocument);
                }

                _appDbContext.RegularTeacherScreenings.Remove(screening);

                await _appDbContext.Logs.AddAsync(new Log
                {
                    LogLevel = Models.Enums.LogLevel.Info.ToString(),
                    Message = $"Triagem excluída com sucesso com ID {screening.ID}",
                    Source = nameof(RegularTeacherScreeningsController),
                    UserID = CurrentUserGuid,
                    EntityType = nameof(RegularTeacherScreening),
                    EntityID = screening.ID,
                    Details = "",
                    Action = "Delete"
                });

                await _appDbContext.SaveChangesAsync();

                if (evaluationDocId != null)
                {
                    var resultDocDelete = await _zapSignService.DeleteDoc(evaluationDocId.Value, "caseStudy");

                    if (!resultDocDelete)
                    {
                        _logger.LogWarning($"Documento no ZapSign não foi excluído para o Estudo de Caso ID {evaluationDocId}.");

                        return StatusCode(207, new ApiResponse<object>()
                        {
                            Success = true,
                            Message = "Triagem excluída com sucesso, mas não foi possível excluir o documento.",
                            Data = new
                            {
                                EvaluationDocumentID = evaluationDocId
                            }
                        });
                    }

                    _logger.LogInformation($"Documento no ZapSign excluído com sucesso para o Estudo de Caso ID {evaluationDocId}.");
                }
                
                return Ok(new ApiResponse<object>()
                {
                    Success = true,
                    Message = "Triagem excluída com sucesso"
                });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao excluir triagem com id {id}");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Ocorreu um erro ao excluir a triagem com id {id}"
                });
            }
        }

        #endregion
    }
}
