using SingularSystem.DTOs.RegularTeachers.EvaluationDocuments;
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
    [Route("api/v1/regular-teacher-evaluations")]
    [ApiController]
    public class RegularTeacherEvaluationDocumentsController : BaseController
    {
        private readonly ILogger<RegularTeacherEvaluationDocumentsController> _logger;
        private readonly AppDbContext _appDbContext;
        private readonly IUserSummaryFactory _userSummaryFactory;
        private readonly StudentEvaluationDocumentService _studentEvaluationDocumentService;
        private readonly ISignatureService _zapSignService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITenantContext _tenantContext;

        public RegularTeacherEvaluationDocumentsController(ILogger<RegularTeacherEvaluationDocumentsController> logger, AppDbContext appDbContext, IUserSummaryFactory userSummaryFactory, StudentEvaluationDocumentService studentEvaluationDocumentService, ISignatureService zapSignService, UserManager<ApplicationUser> userManager, ITenantContext tenantContext)
        {
            _logger = logger;
            _appDbContext = appDbContext;
            _userSummaryFactory = userSummaryFactory;
            _studentEvaluationDocumentService = studentEvaluationDocumentService;
            _zapSignService = zapSignService;
            _userManager = userManager;
            _tenantContext = tenantContext;
        }

        private IQueryable<RegularTeacherEvaluationDocument> BuildRegularTeacherEvaluationDocumentQuery(
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

            var query = _appDbContext.RegularTeacherEvaluationDocuments.IgnoreQueryFilters()
                                         .Include(s => s.User)
                                         .Include(s => s.Student).Include(st => st.Student!.School)
                                         .Include(s => s.RegularTeacherQuestionnaire)
                                         .Include(s => s.RegularTeacherScreening)
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

        private IQueryable<Student> BuildStudentBaseQuery(
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

            var query = _appDbContext.Students.Include(s => s.School)
                                              .AsNoTracking()
                                              .AsSplitQuery();

            if (_tenantContext.IsPrivatePlan)
            {
                query = query.Where(s => s.TenantId == _tenantContext.TenantId);
                return query;
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(s => s != null && s.FullName.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(state))
            {
                query = query.Where(s => s.State == state || (s.School != null && s.School.State == state));
            }

            if (!string.IsNullOrWhiteSpace(city))
            {
                query = query.Where(s => s.City == city || (s.School != null && s.School.City == city));
            }

            if (!string.IsNullOrWhiteSpace(schoolID))
            {
                query = query.Where(s => s.SchoolID == Guid.Parse(schoolID));
            }

            if (!isAdmin)
            {
                if (isCoordinator || isPrincipal)
                {
                    query = query.Where(s => (s.City == currentUser.City && s.State == currentUser.State) ||
                                             (s.School != null && s.School.City == currentUser.City && s.School.State == currentUser.State));
                }
                else if (isPsychologist || isSocialWorker || isMunicipalManager || isSecretary || isPsychopedagogue || isTeacher)
                {
                    query = query.Where(s => (s.City == currentUser.City && s.State == currentUser.State) ||
                                             (s.School != null && s.School.City == currentUser.City && s.School.State == currentUser.State) ||
                                             (currentUser.SchoolID != null && s.SchoolID == currentUser.SchoolID));
                }
                else
                {
                    query = query.Where(s => (s.City == currentUser.City && s.State == currentUser.State) ||
                                             (s.School != null && s.School.City == currentUser.City && s.School.State == currentUser.State));
                }
            }

            return query;
        }

        private IQueryable<Student> ApplyMyTeacherEvaluationsFilter(
            IQueryable<Student> query,
            ApplicationUser user,
            string myStatus)
        {
            var isAdmin = User.IsInRole("Admin");
            var cutoffDate = new DateTime(2026, 8, 18, 0, 0, 0, DateTimeKind.Utc);
            var currentUserId = CurrentUserId;

            query = query.Where(student =>
                _appDbContext.RegularTeacherEvaluationDocuments.Any(d => d.StudentID == student.ID && (isAdmin || d.UserID == currentUserId)) ||
                _appDbContext.RegularTeacherScreenings.Any(s => s.StudentID == student.ID && (isAdmin || s.UserID == currentUserId)) ||
                _appDbContext.RegularTeacherQuestionnaires.Any(q => q.StudentID == student.ID && (isAdmin || q.UserID == currentUserId))
            );

            if (!string.IsNullOrWhiteSpace(myStatus) && !myStatus.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                if (myStatus.Equals("pending_fill", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(student =>
                        !_appDbContext.RegularTeacherEvaluationDocuments.Any(d => d.StudentID == student.ID) &&
                        (_appDbContext.RegularTeacherScreenings.Any(s => s.StudentID == student.ID && (isAdmin || s.UserID == currentUserId)) ||
                         _appDbContext.RegularTeacherQuestionnaires.Any(q => q.StudentID == student.ID && (isAdmin || q.UserID == currentUserId)))
                    );
                }
                else if (myStatus.Equals("pending_sign", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(student => _appDbContext.RegularTeacherEvaluationDocuments.Any(d =>
                        d.StudentID == student.ID &&
                        (isAdmin || d.UserID == currentUserId) &&
                        d.CreatedAt >= cutoffDate &&
                        !d.TeacherSign &&
                        (d.SignedFile == null || d.SignedFile == "")
                    ));
                }
                else if (myStatus.Equals("signed", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(student => _appDbContext.RegularTeacherEvaluationDocuments.Any(d =>
                        d.StudentID == student.ID &&
                        (isAdmin || d.UserID == currentUserId) &&
                        (d.TeacherSign || (d.SignedFile != null && d.SignedFile != "") || d.CreatedAt < cutoffDate)
                    ));
                }
            }

            return query;
        }

        #region GET
        [HttpGet("my-summary")]
        public async Task<IActionResult> GetMyEvaluationsSummary(
            [FromQuery] string state = "",
            [FromQuery] string city = "",
            [FromQuery] string schoolID = "",
            [FromQuery] string searchTerm = "",
            [FromQuery] int? year = null,
            [FromQuery] int? month = null)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(CurrentUserId);
                if (user == null) return Unauthorized();

                var baseQuery = BuildStudentBaseQuery(user, state, city, schoolID, searchTerm);
                if (year.HasValue) baseQuery = baseQuery.Where(s => s.CreatedAt.Year == year.Value);
                if (month.HasValue) baseQuery = baseQuery.Where(s => s.CreatedAt.Month == month.Value);

                var total = await ApplyMyTeacherEvaluationsFilter(baseQuery, user, "all").CountAsync();
                var pendingFill = await ApplyMyTeacherEvaluationsFilter(baseQuery, user, "pending_fill").CountAsync();
                var pendingSign = await ApplyMyTeacherEvaluationsFilter(baseQuery, user, "pending_sign").CountAsync();
                var signed = await ApplyMyTeacherEvaluationsFilter(baseQuery, user, "signed").CountAsync();

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
                _logger.LogError(ex, "Erro ao obter resumo de avaliações do usuário");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Erro ao obter resumo de avaliações do usuário"
                });
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetStudentEvaluationDocuments([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string state = "", [FromQuery] string city = "", [FromQuery] string schoolID = "", [FromQuery] string searchTerm = "", [FromQuery] string status = "", [FromQuery] int? year = null, [FromQuery] int? month = null, [FromQuery] bool onlyMyDocs = false, [FromQuery] bool onlyMyReports = false, [FromQuery] string myStatus = "")
        {
            try
            {
                const int maxPageSize = 50;
                pageSize = Math.Clamp(pageSize, 1, maxPageSize);
                page = Math.Max(page, 1);
                var skip = (page - 1) * pageSize;

                var currentUser = await _userManager.FindByIdAsync(CurrentUserId);
                if (currentUser == null) return Unauthorized();

                var cutoffDate = new DateTime(2026, 8, 18, 0, 0, 0, DateTimeKind.Utc);
                var query = BuildStudentBaseQuery(currentUser, state, city, schoolID, searchTerm);

                if (onlyMyDocs || onlyMyReports)
                {
                    query = ApplyMyTeacherEvaluationsFilter(query, currentUser, myStatus);
                }
                else if (!string.IsNullOrWhiteSpace(status) && !status.Equals("Todos", StringComparison.OrdinalIgnoreCase))
                {
                    if (status.Equals("Completo", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(student => _appDbContext.RegularTeacherEvaluationDocuments.Any(d => d.StudentID == student.ID && d.TeacherSign));
                    }
                    else if (status.Equals("Incompleto", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(student => 
                            _appDbContext.RegularTeacherEvaluationDocuments.Any(d => d.StudentID == student.ID && !d.TeacherSign) ||
                            (!_appDbContext.RegularTeacherEvaluationDocuments.Any(d => d.StudentID == student.ID) && 
                             (_appDbContext.RegularTeacherScreenings.Any(s => s.StudentID == student.ID) || 
                              _appDbContext.RegularTeacherQuestionnaires.Any(q => q.StudentID == student.ID)))
                        );
                    }
                    else if (status.Equals("Não Iniciado", StringComparison.OrdinalIgnoreCase) || status.Equals("Não iniciado", StringComparison.OrdinalIgnoreCase) || status.Equals("NotInitiated", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(student => 
                            !_appDbContext.RegularTeacherEvaluationDocuments.Any(d => d.StudentID == student.ID) && 
                            !_appDbContext.RegularTeacherScreenings.Any(s => s.StudentID == student.ID) && 
                            !_appDbContext.RegularTeacherQuestionnaires.Any(q => q.StudentID == student.ID)
                        );
                    }
                }

                if (year.HasValue)
                {
                    query = query.Where(s => s.CreatedAt.Year == year.Value);
                }
                if (month.HasValue)
                {
                    query = query.Where(s => s.CreatedAt.Month == month.Value);
                }

                var uniqueTotal = await query.CountAsync();

                if (uniqueTotal == 0)
                {
                    return Ok(new ApiResponse<List<RegularTeacherEvaluationDocumentResponse>>()
                    {
                        Success = true,
                        Message = "Nenhum documento de avaliação encontrado",
                        Data = new List<RegularTeacherEvaluationDocumentResponse>(),
                        Total = 0,
                        UniqueTotal = 0
                    });
                }

                var pagedStudents = await query
                    .OrderBy(s => s.FullName)
                    .Skip(skip)
                    .Take(pageSize)
                    .ToListAsync();

                var studentIds = pagedStudents.Select(s => s.ID).ToList();

                var documents = await _appDbContext.RegularTeacherEvaluationDocuments.IgnoreQueryFilters()
                    .Include(d => d.Student).ThenInclude(s => s!.School)
                    .Include(d => d.RegularTeacherScreening)
                    .Include(d => d.RegularTeacherQuestionnaire)
                    .Include(d => d.User)
                    .Where(d => d.StudentID.HasValue && studentIds.Contains(d.StudentID.Value))
                    .ToListAsync();

                var screenings = await _appDbContext.RegularTeacherScreenings
                    .Where(s => s.StudentID.HasValue && studentIds.Contains(s.StudentID.Value))
                    .ToListAsync();

                var questionnaires = await _appDbContext.RegularTeacherQuestionnaires
                    .Where(q => q.StudentID.HasValue && studentIds.Contains(q.StudentID.Value))
                    .ToListAsync();

                var users = documents.Where(d => d.User != null).Select(d => d.User!).DistinctBy(u => u.Id).ToList();
                var userSummaries = await _userSummaryFactory.CreateManyAsync(users);

                var response = new List<RegularTeacherEvaluationDocumentResponse>();
                foreach (var student in pagedStudents)
                {
                    var doc = documents.FirstOrDefault(d => d.StudentID == student.ID);
                    var hasDoc = doc != null;
                    var isDocSigned = doc != null && (doc.TeacherSign || !string.IsNullOrEmpty(doc.SignedFile) || doc.CreatedAt < cutoffDate);
                    var hasScreening = screenings.Any(s => s.StudentID == student.ID);
                    var hasQuestionnaire = questionnaires.Any(q => q.StudentID == student.ID);

                    string calculatedStatus;
                    if (hasDoc && isDocSigned)
                        calculatedStatus = "Completo";
                    else if (hasDoc || hasScreening || hasQuestionnaire)
                        calculatedStatus = "Incompleto";
                    else
                        calculatedStatus = "Não Iniciado";

                    if (doc != null)
                    {
                        var userSummary = doc.User != null && userSummaries.TryGetValue(doc.User.Id, out var us) ? us : null;
                        response.Add(RegularTeacherEvaluationDocumentMapper.ToResponse(doc, userSummary, calculatedStatus, student));
                    }
                    else
                    {
                        var s = student;
                        s.School = student.School;
                        response.Add(RegularTeacherEvaluationDocumentMapper.FromStudent(s, calculatedStatus));
                    }
                }

                return Ok(new ApiResponse<List<RegularTeacherEvaluationDocumentResponse>>()
                {
                    Success = true,
                    Message = "Lista de documentos de avaliação retornada com sucesso",
                    Data = response,
                    Total = uniqueTotal,
                    UniqueTotal = uniqueTotal
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao retornar documentos");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Ocorreu um erro ao retornar documentos."
                });
            }
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllStudentEvaluationDocumentsNoPagination([FromQuery] string state = "", [FromQuery] string city = "", [FromQuery] string schoolID = "", [FromQuery] string searchTerm = "", [FromQuery] string status = "", [FromQuery] int? year = null, [FromQuery] int? month = null, [FromQuery] bool onlyMyDocs = false, [FromQuery] bool onlyMyReports = false, [FromQuery] string myStatus = "")
        {
            try
            {
                var currentUser = await _userManager.FindByIdAsync(CurrentUserId);
                if (currentUser == null) return Unauthorized();

                var cutoffDate = new DateTime(2026, 8, 18, 0, 0, 0, DateTimeKind.Utc);
                var query = BuildStudentBaseQuery(currentUser, state, city, schoolID, searchTerm);

                if (onlyMyDocs || onlyMyReports)
                {
                    query = ApplyMyTeacherEvaluationsFilter(query, currentUser, myStatus);
                }
                else if (!string.IsNullOrWhiteSpace(status) && !status.Equals("Todos", StringComparison.OrdinalIgnoreCase))
                {
                    if (status.Equals("Completo", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(student => _appDbContext.RegularTeacherEvaluationDocuments.Any(d => d.StudentID == student.ID && d.TeacherSign));
                    }
                    else if (status.Equals("Incompleto", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(student => 
                            _appDbContext.RegularTeacherEvaluationDocuments.Any(d => d.StudentID == student.ID && !d.TeacherSign) ||
                            (!_appDbContext.RegularTeacherEvaluationDocuments.Any(d => d.StudentID == student.ID) && 
                             (_appDbContext.RegularTeacherScreenings.Any(s => s.StudentID == student.ID) || 
                              _appDbContext.RegularTeacherQuestionnaires.Any(q => q.StudentID == student.ID)))
                        );
                    }
                    else if (status.Equals("Não Iniciado", StringComparison.OrdinalIgnoreCase) || status.Equals("Não iniciado", StringComparison.OrdinalIgnoreCase) || status.Equals("NotInitiated", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(student => 
                            !_appDbContext.RegularTeacherEvaluationDocuments.Any(d => d.StudentID == student.ID) && 
                            !_appDbContext.RegularTeacherScreenings.Any(s => s.StudentID == student.ID) && 
                            !_appDbContext.RegularTeacherQuestionnaires.Any(q => q.StudentID == student.ID)
                        );
                    }
                }

                if (year.HasValue)
                {
                    query = query.Where(s => s.CreatedAt.Year == year.Value);
                }
                if (month.HasValue)
                {
                    query = query.Where(s => s.CreatedAt.Month == month.Value);
                }

                var uniqueTotal = await query.CountAsync();

                if (uniqueTotal == 0)
                {
                    return Ok(new ApiResponse<List<RegularTeacherEvaluationDocumentResponse>>()
                    {
                        Success = true,
                        Message = "Nenhum documento de avaliação encontrado",
                        Data = new List<RegularTeacherEvaluationDocumentResponse>(),
                        Total = 0,
                        UniqueTotal = 0
                    });
                }

                var students = await query
                    .OrderBy(s => s.FullName)
                    .ToListAsync();

                var studentIds = students.Select(s => s.ID).ToList();

                var documents = await _appDbContext.RegularTeacherEvaluationDocuments.IgnoreQueryFilters()
                    .Include(d => d.Student).ThenInclude(s => s!.School)
                    .Include(d => d.RegularTeacherScreening)
                    .Include(d => d.RegularTeacherQuestionnaire)
                    .Include(d => d.User)
                    .Where(d => d.StudentID.HasValue && studentIds.Contains(d.StudentID.Value))
                    .ToListAsync();

                var screenings = await _appDbContext.RegularTeacherScreenings
                    .Where(s => s.StudentID.HasValue && studentIds.Contains(s.StudentID.Value))
                    .ToListAsync();

                var questionnaires = await _appDbContext.RegularTeacherQuestionnaires
                    .Where(q => q.StudentID.HasValue && studentIds.Contains(q.StudentID.Value))
                    .ToListAsync();

                var users = documents.Where(d => d.User != null).Select(d => d.User!).DistinctBy(u => u.Id).ToList();
                var userSummaries = await _userSummaryFactory.CreateManyAsync(users);

                var response = new List<RegularTeacherEvaluationDocumentResponse>();
                foreach (var student in students)
                {
                    var doc = documents.FirstOrDefault(d => d.StudentID == student.ID);
                    var hasDoc = doc != null;
                    var isDocSigned = doc != null && (doc.TeacherSign || !string.IsNullOrEmpty(doc.SignedFile) || doc.CreatedAt < cutoffDate);
                    var hasScreening = screenings.Any(s => s.StudentID == student.ID);
                    var hasQuestionnaire = questionnaires.Any(q => q.StudentID == student.ID);

                    string calculatedStatus;
                    if (hasDoc && isDocSigned)
                        calculatedStatus = "Completo";
                    else if (hasDoc || hasScreening || hasQuestionnaire)
                        calculatedStatus = "Incompleto";
                    else
                        calculatedStatus = "Não Iniciado";

                    if (doc != null)
                    {
                        var userSummary = doc.User != null && userSummaries.TryGetValue(doc.User.Id, out var us) ? us : null;
                        response.Add(RegularTeacherEvaluationDocumentMapper.ToResponse(doc, userSummary, calculatedStatus, student));
                    }
                    else
                    {
                        var s = student;
                        s.School = student.School;
                        response.Add(RegularTeacherEvaluationDocumentMapper.FromStudent(s, calculatedStatus));
                    }
                }

                return Ok(new ApiResponse<List<RegularTeacherEvaluationDocumentResponse>>()
                {
                    Success = true,
                    Message = "Lista de documentos de avaliação retornada com sucesso",
                    Data = response,
                    Total = uniqueTotal,
                    UniqueTotal = uniqueTotal
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao retornar documentos");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Ocorreu um erro ao retornar documentos."
                });
            }
        }

        [NoSecretary]
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetStudentEvaluationDocument(Guid id)
        {
            try
            {
                RegularTeacherEvaluationDocument? document = await _appDbContext.RegularTeacherEvaluationDocuments.IgnoreQueryFilters()
                                         .Include(s => s.Student)
                                         .Include(st => st.Student!.School)
                                         .Include(u => u.User)
                                         .Include(s => s.RegularTeacherQuestionnaire).ThenInclude(b => b.BehaviorTraits)
                                         .Include(s => s.RegularTeacherScreening)
                                         .AsNoTracking()
                                         .FirstOrDefaultAsync(s => s.ID == id);

                if (document == null)
                {
                    _logger.LogWarning(
                        "Usuário {UserId} tentou acessar documento {DocumentId} não encontrado",
                        CurrentUserId,
                        id
                    );
                    return NotFound(new ApiResponse<object>()
                    {
                        Success = false,
                        Message = "Documento de avaliação não encontrada"
                    });
                }

                var user = await _userManager.FindByIdAsync(CurrentUserId);
                if (user == null) return Unauthorized();

                var roles = await _userManager.GetRolesAsync(user);

                if (document.Student != null && !HasStudentAccess(user, document.Student, roles))
                {
                    return Forbid();
                }

                // Sincroniza o documento com o serviço de assinatura se possuir token e não estiver completo
                if (!string.IsNullOrEmpty(document.DocumentToken) && (!document.TeacherSign || document.DocumentStatus != Status.Complete))
                {
                    var synced = await _zapSignService.SyncDocument(document.DocumentToken);
                    if (synced)
                    {
                        document = await _appDbContext.RegularTeacherEvaluationDocuments.IgnoreQueryFilters()
                                                 .Include(s => s.Student)
                                                 .Include(st => st.Student!.School)
                                                 .Include(u => u.User)
                                                 .Include(s => s.RegularTeacherQuestionnaire).ThenInclude(b => b.BehaviorTraits)
                                                 .Include(s => s.RegularTeacherScreening)
                                                 .AsNoTracking()
                                                 .FirstOrDefaultAsync(s => s.ID == id);

                        if (document == null)
                        {
                            return NotFound(new ApiResponse<object>()
                            {
                                Success = false,
                                Message = "Documento de avaliação não encontrado após sincronização"
                            });
                        }
                    }
                }

                if (string.IsNullOrEmpty(document.SignUrl))
                {
                    var resultDocCreation = await _zapSignService.CreateDoc(document.ID, "caseStudy");

                    if (!resultDocCreation)
                    {
                        _logger.LogWarning($"Documento no ZapSign não foi criado para o Estudo de Caso ID {document.ID}.");
                        var userSummaryWithoutSign = await _userSummaryFactory.CreateAsync(document.User);
                        var responseWithoutSign = RegularTeacherEvaluationDocumentMapper.ToResponse(document, userSummaryWithoutSign);

                        return StatusCode(207, new ApiResponse<object>()
                        {
                            Success = true,
                            Message = "Estudo de Caso retornado com sucesso, mas não foi possível criar o documento para assinatura.",
                            Data = responseWithoutSign
                        });
                    }

                    document = await _appDbContext.RegularTeacherEvaluationDocuments.IgnoreQueryFilters()
                                             .Include(s => s.Student)
                                             .Include(st => st.Student!.School)
                                             .Include(u => u.User)
                                             .Include(s => s.RegularTeacherQuestionnaire).ThenInclude(b => b.BehaviorTraits)
                                             .Include(s => s.RegularTeacherScreening)
                                             .AsNoTracking()
                                             .FirstOrDefaultAsync(s => s.ID == id);

                    if (document == null)
                    {
                        return NotFound(new ApiResponse<object>()
                        {
                            Success = false,
                            Message = "Documento de avaliação não encontrada"
                        });
                    }
                }

                var userSummary = await _userSummaryFactory.CreateAsync(document.User);
                var response = RegularTeacherEvaluationDocumentMapper.ToResponse(document, userSummary);

                return Ok(new ApiResponse<RegularTeacherEvaluationDocumentResponse>()
                {
                    Success = true,
                    Message = "Documento de avaliação retornado com sucesso",
                    Data = response,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao retornar documento {DocumentId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Ocorreu um erro ao retornar documento"
                });
            }
        }
        #endregion
    }
}
