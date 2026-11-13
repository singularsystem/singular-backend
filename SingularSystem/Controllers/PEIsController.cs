using SingularSystem.Domain.Tenancy;
using SingularSystem.DTOs.PEIs;
using SingularSystem.DTOs.Reports;
using SingularSystem.DTOs.Responses;
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

namespace SingularSystem.Controllers
{
    [Authorize]
    [Route("api/v1/peis")]
    [ApiController]
    public class PEIsController : BaseController
    {
        private readonly ILogger<PEIsController> _logger;
        private readonly AppDbContext _appDbContext;
        private readonly NotificationService _notificationService;
        private readonly IUserSummaryFactory _userSummaryFactory;
        private readonly ISignatureService _zapSignService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITenantContext _tenantContext;

        public PEIsController(ILogger<PEIsController> logger, AppDbContext appDbContext, NotificationService notificationService, IUserSummaryFactory userSummaryFactory, ISignatureService zapSignService, UserManager<ApplicationUser> userManager, ITenantContext tenantContext)
        {
            _logger = logger;
            _appDbContext = appDbContext;
            _notificationService = notificationService;
            _userSummaryFactory = userSummaryFactory;
            _zapSignService = zapSignService;
            _userManager = userManager;
            _tenantContext = tenantContext;
        }

        private IQueryable<PEI> BuildPEIQuery(
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

            var query = _appDbContext.PEIs.Include(s => s.Student).ThenInclude(s => s!.School)
                                          .Include(u => u.User)
                                          .Include(p => p.Subjects).ThenInclude(r => r.Responses)
                                          .AsNoTracking()
                                          .AsSplitQuery();

            if (_tenantContext.IsPrivatePlan)
            {
                query = query.Where(p => p.Student != null && p.Student.TenantId == _tenantContext.TenantId);
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

        private IQueryable<PEI> ApplyMyPEIsFilter(
            IQueryable<PEI> query,
            ApplicationUser user,
            string myStatus)
        {
            var isTeacher = User.IsInRole("Teacher");
            var isAdmin = User.IsInRole("Admin");
            var cutoffDate = new DateTime(2026, 8, 18, 0, 0, 0, DateTimeKind.Utc);
            var currentUserId = CurrentUserId;

            query = query.Where(p => isAdmin || p.UserID == currentUserId);

            if (!string.IsNullOrWhiteSpace(myStatus) && !myStatus.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                if (myStatus.Equals("pending_fill", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(p =>
                        !p.TeacherSign &&
                        (p.SignedFile == null || p.SignedFile == "") &&
                        p.DocumentStatus != Status.Complete &&
                        p.CreatedAt >= cutoffDate &&
                        !p.Subjects.Any()
                    );
                }
                else if (myStatus.Equals("pending_sign", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(p =>
                        p.CreatedAt >= cutoffDate &&
                        !p.TeacherSign &&
                        (p.SignedFile == null || p.SignedFile == "") &&
                        p.DocumentStatus != Status.Complete &&
                        p.Subjects.Any()
                    );
                }
                else if (myStatus.Equals("signed", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(p =>
                        p.TeacherSign ||
                        (p.SignedFile != null && p.SignedFile != "") ||
                        p.DocumentStatus == Status.Complete ||
                        p.CreatedAt < cutoffDate
                    );
                }
            }

            return query;
        }

        #region CREATE
        [Authorize(Policy = "AdminOrTeacher")]
        [HttpPost]
        public async Task<IActionResult> CreatePEI([FromBody] PEIRequest model)
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

                var plan = PEIMapper.ToEntity(model, CurrentUserId);
                if (_tenantContext.IsPrivatePlan)
                    plan.TenantId = _tenantContext.TenantId;
                await _appDbContext.PEIs.AddAsync(plan);

                await _appDbContext.Logs.AddAsync(new Log
                {
                    LogLevel = Models.Enums.LogLevel.Info.ToString(),
                    Message = $"PEI criado com sucesso com ID {plan.ID}",
                    Source = nameof(PEIsController),
                    UserID = CurrentUserGuid,
                    EntityType = nameof(PEI),
                    EntityID = plan.ID,
                    Details = "",
                    Action = "Create"
                });

                await _appDbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                bool resultDocCreation;
                try
                {
                    resultDocCreation = await _zapSignService.CreateDoc(plan.ID, "pei");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao criar documento no ZapSign para o PEI ID {PeiId}", plan.ID);
                    resultDocCreation = false;
                }

                if (!resultDocCreation)
                {
                    _logger.LogWarning($"Documento no ZapSign não foi criado para o PEI ID {plan.ID}.");

                    return StatusCode(207, new ApiResponse<object>()
                    {
                        Success = true,
                        Message = "PEI salvo com sucesso, mas não foi possível criar o documento para assinatura.",
                        Data = new
                        {
                            plan.ID
                        }
                    });
                }

                await _notificationService.CreateNotificationAdmins(
                    type: TypeNotification.System,
                    title: "Novo PEI criado!",
                    message: $"Um novo PEI foi criado.",
                    actionURL: "api/v1/peis"
                );

                return StatusCode(201, new ApiResponse<object>()
                {
                    Success = true,
                    Message = "PEI criado com sucesso",
                    Data = new
                    {
                        plan.ID
                    }
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Erro ao criar PEI");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Ocorreu um erro ao criar PEI"
                });
            }
        }
        #endregion

        #region GET
        [HttpGet("my-summary")]
        public async Task<IActionResult> GetMyPEISummary(
            [FromQuery] string state = "",
            [FromQuery] string city = "",
            [FromQuery] string schoolID = "",
            [FromQuery] string searchTerm = "")
        {
            try
            {
                var user = await _userManager.FindByIdAsync(CurrentUserId);
                if (user == null) return Unauthorized();

                var baseQuery = BuildPEIQuery(user, state, city, schoolID, searchTerm);

                var total = await ApplyMyPEIsFilter(baseQuery, user, "all").CountAsync();
                var pendingFill = await ApplyMyPEIsFilter(baseQuery, user, "pending_fill").CountAsync();
                var pendingSign = await ApplyMyPEIsFilter(baseQuery, user, "pending_sign").CountAsync();
                var signed = await ApplyMyPEIsFilter(baseQuery, user, "signed").CountAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Resumo retornado com sucesso",
                    Data = new
                    {
                        total,
                        pendingFill,
                        pendingSign,
                        signed
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter resumo de PEIs do usuário");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Erro ao obter resumo de PEIs do usuário"
                });
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetPEIs([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string state = "", [FromQuery] string city = "", [FromQuery] string schoolID = "", [FromQuery] string searchTerm = "", [FromQuery] bool onlyMyDocs = false, [FromQuery] bool onlyMyReports = false, [FromQuery] string myStatus = "")
        {
            try
            {
                const int maxPageSize = 50;
                pageSize = Math.Clamp(pageSize, 1, maxPageSize);
                page = Math.Max(page, 1);
                var skip = (page - 1) * pageSize;

                var user = await _userManager.FindByIdAsync(CurrentUserId);
                if (user == null) return Unauthorized();

                var query = BuildPEIQuery(user, state, city, schoolID, searchTerm);

                if (onlyMyDocs || onlyMyReports)
                {
                    query = ApplyMyPEIsFilter(query, user, myStatus);
                }

                if (!query.Any())
                {
                    return Ok(new ApiResponse<List<PAEEResponse>>
                    {
                        Success = true,
                        Message = "Nenhum PAEE foi cadastrado ainda"
                    });
                }

                var totalPlans = await query.CountAsync();
                var uniqueTotal = await query
                    .Where(d => d.StudentID.HasValue)
                    .Select(d => d.StudentID!.Value)
                    .Distinct()
                    .CountAsync();

                var plans = await query.OrderByDescending(f => f.CreatedAt)
                                         .Skip(skip)
                                         .Take(pageSize)
                                         .ToListAsync();

                if (plans == null || plans.Count == 0)
                {
                    return Ok(new ApiResponse<List<PEIResponse>>()
                    {
                        Success = true,
                        Message = "Nenhum PEI encontrado",
                        Data = new List<PEIResponse>(),
                        Total = totalPlans,
                        UniqueTotal = uniqueTotal
                    });
                }

                var users = plans.Where(q => q.User != null)
                                          .Select(q => q.User!)
                                          .DistinctBy(u => u.Id)
                                          .ToList();

                var userSummaries = await _userSummaryFactory.CreateManyAsync(users);

                var response = plans.Select(q =>
                    PEIMapper.ToResponse(
                        q,
                        q.User != null && userSummaries.TryGetValue(q.User.Id, out var us)
                            ? us
                            : null
                    )
                ).ToList();

                return Ok(new ApiResponse<List<PEIResponse>>()
                {
                    Success = true,
                    Message = "Lista de PEIs retornado com sucesso",
                    Data = response,
                    Total = totalPlans,
                    UniqueTotal = uniqueTotal
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao retornar PEIs");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Ocorreu um erro ao retornar PEIs."
                });
            }
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllPEIsNoPagination([FromQuery] string state = "", [FromQuery] string city = "", [FromQuery] string schoolID = "", [FromQuery] string searchTerm = "", [FromQuery] bool onlyMyDocs = false, [FromQuery] bool onlyMyReports = false, [FromQuery] string myStatus = "")
        {
            try
            {
                var user = await _userManager.FindByIdAsync(CurrentUserId);
                if (user == null) return Unauthorized();

                var query = BuildPEIQuery(user, state, city, schoolID, searchTerm);

                if (onlyMyDocs || onlyMyReports)
                {
                    query = ApplyMyPEIsFilter(query, user, myStatus);
                }

                if (!query.Any())
                {
                    return Ok(new ApiResponse<List<PAEEResponse>>
                    {
                        Success = true,
                        Message = "Nenhum PAEE foi cadastrado ainda"
                    });
                }

                var totalPlans = await query.CountAsync();
                var uniqueTotal = await query
                    .Where(d => d.StudentID.HasValue)
                    .Select(d => d.StudentID!.Value)
                    .Distinct()
                    .CountAsync();

                var plans = await query.OrderByDescending(f => f.CreatedAt)
                                       .ToListAsync();

                if (plans == null || plans.Count == 0)
                {
                    return Ok(new ApiResponse<List<PEIResponse>>()
                    {
                        Success = true,
                        Message = "Nenhum PEI encontrado",
                        Data = new List<PEIResponse>(),
                        Total = totalPlans,
                        UniqueTotal = uniqueTotal
                    });
                }

                var users = plans.Where(q => q.User != null)
                                          .Select(q => q.User!)
                                          .DistinctBy(u => u.Id)
                                          .ToList();

                var userSummaries = await _userSummaryFactory.CreateManyAsync(users);

                var response = plans.Select(q =>
                    PEIMapper.ToResponse(
                        q,
                        q.User != null && userSummaries.TryGetValue(q.User.Id, out var us)
                            ? us
                            : null
                    )
                ).ToList();

                return Ok(new ApiResponse<List<PEIResponse>>()
                {
                    Success = true,
                    Message = "Lista de PEIs retornado com sucesso",
                    Data = response,
                    Total = totalPlans,
                    UniqueTotal = uniqueTotal
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao retornar PEIs");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Ocorreu um erro ao retornar PEIs."
                });
            }
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetPEI(Guid id)
        {
            try
            {

                PEI? pei = await _appDbContext.PEIs.Include(s => s.Student)
                                                            .Include(s => s.Student!.School)
                                                            .Include(u => u.User)
                                                            .Include(p => p.Subjects).ThenInclude(r => r.Responses)
                                                            .AsNoTracking()
                                                            .FirstOrDefaultAsync(s => s.ID == id);

                if (pei == null)
                {
                    _logger.LogWarning(
                        "Usuário {UserId} tentou acessar PEI {PEIId} não encontrado",
                        CurrentUserId,
                        id
                    );
                    return NotFound(new ApiResponse<PEIResponse>()
                    {
                        Success = false,
                        Message = "PEI não encontrado"
                    });
                }

                // Security check
                var user = await _userManager.FindByIdAsync(CurrentUserId);
                if (user == null) return Unauthorized();

                var roles = await _userManager.GetRolesAsync(user);

                if (pei.Student != null && !HasStudentAccess(user, pei.Student, roles))
                {
                    return Forbid();
                }

                // Sincroniza o documento com o ZapSign se possuir token
                if (!string.IsNullOrEmpty(pei.DocumentToken))
                {
                    var synced = await _zapSignService.SyncDocument(pei.DocumentToken);
                    if (synced)
                    {
                        pei = await _appDbContext.PEIs.Include(s => s.Student)
                                                                    .Include(s => s.Student!.School)
                                                                    .Include(u => u.User)
                                                                    .Include(p => p.Subjects).ThenInclude(r => r.Responses)
                                                                    .AsNoTracking()
                                                                    .FirstOrDefaultAsync(s => s.ID == id);
                        if (pei == null)
                        {
                            return NotFound(new ApiResponse<PEIResponse>()
                            {
                                Success = false,
                                Message = "PEI não encontrado após sincronização"
                            });
                        }
                    }
                }

                if (string.IsNullOrEmpty(pei.SignUrl))
                {
                    var resultDocCreation = await _zapSignService.CreateDoc(pei.ID, "pei");

                    if (!resultDocCreation)
                    {
                        _logger.LogWarning($"Documento no ZapSign não foi criado para o PEI ID {pei.ID}.");
                        var userSummaryWithoutSign = await _userSummaryFactory.CreateAsync(pei.User);
                        var responseWithoutSign = PEIMapper.ToResponse(pei, userSummaryWithoutSign);

                        return StatusCode(207, new ApiResponse<object>()
                        {
                            Success = true,
                            Message = "PEI retornado com sucesso, mas não foi possível criar o documento para assinatura.",
                            Data = userSummaryWithoutSign!
                        });
                    }

                    pei = await _appDbContext.PEIs.Include(s => s.Student)
                                                  .Include(s => s.Student!.School)
                                                  .Include(u => u.User)
                                                  .Include(p => p.Subjects).ThenInclude(r => r.Responses)
                                                  .AsNoTracking()
                                                  .FirstOrDefaultAsync(s => s.ID == id);

                    if (pei == null)
                    {
                        return NotFound(new ApiResponse<PEIResponse>()
                        {
                            Success = false,
                            Message = "PEI não encontrado"
                        });
                    }
                }
      
                var userSummary = await _userSummaryFactory.CreateAsync(pei.User);
                var response = PEIMapper.ToResponse(pei, userSummary);

                return Ok(new ApiResponse<PEIResponse?>()
                {
                    Success = true,
                    Message = "PEI retornado com sucesso",
                    Data = response,
                });
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao retornar PEI {PEIId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Ocorreu um erro ao retornar PEI"
                });
            }
        }

        [HttpGet("student/{studentID}")]
        public async Task<IActionResult> GetPEIsByStudent(Guid studentID)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(CurrentUserId);
                if (user == null) return Unauthorized();

                var roles = await _userManager.GetRolesAsync(user);
                var isAdmin = roles.Contains("Admin");

                var student = await _appDbContext.Students.Include(s => s.School).FirstOrDefaultAsync(s => s.ID == studentID);
                if (student == null)
                {
                    return NotFound(new ApiResponse<object> { Success = false, Message = "Estudante não encontrado" });
                }

                // Security check
                if (!HasStudentAccess(user, student, roles)) return Forbid();

                var peis = await _appDbContext.PEIs.Include(s => s.Student)
                                                  .Include(s => s.Student!.School)
                                                  .Include(u => u.User)
                                                  .Include(p => p.Subjects).ThenInclude(r => r.Responses)
                                                  .AsNoTracking()
                                                  .AsSplitQuery()
                                                  .Where(temp => temp.StudentID == studentID).ToListAsync();

                if (peis.Count == 0)
                {
                    _logger.LogWarning(
                        "Usuário {UserId} tentou acessar PEI do estudante {StudentId} não encontrado",
                        CurrentUserId,
                        studentID
                    );
                    return NotFound(new ApiResponse<List<PEIResponse>>()
                    {
                        Success = false,
                        Message = "PEIs não encontrados"
                    });
                }

                foreach (var pei in peis)
                {
                    if (string.IsNullOrEmpty(pei.SignUrl))
                    {
                        var resultDocCreation = await _zapSignService.CreateDoc(pei.ID, "pei");

                        //if (!resultDocCreation)
                        //{
                        //    _logger.LogWarning($"Documento no ZapSign não foi criado para o PEI ID {pei.ID}.");
                        //    var userSummaryWithoutSign = await _userSummaryFactory.CreateAsync(pei.User);
                        //    var responseWithoutSign = PEIMapper.ToResponse(pei, userSummaryWithoutSign);

                        //    return StatusCode(207, new ApiResponse<object>()
                        //    {
                        //        Success = true,
                        //        Message = "PEI retornado com sucesso, mas não foi possível criar o documento para assinatura.",
                        //        Data = userSummaryWithoutSign!
                        //    });
                        //}
                    }
                }

                var users = peis.Where(q => q.User != null)
                                          .Select(q => q.User!)
                                          .DistinctBy(u => u.Id)
                                          .ToList();

                var userSummaries = await _userSummaryFactory.CreateManyAsync(users);

                var response = peis.Select(q =>
                    PEIMapper.ToResponse(
                        q,
                        q.User != null && userSummaries.TryGetValue(q.User.Id, out var us)
                            ? us
                            : null
                    )
                ).ToList();

                return Ok(new ApiResponse<List<PEIResponse>>()
                {
                    Success = true,
                    Message = "PEI retornado com sucesso",
                    Data = response,
                });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao retornar PEIs do estudante {StudentId}", studentID);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Ocorreu um erro ao retornar PEI"
                });
            }
        }

        #endregion

        #region UPDATE
        [Authorize(Policy = "AdminOrTeacher")]
        [HttpPut]
        public async Task<IActionResult> UpdatePEIs([FromBody] PEIUpdateRequest model)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem();
            }

            using var transaction = await _appDbContext.Database.BeginTransactionAsync();

            try
            {
                var questionnaire = await _appDbContext.PEIs.Include(s => s.Subjects).ThenInclude(r => r.Responses).FirstOrDefaultAsync(s => s.ID == model.ID);

                if (questionnaire == null)
                {
                    _logger.LogWarning(
                        "Usuário {UserId} tentou acessar PEI {PEIId} não encontrado",
                        CurrentUserId,
                        model.ID
                    );
                    return NotFound(new ApiResponse<object>()
                    {
                        Success = false,
                        Message = "PEI não encontrado"
                    });
                }

                if (User.IsInRole("Teacher") && !string.Equals(questionnaire.UserID, CurrentUserId, StringComparison.Ordinal))
                {
                    return Forbid();
                }

                PEIMapper.UpdateEntity(questionnaire, model);

                await _appDbContext.Logs.AddAsync(new Log
                {
                    LogLevel = Models.Enums.LogLevel.Info.ToString(),
                    Message = $"PEI atualizado com sucesso com ID {questionnaire.ID}",
                    Source = nameof(PEIsController),
                    UserID = CurrentUserGuid,
                    EntityType = nameof(PEI),
                    EntityID = questionnaire.ID,
                    Details = "",
                    Action = "Update"
                });

                await _appDbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                await _zapSignService.DeleteDoc(questionnaire.ID, "pei");

                _logger.LogInformation($"Documento no ZapSign excluído com sucesso para o PEI ID {questionnaire.ID}.");

                bool resultDocCreation;
                try
                {
                    resultDocCreation = await _zapSignService.CreateDoc(questionnaire.ID, "pei");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao criar documento no ZapSign para o PEI ID {PeiId}", questionnaire.ID);
                    resultDocCreation = false;
                }

                if (!resultDocCreation)
                {
                    _logger.LogWarning($"Documento no ZapSign não foi criado para o PEI ID {questionnaire.ID}.");

                    return StatusCode(207, new ApiResponse<object>()
                    {
                        Success = true,
                        Message = "PEI atualizado com sucesso, mas não foi possível criar o documento para assinatura.",
                        Data = new
                        {
                            questionnaire.ID
                        }
                    });
                }

                return Ok(new ApiResponse<object>()
                {
                    Success = true,
                    Message = "PEI atualizado com sucesso",
                    Data = new
                    {
                        questionnaire.ID,
                    }
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Erro ao atualizar PEI com ID {Id}", model.ID);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Ocorreu um erro interno ao atualizar o PEI"
                });
            }
        }

        [Authorize(Policy = "AdminOrTeacher")]
        [HttpPut("{id}/subjects")]
        public async Task<IActionResult> AddSubjectsToPEI([FromBody] PEISubjectRequest model, Guid id)
        {
            try
            {
                var pei = await _appDbContext.PEIs.Include(s => s.Student)
                                                            .Include(s => s.Student!.School)
                                                            .Include(u => u.User)
                                                            .Include(p => p.Subjects).ThenInclude(r => r.Responses)
                                                            .AsNoTracking()
                                                            .FirstOrDefaultAsync(s => s.ID == id);

                if (pei == null)
                {
                    return NotFound(new ApiResponse<PEIResponse>
                    {
                        Success = false,
                        Message = "PEI não encontrado"
                    });
                }

                var plan = PEIMapper.ToSubjectEntity(model, id);
                await _appDbContext.PEI_Subjects.AddAsync(plan);

                await _appDbContext.Logs.AddAsync(new Log
                {
                    LogLevel = Models.Enums.LogLevel.Info.ToString(),
                    Message = $"PEI criado com sucesso com ID {plan.ID}",
                    Source = nameof(PEIsController),
                    UserID = CurrentUserGuid,
                    EntityType = nameof(PEI),
                    EntityID = plan.ID,
                    Details = "",
                    Action = "Create"
                });

                await _appDbContext.SaveChangesAsync();

                await _notificationService.CreateNotificationAdmins(
                    type: TypeNotification.System,
                    title: "Nova disciplina adiiconada ao PEI!",
                    message: $"Uma nova disciplina foi adicionada ao PEI com ID {id}.",
                    actionURL: $"api/v1/peis/{id}/subjects"
                );

                return StatusCode(201, new ApiResponse<object>()
                {
                    Success = true,
                    Message = "PEI atualizado com sucesso",
                    Data = new
                    {
                        pei.ID
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao adiconar disciplina ao PEI");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Ocorreu um erro ao adicionar nova disciplina ao PEI"
                });
            }
        }
        #endregion

        #region DELETE
        [Authorize(Policy = "AdminOrTeacher")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePEIs(Guid id)
        {
            try
            {
                var questionnaire = await _appDbContext.PEIs.FirstOrDefaultAsync(temp => temp.ID == id);

                if (questionnaire == null)
                {
                    _logger.LogWarning(
                        "Usuário {UserId} tentou acessar PEI {PEIId} não encontrado",
                        CurrentUserId,
                        id
                    );
                    return NotFound(new ApiResponse<object>()
                    {
                        Success = false,
                        Message = "PEI não encontrado"
                    });
                }

                if (User.IsInRole("Teacher") && !string.Equals(questionnaire.UserID, CurrentUserId, StringComparison.Ordinal))
                {
                    return Forbid();
                }

                _appDbContext.PEIs.Remove(questionnaire);

                await _appDbContext.Logs.AddAsync(new Log
                {
                    LogLevel = Models.Enums.LogLevel.Info.ToString(),
                    Message = $"PEI excluído com sucesso com ID {questionnaire.ID}",
                    Source = nameof(PEIsController),
                    UserID = CurrentUserGuid,
                    EntityType = nameof(PEI),
                    EntityID = questionnaire.ID,
                    Details = "",
                    Action = "Delete"
                });

                await _appDbContext.SaveChangesAsync();

                var resultDocDelete = await _zapSignService.DeleteDoc(questionnaire.ID, "pei");

                if (!resultDocDelete)
                {
                    _logger.LogWarning($"Documento no ZapSign não foi excluído para o PEI ID {questionnaire.ID}.");

                    return StatusCode(207, new ApiResponse<object>()
                    {
                        Success = true,
                        Message = "PEI excluído com sucesso, mas não foi possível excluir o documento.",
                        Data = new
                        {
                            questionnaire.ID
                        }
                    });
                }

                _logger.LogInformation($"Documento no ZapSign excluído com sucesso para o PEI ID {questionnaire.ID}.");

                return Ok(new ApiResponse<object>()
                {
                    Success = true,
                    Message = "PEI excluído com sucesso"
                });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao excluir PEI com id {id}");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Ocorreu um erro ao excluir a PEI com id {id}"
                });
            }
        }
        #endregion
    }
}
