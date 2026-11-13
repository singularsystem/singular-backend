using SingularSystem.DTOs.PsychopedagogueReports;
using SingularSystem.Interfaces;
using SingularSystem.Mappers;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
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
    [Route("api/v1/psychopedagogue-reports")]
    [ApiController]
    public class PsychopedagogueReportsController : BaseController
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<PsychopedagogueReportsController> _logger;
        private readonly AppDbContext _appDbContext;
        private readonly NotificationService _notificationService;
        private readonly IUserSummaryFactory _userSummaryFactory;

        public PsychopedagogueReportsController(ILogger<PsychopedagogueReportsController> logger, AppDbContext appDbContext, NotificationService notificationService, UserManager<ApplicationUser> userManager, IUserSummaryFactory userSummaryFactory)
        {
            _logger = logger;
            _appDbContext = appDbContext;
            _notificationService = notificationService;
            _userManager = userManager;
            _userSummaryFactory = userSummaryFactory;
        }

        #region GET
        [HttpGet]
        public async Task<IActionResult> GetPsychopedagogueReports([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchTerm = "")
        {
            try
            {
                const int maxPageSize = 50;
                pageSize = Math.Clamp(pageSize, 1, maxPageSize);
                page = Math.Max(page, 1);
                var skip = (page - 1) * pageSize;

                var query = _appDbContext.PsychopedagogueReports.Include(s => s.Student).Include(st => st.Student!.School)
                                         .Include(u => u.User)
                                         .Include(p => p.PsychopedagogueQuestionnaire)
                                         .Include(c => c.CognitiveAssessment)
                                         .Include(c => c.CARSQuestionnaire)
                                         .Include(m => m.MCHATQuestionnaire)
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
                    return Ok(new ApiResponse<List<PsychopedagogueReportResponse>>()
                    {
                        Success = true,
                        Message = "Nenhuma relatório encontrado",
                        Data = new List<PsychopedagogueReportResponse>(),
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
                    PsychopedagogueReportMapper.ToResponse(
                        q,
                        q.User != null && userSummaries.TryGetValue(q.User.Id, out var us)
                            ? us
                            : null
                    )
                ).ToList();

                return Ok(new ApiResponse<List<PsychopedagogueReportResponse>>()
                {
                    Success = true,
                    Message = "Lista de relatórios retornada com sucesso",
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
        public async Task<IActionResult> GetPsychopedagogueReport(Guid id)
        {
            try
            {

                var report = await _appDbContext.PsychopedagogueReports.IgnoreQueryFilters()
                                         .Include(s => s.Student).Include(st => st.Student!.School)
                                         .Include(p => p.PsychopedagogueQuestionnaire).ThenInclude(u => u.User)
                                         .Include(p => p.PsychopedagogueQuestionnaire).ThenInclude(u => u.Student).ThenInclude(s => s!.School)
                                         .Include(p => p.PsychopedagogueQuestionnaire).ThenInclude(u => u.Recommendations)
                                         .Include(p => p.PsychopedagogueQuestionnaire).ThenInclude(u => u.TypeOfDisorder)
                                         .Include(p => p.PsychopedagogueQuestionnaire).ThenInclude(u => u.TypeOfDisability)
                                         .Include(c => c.CognitiveAssessment).ThenInclude(u => u.User)
                                         .Include(c => c.CARSQuestionnaire).ThenInclude(u => u!.User)
                                         .Include(m => m.MCHATQuestionnaire).ThenInclude(u => u!.User)
                                         .Include(u => u.User)
                                         .AsNoTracking()
                                         .FirstOrDefaultAsync(s => s.ID == id
                                             || (s.PsychopedagogueQuestionnaire != null && s.PsychopedagogueQuestionnaire.ID == id)
                                             || (s.CognitiveAssessment != null && s.CognitiveAssessment.ID == id)
                                             || (s.CARSQuestionnaire != null && s.CARSQuestionnaire.ID == id)
                                             || (s.MCHATQuestionnaire != null && s.MCHATQuestionnaire.ID == id));

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
                var isAdmin = roles.Contains("Admin");

                if (report.Student != null && IsOutsideTenant(user, report.Student, roles))
                {
                    return StatusCode(403, DocumentAccessDenied.Body());
                }

                if (!isAdmin)
                {
                    if (roles.Contains("Principal"))
                    {
                        if (user.SchoolID != null)
                        {
                            if (report.Student?.SchoolID != user.SchoolID)
                            {
                                _logger.LogWarning(
                                    "Usuário {UserId} tentou acessar relatório psicopedagógico {ReportId} sem permissão (escola divergente)",
                                    CurrentUserId,
                                    id
                                );
                                return StatusCode(403, new ApiResponse<object>
                                {
                                    Success = false,
                                    Message = "Você não tem permissão para acessar este relatório."
                                });
                            }
                        }
                        else
                        {
                            bool sameMunicipality = (report.Student?.City == user.City && report.Student?.State == user.State) ||
                                                    (report.Student?.School != null && report.Student.School.City == user.City && report.Student.School.State == user.State);
                            if (!sameMunicipality)
                            {
                                _logger.LogWarning(
                                    "Usuário {UserId} tentou acessar relatório psicopedagógico {ReportId} sem permissão (município divergente)",
                                    CurrentUserId,
                                    id
                                );
                                return StatusCode(403, new ApiResponse<object>
                                {
                                    Success = false,
                                    Message = "Você não tem permissão para acessar este relatório."
                                });
                            }
                        }
                    }
                    else if (roles.Contains("Coordinator") || roles.Contains("Teacher") || roles.Contains("Psychologist") || roles.Contains("SocialWork") ||
                             roles.Contains("MunicipalManager") || roles.Contains("Secretary") ||
                             roles.Contains("Psychopedagogue"))
                    {
                        bool sameMunicipality = (report.Student?.City == user.City && report.Student?.State == user.State) ||
                                                (report.Student?.School != null && report.Student.School.City == user.City && report.Student.School.State == user.State);

                        if (!sameMunicipality)
                        {
                            _logger.LogWarning(
                                "Usuário {UserId} tentou acessar relatório psicopedagógico {ReportId} sem permissão (município divergente)",
                                CurrentUserId,
                                id
                            );
                            return StatusCode(403, new ApiResponse<object>
                            {
                                Success = false,
                                Message = "Você não tem permissão para acessar este relatório."
                            });
                        }
                    }
                }

                var userSummary = await _userSummaryFactory.CreateAsync(report.PsychopedagogueQuestionnaire?.User);
                var response = PsychopedagogueReportMapper.ToResponse(report, userSummary);

                return Ok(new ApiResponse<PsychopedagogueReportResponse>()
                {
                    Success = true,
                    Message = "Relatório retornado com sucesso",
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

    }
}
