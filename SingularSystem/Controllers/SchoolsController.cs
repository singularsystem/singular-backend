using SingularSystem.DTOs.Schools;
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
    [Route("api/v1/[controller]")]
    [ApiController]
    public class SchoolsController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _appDbContext;
        private readonly ILogger<SchoolsController> _logger;
        private readonly NotificationService _notificationService;

        public SchoolsController(AppDbContext appDbContext, ILogger<SchoolsController> logger, UserManager<ApplicationUser> userManager, NotificationService notificationService)
        {
            _userManager = userManager;
            _appDbContext = appDbContext;
            _logger = logger;
            _notificationService = notificationService;
        }

        //[Authorize(Roles = "Admin,Secretary")]
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateSchool([FromBody] SchoolRequest model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return ValidationProblem();
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var user = await _appDbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                    return Unauthorized(new ApiResponse<object> { Success = false, Message = "Usuário não encontrado." });

                if (User.IsInRole("Secretary"))
                {
                    if (!string.Equals(user.City, model.City, StringComparison.OrdinalIgnoreCase) ||
                        !string.Equals(user.State, model.State, StringComparison.OrdinalIgnoreCase))
                    {
                        return BadRequest(new ApiResponse<object>
                        {
                            Success = false,
                            Message = "Secretário só pode criar escolas na mesma cidade e estado do seu cadastro."
                        });
                    }
                }

                if (!string.IsNullOrEmpty(model.PrincipalID))
                {
                    var schoolInDatabase = await _appDbContext.Schools.FirstOrDefaultAsync(temp => temp.PrincipalID == model.PrincipalID);

                    if (schoolInDatabase != null)
                    {
                        return BadRequest(new ApiResponse<object>()
                        {
                            Success = false,
                            Message = "Um usuário não pode ser diretor em mais de uma escola. Escolha outro(a) diretor(a)"
                        });
                    }
                }

                var school = SchoolMapper.ToEntity(model);

                await _appDbContext.Schools.AddAsync(school);
                await _appDbContext.SaveChangesAsync();

                await _appDbContext.Logs.AddAsync(new Log
                {
                    LogLevel = Models.Enums.LogLevel.Info.ToString(),
                    Message = $"Escola criada com sucesso com ID {school.ID}",
                    Source = "SchoolsController",
                    UserID = Guid.Parse(userId!),
                    EntityType = "School",
                    EntityID = school.ID,
                    Details = "",
                    Action = "Create"
                });
                await _appDbContext.SaveChangesAsync();

                await _notificationService.CreateNotificationAdmins(
                    type: TypeNotification.System,
                    title: "Nova escola criada!",
                    message: $"Escola {model.Name} foi criada em {model.City} - {model.State}.",
                    actionURL: "api/v1/schools"
                );

                return StatusCode(201, new ApiResponse<object>()
                {
                    Success = true,
                    Message = "Escola criada com sucesso",
                    Data = new
                    {
                        school.ID
                    }
                });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar escola");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Ocorreu um erro ao criar escola"
                });
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAllSchools([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchTerm = "", [FromQuery] string? state = null, [FromQuery] string? city = null)
        {
            try
            {
                const int maxPageSize = 50;
                pageSize = Math.Min(pageSize, maxPageSize);
                page = Math.Max(page, 1);
                var skip = (page - 1) * pageSize;

                var query = _appDbContext.Schools.Include(s => s.Students)
                                                 .AsNoTracking()
                                                 .Where(s => s.Name.Contains(searchTerm));

                if (!string.IsNullOrEmpty(state))
                {
                    query = query.Where(s => s.State == state);
                }

                if (!string.IsNullOrEmpty(city))
                {
                    query = query.Where(s => s.City == city);
                }

                if (!query.Any())
                {
                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = "Nenhuma escola foi cadastrado ainda"
                    });
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var user = await _userManager.FindByIdAsync(userId);

                if (user == null)
                {

                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Usuário com ID {userId} não foi encontrado"
                    });
                }

                var roles = await _userManager.GetRolesAsync(user);
                var isAdmin = roles.Contains("Admin") || roles.Contains("AdminMaster");

                if (!isAdmin)
                {
                    query = query.Where(p => p.City == user.City &&
                                             (string.IsNullOrEmpty(user.State) || p.State == user.State));
                }

                var totalSchools = await query.CountAsync();

                var schools = await query.OrderBy(u => u.CreatedAt)
                                   .Skip(skip)
                                   .Take(pageSize)
                                   .AsSplitQuery()
                                   .ToListAsync();

                var response = schools.Select(SchoolMapper.ToResponse).ToList();

                return Ok(new ApiResponse<List<SchoolResponse>>()
                {
                    Success = true,
                    Message = "Lista de escolas retornada com sucesso",
                    Data = response,
                    Total = totalSchools
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(new ApiResponse<object>()
                {
                    Success = false,
                    Message = $"Ocorreu um erro ao retornar escolas. {ex.Message}"
                });
            }
        }

        [HttpGet("city/{city}")]
        public async Task<IActionResult> GetAllSchoolsByCity(string city, [FromQuery] string? state = null)
        {
            try
            {
                var query = _appDbContext.Schools.Include(s => s.Students)
                                                 .AsSplitQuery()
                                                 .Where(temp => temp.City == city);

                if (!string.IsNullOrEmpty(state))
                {
                    query = query.Where(temp => temp.State == state);
                }

                var schools = await query.ToListAsync();

                if (schools == null || schools.Count == 0)
                {
                    return Ok(new ApiResponse<object>()
                    {
                        Success = true,
                        Message = $"Nenhuma escola foi cadastrada ainda para a cidade {city}"
                    });
                }

                var response = schools.Select(SchoolMapper.ToResponse).ToList();

                return Ok(new ApiResponse<List<SchoolResponse>>()
                {
                    Success = true,
                    Message = "Lista de escolas retornada com sucesso",
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
                    Message = $"Ocorreu um erro ao retornar escolas. {ex.Message}"
                });
            }
        }

        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSchool(Guid id)
        {
            try
            {
                var school = await _appDbContext.Schools.Include(s => s.Students).AsSplitQuery().FirstOrDefaultAsync(temp => temp.ID == id);

                if (school == null)
                {
                    return NotFound(new ApiResponse<object>()
                    {
                        Success = false,
                        Message = $"Escola com ID {id} não encontrada"
                    });
                }

                var schoolResponse = SchoolMapper.ToResponse(school);

                return Ok(new ApiResponse<SchoolResponse>()
                {
                    Success = true,
                    Message = "Escola retornada com sucesso",
                    Data = schoolResponse
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(new ApiResponse<object>()
                {
                    Success = false,
                    Message = $"Ocorreu um erro ao retornar escola. {ex.Message}"
                });
            }
        }

        //[Authorize(Roles = "Admin,Secretary,Principal,Coordinator")]
        [Authorize]
        [HttpPut]
        public async Task<IActionResult> UpdateSchool([FromBody] SchoolUpdateRequest model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return ValidationProblem();
                }

                var school = await _appDbContext.Schools.FirstOrDefaultAsync(s => s.ID == model.ID);

                if (school == null)
                {
                    return NotFound(new ApiResponse<object>()
                    {
                        Success = false,
                        Message = $"Escola com ID {model.ID} não encontrada"
                    });
                }

                if (model.Name == school.Name && model.City == school.City && model.State == school.State && model.PrincipalID == school.PrincipalID)
                {
                    return Ok(new ApiResponse<object>()
                    {
                        Success = false,
                        Message = "Nenhuma alteração foi feita pois os dados já estavam atualizados.",
                        Data = new
                        {
                            school.ID,
                            school.Name,
                            school.City
                        }
                    });
                }

                SchoolMapper.UpdateEntity(school, model);
                
                _appDbContext.Schools.Update(school);
                await _appDbContext.SaveChangesAsync();

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                await _appDbContext.Logs.AddAsync(new Log
                {
                    LogLevel = Models.Enums.LogLevel.Info.ToString(),
                    Message = $"Escola atualizada com sucesso com ID {school.ID}",
                    Source = "SchoolsController",
                    UserID = Guid.Parse(userId!),
                    EntityType = "School",
                    EntityID = school.ID,
                    Details = "",
                    Action = "Update"
                });
                await _appDbContext.SaveChangesAsync();

                await _notificationService.CreateNotificationAdmins(
                    type: TypeNotification.System,
                    title: "Escola atualizada!",
                    message: $"Escola {model.Name} em {model.City} - {model.State} foi atualizada com sucesso!",
                    actionURL: "api/v1/schools"
                );

                return Ok(new ApiResponse<object>()
                {
                    Success = true,
                    Message = "Escola atualizada com sucesso",
                    Data = new
                    {
                        school.ID,
                        school.Name,
                        school.PrincipalID,
                        school.City
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar dados de escola");
                return BadRequest(new ApiResponse<object>()
                {
                    Success = false,
                    Message = "Ocorreu um erro ao atualizar a escola"
                });
            }
        }

        //[Authorize(Roles = "Admin,Secretary,Principal,Coordinator")]
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeactivateSchool(Guid id, [FromQuery] bool confirmDeletion)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return ValidationProblem();
                }

                if (!confirmDeletion)
                {
                    return BadRequest(new ApiResponse<object>()
                    {
                        Success = false,
                        Message = "A confirmação da exclusão é obrigatória"
                    });
                }

                var school = await _appDbContext.Schools.IgnoreQueryFilters().Include(s => s.Students).FirstOrDefaultAsync(temp => temp.ID == id);

                if (school == null)
                {
                    return NotFound(new ApiResponse<object>()
                    {
                        Success = false,
                        Message = $"Escola com ID {id} não encontrada"
                    });
                }

                if (!school.IsActive)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Esta escola já está desativada"
                    });
                }

                if (school.Students.Any())
                {
                    return BadRequest(new ApiResponse<object>()
                    {
                        Success = false,
                        Message = "Não é possivel excluir escola com estudantes ativos"
                    });
                }

                school.IsActive = false;
                school.DeactivatedAt = DateTime.UtcNow;

                _appDbContext.Schools.Update(school);
                await _appDbContext.SaveChangesAsync();

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                await _appDbContext.Logs.AddAsync(new Log
                {
                    LogLevel = Models.Enums.LogLevel.Info.ToString(),
                    Message = $"Escola desativada com sucesso com ID {school.ID}",
                    Source = "SchoolsController",
                    UserID = Guid.Parse(userId!),
                    EntityType = "School",
                    EntityID = school.ID,
                    Details = "",
                    Action = "Deactivate"
                });
                await _appDbContext.SaveChangesAsync();

                await _notificationService.CreateNotificationAdmins(
                    type: TypeNotification.System,
                    title: "Escola desativada",
                    message: $"Escola {school.Name} foi desativada com sucesso!",
                    actionURL: "api/v1/schools"
                );

                return Ok(new ApiResponse<object>()
                {
                    Success = true,
                    Message = "Escola desativada com sucesso"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao desativar escola com id {id}");
                return BadRequest(new ApiResponse<object>()
                {
                    Success = false,
                    Message = $"Ocorreu um erro ao desativar a escola {id}"
                });
            }
        }
    }
}
