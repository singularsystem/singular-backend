using SingularSystem.DTOs.Portage;
using SingularSystem.Extensions;
using SingularSystem.Interfaces;
using SingularSystem.Mappers;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;
using SingularSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using static SingularSystem.Models.Helpers;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace SingularSystem.Controllers
{
    [Authorize]
    [Route("api/v1/[controller]")]
    [ApiController]
    public class PortageController : BaseController
    {
        private readonly AppDbContext _appDbContext;
        private readonly NotificationService _notificationService;
        private readonly ILogger<PortageController> _logger;
        private readonly IUserSummaryFactory _userSummaryFactory;

        public PortageController(AppDbContext appDbContext, ILogger<PortageController> logger, NotificationService notificationService, IUserSummaryFactory userSummaryFactory)
        {
            _appDbContext = appDbContext;
            _logger = logger;
            _notificationService = notificationService;
            _userSummaryFactory = userSummaryFactory;
        }

        #region CREATE
        [Authorize(Policy = "AdminOrTeacher")]
        [HttpPost]
        public async Task<IActionResult> CreatePortage([FromBody] PortageRequest model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return ValidationProblem();
                }

                var portage = PortageGuideMapper.ToEntity(model, CurrentUserId);

                await _appDbContext.PortageGuides.AddAsync(portage);
                await _appDbContext.SaveChangesAsync();

                await _appDbContext.Logs.AddAsync(new Log
                {
                    LogLevel = Models.Enums.LogLevel.Info.ToString(),
                    Message = $"Guia Portage criado com sucesso com ID {portage.ID}",
                    Source = "ReportsController",
                    UserID = CurrentUserGuid,
                    EntityType = "PortageGuide",
                    EntityID = portage.ID,
                    Details = "",
                    Action = "Create"
                });
                await _appDbContext.SaveChangesAsync();

                await _notificationService.CreateNotificationAdmins(
                    type: TypeNotification.System,
                    title: "Novo Guia Portage criado!",
                    message: "Um novo Guia Portage foi criado com sucesso.",
                    actionURL: "api/v1/portage"
                );

                return StatusCode(201, new ApiResponse<object>()
                {
                    Success = true,
                    Message = "Guia Portage criado com sucesso",
                    Data = new
                    {
                        portage.ID
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>()
                {
                    Success = false,
                    Message = $"Ocorreu um erro ao criar o guia Portage. {ex.Message}"
                });
            }
        }

        #endregion

        #region GET
        [HttpGet]
        public async Task<IActionResult> GetPortageGuides([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchTerm = "")
        {
            try
            {
                const int maxPageSize = 50;
                pageSize = Math.Min(pageSize, maxPageSize);
                page = Math.Max(page, 1);
                var skip = (page - 1) * pageSize;

                var currentUser = await _appDbContext.Users.Include(u => u.School)
                                                           .FirstOrDefaultAsync(u => u.Id == CurrentUserId);

                if (currentUser == null)
                {
                    return Unauthorized();
                }

                var isTeacher = User.IsInRole("Teacher");
                var isCoordinator = User.IsInRole("Coordinator");
                var isPrincipal = User.IsInRole("Principal");
                var isMunicipalManager = User.IsInRole("MunicipalManager");

                var query = _appDbContext.PortageGuides.IgnoreQueryFilters()
                                                       .Include(s => s.Student)
                                                       .Include(s => s.Student!.School)
                                                       .Include(pg => pg.Responses)
                                                       .Include(u => u.User)
                                                       .AsNoTracking()
                                                       .WhereStudentInTenant(TenantContext);

                if (isTeacher && currentUser != null)
                {
                    query = query.Where(p => p.UserID == CurrentUserId);
                }

                if (isPrincipal && currentUser != null)
                {
                    query = query.Where(p => p.Student != null && p.Student.SchoolID == currentUser.SchoolID);
                }

                if ((isCoordinator || isMunicipalManager) && currentUser != null)
                {
                    query = query.Where(p => p.Student != null && p.Student.School != null && p.Student.School.City == currentUser.City && p.Student.School.State == currentUser.State);
                }

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    query = query.Where(p => p.Student != null &&
                        p.Student.FullName.Contains(searchTerm));
                }

                var totalPortages = await query.CountAsync();
                var portages = await query.OrderBy(f => f.CreatedAt)
                                      .Skip(skip)
                                      .Take(pageSize)
                                      .ToListAsync();

                var users = portages.Where(q => q.User != null)
                                          .Select(q => q.User!)
                                          .DistinctBy(u => u.Id)
                                          .ToList();

                var userSummaries = await _userSummaryFactory.CreateManyAsync(users);

                var response = portages.Select(q =>
                    PortageGuideMapper.ToResponse(
                        q,
                        q.User != null && userSummaries.TryGetValue(q.User.Id, out var us)
                            ? us
                            : null
                    )
                ).ToList();

                return base.Ok(new ApiResponse<List<DTOs.Portage.PortageResponse>>()
                {
                    Success = true,
                    Message = "Lista de guias Portage retornada com sucesso",
                    Data = response,
                    Total = totalPortages
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>()
                {
                    Success = false,
                    Message = $"Ocorreu um erro ao retornar a lista de guias Portage. {ex.Message}"
                });
            }
        }

        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPortageGuide(Guid id)
        {
            try
            {
                var portage = await _appDbContext.PortageGuides.IgnoreQueryFilters()
                                                       .Include(s => s.Student)
                                                       .Include(s => s.Student!.School).Include(pg => pg.Responses)
                                                        .AsSplitQuery()
                                                        .Include(u => u.User)
                                                        .FirstOrDefaultAsync(temp => temp.ID == id);

                if (portage == null)
                {
                    return NotFound(new ApiResponse<object>()
                    {
                        Success = false,
                        Message = $"Nenhum guia Portage foi cadastrado ainda"
                    });
                }

                var currentUser = await _appDbContext.Users.FirstOrDefaultAsync(u => u.Id == CurrentUserId);
                if (currentUser == null) return Unauthorized();
                var userManager = HttpContext?.RequestServices?.GetService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>();
                var roles = userManager != null
                    ? await userManager.GetRolesAsync(currentUser)
                    : User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

                var student = portage.Student ?? await _appDbContext.Students.IgnoreQueryFilters().AsNoTracking()
                    .Include(s => s.School)
                    .FirstOrDefaultAsync(s => s.ID == portage.StudentID);

                if (student != null && IsOutsideTenant(currentUser, student, roles))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, DocumentAccessDenied.Body());
                }

                var userSummary = await _userSummaryFactory.CreateAsync(portage.User);
                var portageResponse = PortageGuideMapper.ToResponse(portage, userSummary);

                return base.Ok(new ApiResponse<DTOs.Portage.PortageResponse>()
                {
                    Success = true,
                    Message = "Guia Portage retornado com sucesso",
                    Data = portageResponse
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>()
                {
                    Success = false,
                    Message = $"Ocorreu um erro ao retornar a lista de guias Portage. {ex.Message}"
                });
            }
        }

        [Authorize]
        [HttpGet("student/{studentID}")]
        public async Task<IActionResult> GetPortageGuidesByStudent(Guid studentID)
        {
            try
            {
                var student = await _appDbContext.Students.IgnoreQueryFilters().AsNoTracking()
                    .Include(s => s.School)
                    .FirstOrDefaultAsync(s => s.ID == studentID);

                if (student == null)
                {
                    return NotFound(new ApiResponse<List<PortageResponse>>
                    {
                        Success = false,
                        Message = $"Nenhum guia Portage foi cadastrado ainda para esse estudante"
                    });
                }

                var currentUser = await _appDbContext.Users.FirstOrDefaultAsync(u => u.Id == CurrentUserId);
                if (currentUser == null) return Unauthorized();
                var userManager = HttpContext?.RequestServices?.GetService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>();
                var roles = userManager != null
                    ? await userManager.GetRolesAsync(currentUser)
                    : User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

                if (IsOutsideTenant(currentUser, student, roles))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, DocumentAccessDenied.Body());
                }

                var isTeacher = User.IsInRole("Teacher");

                var query = _appDbContext.PortageGuides
                                        .Include(s => s.Student)
                                        .Include(u => u.User)
                                        .Include(s => s.Student!.School)
                                        .Include(pg => pg.Responses)
                                        .AsSplitQuery()
                                        .AsNoTracking()
                                        .Where(temp => temp.StudentID == studentID);

                if (isTeacher)
                {
                    query = query.Where(q => q.UserID == CurrentUserId);
                }

                var portages = await query.ToListAsync();

                if (portages.Count == 0)
                {
                    return NotFound(new ApiResponse<List<PortageResponse>>()
                    {
                        Success = false,
                        Message = $"Nenhum guia Portage foi cadastrado ainda para esse estudante"
                    });
                }

                var users = portages.Where(q => q.User != null)
                                          .Select(q => q.User!)
                                          .DistinctBy(u => u.Id)
                                          .ToList();

                var userSummaries = await _userSummaryFactory.CreateManyAsync(users);

                var response = portages.Select(q =>
                    PortageGuideMapper.ToResponse(
                        q,
                        q.User != null && userSummaries.TryGetValue(q.User.Id, out var us)
                            ? us
                            : null
                    )
                ).ToList();

                return base.Ok(new ApiResponse<List<DTOs.Portage.PortageResponse>>()
                {
                    Success = true,
                    Message = "Guias Portage retornados com sucesso",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>()
                {
                    Success = false,
                    Message = $"Ocorreu um erro ao retornar a lista de guias Portage. {ex.Message}"
                });
            }
        }

        #endregion

        #region UPDATE
        [Authorize(Policy = "AdminOrTeacher")]
        [HttpPut]
        public async Task<IActionResult> UpdatePortageGuide([FromBody] PortageUpdateRequest model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return ValidationProblem();
                }

                var portage = await _appDbContext.PortageGuides.AsSplitQuery()
                                                   .FirstOrDefaultAsync(p => p.ID == model.ID);

                if (portage == null)
                {
                    return NotFound(new ApiResponse<object>()
                    {
                        Success = false,
                        Message = $"Nenhum Guia Portage foi encontrado com o ID {model.ID}"
                    });
                }

                _appDbContext.PortageGuideResponses.RemoveRange(portage.Responses);
                PortageGuideMapper.UpdateEntity(portage, model);

                await _appDbContext.Logs.AddAsync(new Log
                {
                    LogLevel = Models.Enums.LogLevel.Info.ToString(),
                    Message = $"Guia Portage atualizado com sucesso com ID {portage.ID}",
                    Source = "PortageController",
                    UserID = CurrentUserGuid,
                    EntityType = "PortageGuide",
                    EntityID = portage.ID,
                    Details = "",
                    Action = "Update"
                });
                await _appDbContext.SaveChangesAsync();

                await _notificationService.CreateNotificationAdmins(
                    type: TypeNotification.System,
                    title: "Guia Portage atualizado!",
                    message: "Um Guia Portage foi atualizado com sucesso.",
                    actionURL: "api/v1/portage"
                );

                return Ok(new ApiResponse<object>()
                {
                    Success = true,
                    Message = "Guia Portage atualizado com sucesso",
                    Data = new
                    {
                        portage.ID,
                        portage.StudentID
                    }
                });


            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>()
                {
                    Success = false,
                    Message = $"Ocorreu um erro ao atualizar Guia Portage. {ex.Message}"
                });
            }
        }
        #endregion

        #region DELETE
        [Authorize(Policy = "AdminOrTeacher")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePortageGuid(Guid id, [FromQuery] bool confirmDeletion)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return ValidationProblem();
                }

                if (!confirmDeletion)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "A confirmação da exclusão é obrigatória"
                    });
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var portage = await _appDbContext.PortageGuides.FirstOrDefaultAsync(temp => temp.ID == id);

                if (portage == null)
                {
                    return NotFound(new ApiResponse<object>()
                    {
                        Success = false,
                        Message = $"Guia Portage com ID {id} não encontrado"
                    });
                }

                _appDbContext.PortageGuides.Remove(portage);
                await _appDbContext.SaveChangesAsync();

                await _appDbContext.Logs.AddAsync(new Log
                {
                    LogLevel = Models.Enums.LogLevel.Info.ToString(),
                    Message = $"Guia Portage excluido com sucesso com ID {portage.ID}",
                    Source = "PortageController",
                    UserID = Guid.Parse(userId!),
                    EntityType = "PortageGuide",
                    EntityID = portage.ID,
                    Details = "",
                    Action = "Delete"
                });
                await _appDbContext.SaveChangesAsync();

                await _notificationService.CreateNotificationAdmins(
                    type: TypeNotification.System,
                    title: "Guia Portage excluido!",
                    message: "Guia Portage foi removido com sucesso.",
                    actionURL: "api/v1/portage"
                );

                return Ok(new ApiResponse<object>()
                {
                    Success = true,
                    Message = "Guia Portage excluído com sucesso"
                });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao excluir Guia Portage com id {id}");
                return BadRequest(new ApiResponse<object>()
                {
                    Success = false,
                    Message = $"Ocorreu um erro ao excluir o Guia Portage com id {id}"
                });
            }
        }
        #endregion
    }
}
