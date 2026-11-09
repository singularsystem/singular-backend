using SingularSystem.DTOs.PsychologistReports;
using SingularSystem.Interfaces;
using SingularSystem.Mappers;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;
using SingularSystem.Authorization;

namespace SingularSystem.Controllers
{
    [Authorize]
    [NoSecretary]
    [Route("api/v1/psychologist-reports")]
    [ApiController]
    public class PsychologistReportsController : BaseController
    {
        private readonly ILogger<PsychologistReportsController> _logger;
        private readonly AppDbContext _appDbContext;
        private readonly NotificationService _notificationService;
        private readonly IUserSummaryFactory _userSummaryFactory;
        private readonly UserManager<ApplicationUser> _userManager;

        public PsychologistReportsController(ILogger<PsychologistReportsController> logger, AppDbContext appDbContext, NotificationService notificationService, IUserSummaryFactory userSummaryFactory, UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _appDbContext = appDbContext;
            _notificationService = notificationService;
            _userSummaryFactory = userSummaryFactory;
            _userManager = userManager;
        }

        #region GET
        [HttpGet]
        public async Task<IActionResult> GetPsychologistReports([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchTerm = "")
        {
            try
            {
                const int maxPageSize = 50;
                pageSize = Math.Clamp(pageSize, 1, maxPageSize);
                page = Math.Max(page, 1);
                var skip = (page - 1) * pageSize;

                var currentUser = await _userManager.FindByIdAsync(CurrentUserId);
                if (currentUser == null) return Unauthorized();

                var roles = await _userManager.GetRolesAsync(currentUser);
                var isAdmin = roles.Contains("Admin");

                var query = _appDbContext.PsychologistReports.IgnoreQueryFilters()
                                         .Include(s => s.Student).Include(st => st.Student!.School)
                                         .Include(u => u.User)
                                         .Include(a => a.AdaptiveBehaviorScaleDI)
                                         .Include(p => p.PsychologistQuestionnaire)
                                         .Include(s => s.SnapIVQuestionnaire)
                                         .AsNoTracking();

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var term = searchTerm.Trim().ToLower();
                    query = query.Where(s => s.Student != null &&
                        s.Student.FullName.ToLower().Contains(term));
                }

                if (!isAdmin)
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
                    return Ok(new ApiResponse<List<PsychologistReportResponse>>()
                    {
                        Success = true,
                        Message = "Nenhuma relatório encontrado",
                        Data = new List<PsychologistReportResponse>(),
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
                    PsychologistReportMapper.ToResponse(
                        q,
                        q.User != null && userSummaries.TryGetValue(q.User.Id, out var us)
                            ? us
                            : null
                    )
                ).ToList();

                return Ok(new ApiResponse<List<PsychologistReportResponse>>()
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
        public async Task<IActionResult> GetPsychologistReport(Guid id)
        {
            try
            {
                var report = await _appDbContext.PsychologistReports.IgnoreQueryFilters()
                                         .Include(s => s.Student).Include(st => st.Student!.School)
                                         .Include(a => a.AdaptiveBehaviorScaleDI).ThenInclude(u => u.User)
                                         .Include(p => p.PsychologistQuestionnaire).ThenInclude(u => u.User)
                                         .Include(s => s.SnapIVQuestionnaire).ThenInclude(u => u.User)
                                         .AsNoTracking()
                                         .FirstOrDefaultAsync(s => s.ID == id 
                                             || (s.PsychologistQuestionnaire != null && s.PsychologistQuestionnaire.ID == id)
                                             || (s.AdaptiveBehaviorScaleDI != null && s.AdaptiveBehaviorScaleDI.ID == id)
                                             || (s.SnapIVQuestionnaire != null && s.SnapIVQuestionnaire.ID == id));

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

                if (report.Student != null && !HasStudentAccess(user, report.Student, roles))
                {
                    return StatusCode(403, new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Sem permissão para acessar este relatório."
                    });
                }

                var userSummary = await _userSummaryFactory.CreateAsync(report.PsychologistQuestionnaire?.User);
                var response = PsychologistReportMapper.ToResponse(report, userSummary);

                return Ok(new ApiResponse<PsychologistReportResponse>()
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
