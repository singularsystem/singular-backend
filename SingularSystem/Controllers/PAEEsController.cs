using SingularSystem.Domain.Tenancy;
using SingularSystem.DTOs.Requests;
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
using System.Security.Claims;

namespace SingularSystem.Controllers
{
    [Authorize]
    [Route("api/v1/[controller]")]
    [ApiController]
    public class PAEEsController : BaseController
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _appDbContext;
        private readonly NotificationService _notificationService;
        private readonly ISignatureService _zapSignService;
        private readonly ILogger<PAEEsController> _logger;
        private readonly IUserSummaryFactory _userSummaryFactory;
        private readonly ITenantContext _tenantContext;

        public PAEEsController(AppDbContext appDbContext, ILogger<PAEEsController> logger, NotificationService notificationService, UserManager<ApplicationUser> userManager, ISignatureService zapSignService, IUserSummaryFactory userSummaryFactory, ITenantContext tenantContext)
        {
            _appDbContext = appDbContext;
            _logger = logger;
            _notificationService = notificationService;
            _userManager = userManager;
            _zapSignService = zapSignService;
            _userSummaryFactory = userSummaryFactory;
            _tenantContext = tenantContext;
        }

        private IQueryable<PAEE> BuildPaeeQuery(
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

            var query = _appDbContext.PAEEs
                                    .Include(st => st.Student).ThenInclude(s => s!.School)
                                    .Include(p => p.Portage).ThenInclude(s => s.Student)
                                    .Include(u => u.User).ThenInclude(s => s!.School)
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
                query = query.Where(s => s.Student != null && (s.Student.State == state || (s.Student.School != null && s.Student.School.State == state)));

            if (!string.IsNullOrWhiteSpace(city))
                query = query.Where(s => s.Student != null && (s.Student.City == city || (s.Student.School != null && s.Student.School.City == city)));

            if (!string.IsNullOrWhiteSpace(schoolID))
            {
                if (Guid.TryParse(schoolID, out var sid))
                    query = query.Where(s => s.Student != null && s.Student.SchoolID == sid);
                else
                    query = query.Where(_ => false); 
            }

            if (!isAdmin)
            {
                if (isTeacher)
                {
                    query = query.Where(p => p.UserID == CurrentUserId || 
                                             (p.Student != null && 
                                              ((p.Student.City == currentUser.City && p.Student.State == currentUser.State) ||
                                               (p.Student.School != null && p.Student.School.City == currentUser.City && p.Student.School.State == currentUser.State))));
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

        private IQueryable<PAEE> ApplyMyPAEEsFilter(
            IQueryable<PAEE> query,
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
                        (p.DocumentToken == null || p.DocumentToken == "")
                    );
                }
                else if (myStatus.Equals("pending_sign", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(p =>
                        p.CreatedAt >= cutoffDate &&
                        !p.TeacherSign &&
                        (p.SignedFile == null || p.SignedFile == "") &&
                        p.DocumentStatus != Status.Complete &&
                        p.DocumentToken != null && p.DocumentToken != ""
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

        [HttpPost]
        [Authorize(Policy = "AdminOrTeacher")]
        public async Task<IActionResult> CreatePAEE([FromBody] PAEERequest model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return ValidationProblem();
                }

                var student = await _appDbContext.Students.FirstOrDefaultAsync(temp => temp.ID == model.StudentID);

                if (student == null)
                {
                    return NotFound(new ApiResponse<object>()
                    {
                        Success = false,
                        Message = $"Estudante com ID {model.StudentID} não foi encontrado!"
                    });
                }

                var paee = PAEEMapper.ToEntity(model, CurrentUserId);
                if (_tenantContext.IsPrivatePlan)
                    paee.TenantId = _tenantContext.TenantId;

                await _appDbContext.PAEEs.AddAsync(paee);
                await _appDbContext.SaveChangesAsync();

                await _appDbContext.Logs.AddAsync(new Log
                {
                    LogLevel = Models.Enums.LogLevel.Info.ToString(),
                    Message = $"PAEE criado com sucesso com ID {paee.ID}",
                    Source = "PAEEController",
                    UserID = CurrentUserGuid,
                    EntityType = "PAEE",
                    EntityID = paee.ID,
                    Details = "",
                    Action = "Create"
                });
                await _appDbContext.SaveChangesAsync();

                var resultDocCreation = await _zapSignService.CreateDoc(paee.ID, "paee");

                if (!resultDocCreation)
                {
                    _logger.LogWarning($"Documento no ZapSign não foi criado para o PAEE ID {paee.ID}.");

                    return StatusCode(207, new ApiResponse<object>()
                    {
                        Success = true,
                        Message = "PAEE salvo com sucesso, mas não foi possível criar o documento para assinatura.",
                        Data = new
                        {
                            paee.ID
                        }
                    });
                }

                await _notificationService.CreateNotificationAdmins(
                    type: TypeNotification.System,
                    title: "Novo PAEE criado!",
                    message: "Um novo PAEE foi criado e já está disponível para assinatura.",
                    actionURL: "api/v1/paees"
                );

                _logger.LogInformation($"Documento no ZapSign criado com sucesso para o PAEE ID {paee.ID}.");

                var usersInRole = await _userManager.GetUsersInRoleAsync("Teacher");
                var filteredUser = usersInRole
                    .FirstOrDefault(u => u.Id == paee?.User?.Id);

                if (filteredUser != null)
                {
                    await _notificationService.CreateNotification(
                        userId: filteredUser.Id,
                        type: TypeNotification.System,
                        title: "Novo PAEE criado!",
                        message: "Um novo PAEE foi criado e já está disponível para assinatura.",
                        actionURL: "api/v1/paees"
                    );
                }

                return StatusCode(201, new ApiResponse<object>()
                {
                    Success = true,
                    Message = "PAEE criado com sucesso",
                    Data = new
                    {
                        paee.ID
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(new ApiResponse<object>()
                {
                    Success = false,
                    Message = $"Ocorreu um erro ao criar PAEE. {ex.Message}"
                });

            }
        }

        #endregion

        #region GET
        [HttpGet("my-summary")]
        public async Task<IActionResult> GetMyPAEESummary(
            [FromQuery] string state = "",
            [FromQuery] string city = "",
            [FromQuery] string schoolID = "",
            [FromQuery] string searchTerm = "")
        {
            try
            {
                var user = await _userManager.FindByIdAsync(CurrentUserId);
                if (user == null) return Unauthorized();

                var baseQuery = BuildPaeeQuery(user, state, city, schoolID, searchTerm);

                var total = await ApplyMyPAEEsFilter(baseQuery, user, "all").CountAsync();
                var pendingFill = await ApplyMyPAEEsFilter(baseQuery, user, "pending_fill").CountAsync();
                var pendingSign = await ApplyMyPAEEsFilter(baseQuery, user, "pending_sign").CountAsync();
                var signed = await ApplyMyPAEEsFilter(baseQuery, user, "signed").CountAsync();

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
                _logger.LogError(ex, "Erro ao obter resumo de PAEEs do usuário");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Erro ao obter resumo de PAEEs do usuário"
                });
            }
        }
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetPAEEs([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string state = "", [FromQuery] string city = "", [FromQuery] string schoolID = "", [FromQuery] string searchTerm = "", [FromQuery] bool onlyMyDocs = false, [FromQuery] bool onlyMyReports = false, [FromQuery] string myStatus = "")
        {
            try
            {
                const int maxPageSize = 50;
                pageSize = Math.Min(pageSize, maxPageSize);
                page = Math.Max(page, 1);
                var skip = (page - 1) * pageSize;

                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var currentUser = await _appDbContext.Users.Include(u => u.School)
                                                           .FirstOrDefaultAsync(u => u.Id == currentUserId);

                if (currentUser == null)
                {
                    return Unauthorized();
                }

                var query = BuildPaeeQuery(currentUser, state, city, schoolID, searchTerm);

                if (onlyMyDocs || onlyMyReports)
                {
                    query = ApplyMyPAEEsFilter(query, currentUser, myStatus);
                }

                if (!query.Any())
                {
                    return Ok(new ApiResponse<List<PAEEResponse>>
                    {
                        Success = true,
                        Message = "Nenhum PAEE foi cadastrado ainda"
                    });
                }

                var totalPAEEs = await query.CountAsync();
                var uniqueTotal = await query
                    .Where(d => d.StudentID.HasValue)
                    .Select(d => d.StudentID!.Value)
                    .Distinct()
                    .CountAsync();
                var paees = await query.OrderByDescending(f => f.CreatedAt)
                                      .Skip(skip)
                                      .Take(pageSize)
                                      .ToListAsync();

                if (paees == null || paees.Count == 0)
                {
                    return Ok(new ApiResponse<List<PAEESummaryResponse>>()
                    {
                        Success = true,
                        Message = "Nenhum PAEE encontrado",
                        Data = new List<PAEESummaryResponse>(),
                        Total = totalPAEEs,
                        UniqueTotal = uniqueTotal
                    });
                }

                var users = paees.Where(q => q.User != null)
                                          .Select(q => q.User!)
                                          .DistinctBy(u => u.Id)
                                          .ToList();

                var userSummaries = await _userSummaryFactory.CreateManyAsync(users);

                var response = paees.Select(q =>
                    PAEEMapper.ToSummaryResponse(
                        q,
                        q.User != null && userSummaries.TryGetValue(q.User.Id, out var us)
                            ? us
                            : null
                    )
                ).ToList();

                return Ok(new ApiResponse<List<PAEESummaryResponse>>()
                {
                    Success = true,
                    Message = "Lista de PAEEs retornada com sucesso",
                    Data = response,
                    Total = totalPAEEs,
                    UniqueTotal = uniqueTotal
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(new ApiResponse<object>()
                {
                    Success = false,
                    Message = "Ocorreu um erro ao retornar os PAEEs"
                });

            }
        }

        [Authorize]
        [HttpGet("all")]
        public async Task<IActionResult> GetAllPAEEsNoPaging([FromQuery] string state = "", [FromQuery] string city = "", [FromQuery] string schoolID = "", [FromQuery] string searchTerm = "", [FromQuery] bool onlyMyDocs = false, [FromQuery] bool onlyMyReports = false, [FromQuery] string myStatus = "")
        {
            try
            {
                var user = await _userManager.FindByIdAsync(CurrentUserId);

                if (user == null)
                {
                    return Unauthorized();
                }

                var query = BuildPaeeQuery(user, state, city, schoolID, searchTerm);

                if (onlyMyDocs || onlyMyReports)
                {
                    query = ApplyMyPAEEsFilter(query, user, myStatus);
                }

                if (!query.Any())
                {
                    return Ok(new ApiResponse<List<PAEEResponse>>
                    {
                        Success = true,
                        Message = "Nenhum PAEE foi cadastrado ainda"
                    });
                }

                var totalPAEEs = await query.CountAsync();
                var uniqueTotal = await query
                    .Where(d => d.StudentID.HasValue)
                    .Select(d => d.StudentID!.Value)
                    .Distinct()
                    .CountAsync();
                var paees = await query.OrderByDescending(f => f.CreatedAt).ToListAsync();

                if (paees == null || paees.Count == 0)
                {
                    return Ok(new ApiResponse<List<PAEESummaryResponse>>()
                    {
                        Success = true,
                        Message = "Nenhum PAEE encontrado",
                        Data = new List<PAEESummaryResponse>(),
                        Total = totalPAEEs,
                        UniqueTotal = uniqueTotal
                    });
                }

                var users = paees.Where(q => q.User != null)
                                          .Select(q => q.User!)
                                          .DistinctBy(u => u.Id)
                                          .ToList();

                var userSummaries = await _userSummaryFactory.CreateManyAsync(users);

                var response = paees.Select(q =>
                    PAEEMapper.ToSummaryResponse(
                        q,
                        q.User != null && userSummaries.TryGetValue(q.User.Id, out var us)
                            ? us
                            : null
                    )
                ).ToList();

                return Ok(new ApiResponse<List<PAEESummaryResponse>>()
                {
                    Success = true,
                    Message = "Lista de PAEEs retornada com sucesso",
                    Data = response,
                    Total = totalPAEEs,
                    UniqueTotal = uniqueTotal
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(new ApiResponse<object>()
                {
                    Success = false,
                    Message = "Ocorreu um erro ao retornar os PAEEs"
                });

            }
        }

        [Authorize]
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetPAEE(Guid id)
        {
            try
            {
                var paee = await _appDbContext.PAEEs.IgnoreQueryFilters()
                                                    .Include(s => s.Student)
                                                    .Include(s => s.Student!.School)
                                                    .Include(s => s.User)
                                                    .Include(p => p.Portage).ThenInclude(r => r.Responses)
                                                    .Include(s => s.OrganizationOfCare)
                                                    .Include(s => s.OtherProfessionals)
                                                    .Include(s => s.DifferentiatedActivitiesInAEE)
                                                    .Include(s => s.WorkMethodology)
                                                    .Include(s => s.MaterialResourcesAndEquipment)
                                                    .Include(s => s.CriteriaInEvaluation)
                                                    .Include(s => s.ResourcesAndStrategies)
                                                    .AsSplitQuery()
                                                    .FirstOrDefaultAsync(temp => temp.ID == id);

                if (paee == null)
                {
                    return NotFound(new ApiResponse<object>()
                    {
                        Success = false,
                        Message = $"PAEE com ID {id} não encontrado"
                    });
                }

                // Sincroniza o documento com o serviço de assinatura se possuir token e não estiver completo
                if (!string.IsNullOrEmpty(paee.DocumentToken) && (!paee.TeacherSign || paee.DocumentStatus != Status.Complete))
                {
                    var synced = await _zapSignService.SyncDocument(paee.DocumentToken);
                    if (synced)
                    {
                        paee = await _appDbContext.PAEEs.IgnoreQueryFilters()
                                                        .Include(s => s.Student)
                                                        .Include(s => s.Student!.School)
                                                        .Include(s => s.User)
                                                        .Include(p => p.Portage).ThenInclude(r => r.Responses)
                                                        .Include(s => s.OrganizationOfCare)
                                                        .Include(s => s.OtherProfessionals)
                                                        .Include(s => s.DifferentiatedActivitiesInAEE)
                                                        .Include(s => s.WorkMethodology)
                                                        .Include(s => s.MaterialResourcesAndEquipment)
                                                        .Include(s => s.CriteriaInEvaluation)
                                                        .Include(s => s.ResourcesAndStrategies)
                                                        .AsSplitQuery()
                                                        .FirstOrDefaultAsync(temp => temp.ID == id);

                        if (paee == null)
                        {
                            return NotFound(new ApiResponse<object>()
                            {
                                Success = false,
                                Message = $"PAEE com ID {id} não encontrado após sincronização"
                            });
                        }
                    }
                }

                if (string.IsNullOrEmpty(paee.SignUrl))
                {
                    var resultDocCreation = await _zapSignService.CreateDoc(paee.ID, "paee");

                    if (!resultDocCreation)
                    {
                        _logger.LogWarning($"Documento no ZapSign não foi criado para o PAEE ID {paee.ID}.");
                        var userSummaryWithoutSign = await _userSummaryFactory.CreateAsync(paee.User);
                        var responseWithoutSign = PAEEMapper.ToResponse(paee, userSummaryWithoutSign);

                        return StatusCode(207, new ApiResponse<object>()
                        {
                            Success = true,
                            Message = "PAEE retornado com sucesso, mas não foi possível criar o documento para assinatura.",
                            Data = userSummaryWithoutSign!
                        });
                    }
                }

                var userSummary = await _userSummaryFactory.CreateAsync(paee.User);
                var response = PAEEMapper.ToResponse(paee, userSummary);

                return Ok(new ApiResponse<PAEEResponse>()
                {
                    Success = true,
                    Message = "PAEE retornado com sucesso",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(new ApiResponse<object>()
                {
                    Success = false,
                    Message = "Ocorreu um erro ao retornar o PAEE"
                });

            }
        }

        [Authorize]
        [HttpGet("student/{studentID}")]
        public async Task<IActionResult> GetPAEEsByStudent(Guid studentID)
        {
            try
            {
                var paees = await _appDbContext.PAEEs.Include(s => s.Student)
                                                     .Include(s => s.Student!.School)
                                                     .Include(s => s.OrganizationOfCare)
                                                     .Include(s => s.OtherProfessionals)
                                                     .Include(s => s.DifferentiatedActivitiesInAEE)
                                                     .Include(s => s.WorkMethodology)
                                                     .Include(s => s.MaterialResourcesAndEquipment)
                                                     .Include(s => s.CriteriaInEvaluation)
                                                     .Include(s => s.ResourcesAndStrategies)
                                                     .AsSplitQuery()
                                                     .Where(temp => temp.StudentID == studentID).ToListAsync();

                if (paees.Count == 0)
                {
                    return Ok(new ApiResponse<List<PAEEResponse>>()
                    {
                        Success = true,
                        Message = $"Não foi encontrado nenhum PAEE para estudante com ID {studentID}"
                    });
                }

                var users = paees.Where(q => q.User != null)
                                          .Select(q => q.User!)
                                          .DistinctBy(u => u.Id)
                                          .ToList();

                var userSummaries = await _userSummaryFactory.CreateManyAsync(users);

                var response = paees.Select(q =>
                    PAEEMapper.ToResponse(
                        q,
                        q.User != null && userSummaries.TryGetValue(q.User.Id, out var us)
                            ? us
                            : null
                    )
                ).ToList();

                return Ok(new ApiResponse<List<PAEEResponse>>()
                {
                    Success = true,
                    Message = "Lista de PAEEs retornada com sucesso",
                    Data = response,
                    Total = response.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(new ApiResponse<object>()
                {
                    Success = false,
                    Message = "Ocorreu um erro ao retornar o PAEE"
                });

            }
        }

        #endregion

        #region UPDATE

        [Authorize(Policy = "AdminOrTeacher")]
        [HttpPut]
        public async Task<IActionResult> UpdatePAEE([FromBody] PAEEUpdateRequest model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return ValidationProblem();
                }

                var paee = await _appDbContext.PAEEs.Include(s => s.OrganizationOfCare)
                                                  .Include(s => s.OtherProfessionals)
                                                  .Include(s => s.DifferentiatedActivitiesInAEE)
                                                  .Include(s => s.WorkMethodology)
                                                  .Include(s => s.MaterialResourcesAndEquipment)
                                                  .Include(s => s.CriteriaInEvaluation)
                                                  .Include(s => s.ResourcesAndStrategies)
                                                  .AsSplitQuery()
                                                  .FirstOrDefaultAsync(temp => temp.ID == model.ID);

                if (paee == null)
                {
                    return NotFound(new ApiResponse<object>()
                    {
                        Success = false,
                        Message = $"Nenhum PAEE foi encontrado com ID {model.ID}"
                    });
                }

                _appDbContext.PAEE_TypeAEE.RemoveRange(paee.OrganizationOfCare);
                _appDbContext.PAEE_ProfessionalsAEE.RemoveRange(paee.OtherProfessionals);
                _appDbContext.PAEE_ActivitiesAEE.RemoveRange(paee.DifferentiatedActivitiesInAEE);
                _appDbContext.PAEE_WorkMethodologyAEE.RemoveRange(paee.WorkMethodology);
                _appDbContext.PAEE_MaterialResourcesAEE.RemoveRange(paee.MaterialResourcesAndEquipment);
                _appDbContext.PAEE_EvaluationCriterionAEE.RemoveRange(paee.CriteriaInEvaluation);
                _appDbContext.PAEE_ResourcesAndStrategiesAEE.RemoveRange(paee.ResourcesAndStrategies);

                PAEEMapper.UpdateEntity(paee, model);

                _appDbContext.PAEEs.Update(paee);
                await _appDbContext.SaveChangesAsync();

                await _appDbContext.Logs.AddAsync(new Log
                {
                    LogLevel = Models.Enums.LogLevel.Info.ToString(),
                    Message = $"PAEE atualizado com sucesso com ID {paee.ID}",
                    Source = "PAEEController",
                    UserID = CurrentUserGuid,
                    EntityType = "PAEE",
                    EntityID = paee.ID,
                    Details = "",
                    Action = "Update"
                });
                await _appDbContext.SaveChangesAsync();

                await _zapSignService.DeleteDoc(paee.ID, "paee");

                _logger.LogInformation($"Documento no ZapSign excluído com sucesso para o PAEE ID {paee.ID}.");

                var resultDocCreation = await _zapSignService.CreateDoc(paee.ID, "paee");

                if (!resultDocCreation)
                {
                    _logger.LogWarning($"Documento no ZapSign não foi criado para o PAEE ID {paee.ID}.");

                    return StatusCode(207, new ApiResponse<object>()
                    {
                        Success = true,
                        Message = "PAEE atualizado com sucesso, mas não foi possível criar o documento para assinatura.",
                        Data = new
                        {
                            paee.ID
                        }
                    });
                }

                await _notificationService.CreateNotificationAdmins(
                    type: TypeNotification.System,
                    title: "PAEE atualizado!",
                    message: "Um PAEE foi atualizado e já está disponível para assinatura.",
                    actionURL: "api/v1/paee"
                );

                _logger.LogInformation($"Documento no ZapSign criado com sucesso para o PAEE ID {paee.ID}.");

                var usersInRole = await _userManager.GetUsersInRoleAsync("Teacher");
                var filteredUser = usersInRole
                    .FirstOrDefault(u => u.Id == paee?.User?.Id);

                if (filteredUser != null)
                {
                    await _notificationService.CreateNotification(
                        userId: filteredUser.Id,
                        type: TypeNotification.System,
                        title: "PAEE atualizado!",
                        message: "Um PAEE foi atualizado e já está disponível para assinatura.",
                        actionURL: "api/v1/paee"
                    );
                }

                return Ok(new ApiResponse<object>()
                {
                    Success = true,
                    Message = "PAEE atualizado com sucesso",
                    Data =
                    new
                    {
                        paee.ID,
                        paee.StudentID,
                        paee.UserID,
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>()
                {
                    Success = false,
                    Message = $"Ocorreu um erro ao atualizar PAEE. {ex.Message}"
                });
            }
        }

        #endregion

        #region DELETE
        [Authorize(Policy = "AdminOrTeacher")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePAEE(Guid id)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return ValidationProblem();
                }

                var paee = await _appDbContext.PAEEs.Include(p => p.Portage).FirstOrDefaultAsync(temp => temp.ID == id);

                if (paee == null)
                {
                    return NotFound(new ApiResponse<object>()
                    {
                        Success = false,
                        Message = $"Nenhum PAEE foi encontrado com ID {id}"
                    });
                }

                if (paee.Portage != null)
                {
                    _appDbContext.PortageGuides.Remove(paee.Portage);
                }

                _appDbContext.PAEEs.Remove(paee);
                await _appDbContext.SaveChangesAsync();

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                await _appDbContext.Logs.AddAsync(new Log
                {
                    LogLevel = Models.Enums.LogLevel.Info.ToString(),
                    Message = $"PAEE excluído com sucesso com ID {paee.ID}",
                    Source = "PAEEController",
                    UserID = Guid.Parse(userId!),
                    EntityType = "PAEE",
                    EntityID = paee.ID,
                    Details = "",
                    Action = "Delete"
                });
                await _appDbContext.SaveChangesAsync();

                await _notificationService.CreateNotificationAdmins(
                    type: TypeNotification.System,
                    title: "PAEE excluído!",
                    message: "Um PAEE foi removido com sucesso.",
                    actionURL: "api/v1/paee"
                );

                var resultDocDelete = await _zapSignService.DeleteDoc(paee.ID, "paee");

                if (!resultDocDelete)
                {
                    _logger.LogWarning($"Documento no ZapSign não foi excluído para o PAEE ID {paee.ID}.");

                    return StatusCode(207, new ApiResponse<object>()
                    {
                        Success = true,
                        Message = "PAEE excluído com sucesso, mas não foi possível excluir o documento.",
                        Data = new
                        {
                            paee.ID
                        }
                    });
                }

                _logger.LogInformation($"Documento no ZapSign excluído com sucesso para o PAEE ID {paee.ID}.");

                return Ok(new ApiResponse<object>()
                {
                    Success = true,
                    Message = "PAEE excluído com sucesso"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>()
                {
                    Success = false,
                    Message = $"Ocorreu um erro ao excluir o PAEE. {ex.Message}"
                });
            }
        }

        #endregion
    }
}
