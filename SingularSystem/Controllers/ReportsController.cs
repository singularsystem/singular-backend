using SingularSystem.DTOs.Reports;
using SingularSystem.DTOs.Users;
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
    [Route("api/v1/[controller]")]
    [ApiController]
    public class ReportsController : BaseController
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly NotificationService _notificationService;
        private readonly ISignatureService _zapSignService;
        private readonly AppDbContext _appDbContext;
        private readonly ILogger<ReportsController> _logger;
        private readonly IUserSummaryFactory _userSummaryFactory;

        public ReportsController(AppDbContext appDbContext, ILogger<ReportsController> logger, NotificationService notificationService, ISignatureService zapSignService, IUserSummaryFactory userSummaryFactory, UserManager<ApplicationUser> userManager)
        {
            _appDbContext = appDbContext;
            _logger = logger;
            _notificationService = notificationService;
            _zapSignService = zapSignService;
            _userSummaryFactory = userSummaryFactory;
            _userManager = userManager;
        }

        private async Task<Dictionary<string, UserSummaryResponse>> LoadCreatorsAsync(IEnumerable<Report> reports)
        {
            var ids = reports
                .SelectMany(r => new[] { 
                    r.SocialWorkerReport?.UserID, 
                    r.PsychologistReport?.UserID, 
                    r.PsychopedagogueReport?.UserID,
                    r.PsychologistReport?.PsychologistQuestionnaire?.UserID,
                    r.PsychopedagogueReport?.PsychopedagogueQuestionnaire?.UserID
                })
                .Where(id => !string.IsNullOrEmpty(id))
                .Select(id => id!)
                .Distinct()
                .ToList();

            if (ids.Count == 0) return new Dictionary<string, UserSummaryResponse>();

            var rows = await _userManager.Users
                .Where(u => ids.Contains(u.Id))
                .Select(u => new { u.Id, u.FullName, u.State, u.City, u.SchoolID })
                .ToListAsync();

            return rows.ToDictionary(
                u => u.Id,
                u => new UserSummaryResponse { ID = u.Id, FullName = u.FullName, State = u.State, City = u.City, SchoolID = u.SchoolID, Roles = new List<string>() }
            );
        }

        [AllowAnonymous]
        [HttpGet("revert-incomplete")]
        public async Task<IActionResult> RevertIncomplete()
        {
            var reports = await _appDbContext.Reports
                .Include(r => r.Student)
                .Include(r => r.SocialWorkerReport)
                .Include(r => r.PsychologistReport)
                    .ThenInclude(p => p.PsychologistQuestionnaire)
                .Include(r => r.PsychologistReport)
                    .ThenInclude(p => p.SnapIVQuestionnaire)
                .Include(r => r.PsychologistReport)
                    .ThenInclude(p => p.AdaptiveBehaviorScaleDI)
                .Include(r => r.PsychopedagogueReport)
                    .ThenInclude(p => p.PsychopedagogueQuestionnaire)
                .Include(r => r.PsychopedagogueReport)
                    .ThenInclude(p => p.CARSQuestionnaire)
                .Include(r => r.PsychopedagogueReport)
                    .ThenInclude(p => p.MCHATQuestionnaire)
                .Include(r => r.PsychopedagogueReport)
                    .ThenInclude(p => p.CognitiveAssessment)
                .Where(r => r.DocumentToken != null && r.DocumentToken != "")
                .ToListAsync();

            var incompleteReports = reports.Where(r => !r.IsReadyForUnification()).ToList();
            int revertedCount = 0;
            
            foreach (var report in incompleteReports)
            {
                if (!string.IsNullOrEmpty(report.DocumentToken))
                {
                    await _zapSignService.DeleteDoc(report.ID, "report");
                }

                report.DocumentToken = null;
                report.SignedFile = null;
                report.SocialWorkerSignUrl = null;
                report.PsychologistSignUrl = null;
                report.PsychopedagogueSignUrl = null;
                report.UnifiedAt = null;
                report.ReportConfirmed = false;
                report.SocialWorkerSign = false;
                report.PsychologistSign = false;
                report.PsychopedagogueSign = false;
                report.SocialWorkerSignedAt = null;
                report.PsychologistSignedAt = null;
                report.PsychopedagogueSignedAt = null;
                report.DocumentStatus = Status.InProgress;
                report.ReportStatus = Status.InProgress;

                _appDbContext.Reports.Update(report);
                revertedCount++;
            }

            await _appDbContext.SaveChangesAsync();

            return Ok(new { Message = "Sincronização reversa concluída.", RevertedCount = revertedCount });
        }

        /// <summary>
        /// Endpoint admin-only que marca como "Assinatura Bloqueada" todos os relatórios
        /// completos criados até 17/08/2026 que nunca foram assinados via ZapSign.
        /// Para usuários comuns, esses relatórios aparecerão como "Assinado".
        /// Para administradores, aparecerão com status "Assinatura Bloqueada".
        /// Deve ser executado uma única vez após o deploy desta funcionalidade.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("block-old-signatures")]
        public async Task<IActionResult> BlockOldSignatures()
        {
            try
            {
                // Data de corte: relatórios criados ANTES de 18/08/2026 (em UTC)
                var cutoffDate = new DateTime(2026, 8, 18, 0, 0, 0, DateTimeKind.Utc);

                var reports = await _appDbContext.Reports
                    .Where(r =>
                        r.CreatedAt < cutoffDate &&
                        (r.ReportStatus == Status.Complete || r.UnifiedAt != null) &&
                        r.SignatureBlocked == false &&
                        !(r.DocumentStatus == Status.Complete || (r.SocialWorkerSign && r.PsychologistSign && r.PsychopedagogueSign)))
                    .ToListAsync();

                int blockedCount = 0;

                foreach (var report in reports)
                {
                    report.SignatureBlocked = true;
                    _appDbContext.Reports.Update(report);
                    blockedCount++;
                }

                await _appDbContext.SaveChangesAsync();

                _logger.LogInformation(
                    "BlockOldSignatures executado pelo admin {UserId}. {Count} relatório(s) marcado(s) como Assinatura Bloqueada.",
                    CurrentUserId, blockedCount);

                return Ok(new
                {
                    Message = "Operação concluída com sucesso.",
                    BlockedCount = blockedCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao executar BlockOldSignatures");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Ocorreu um erro ao bloquear assinaturas antigas. {ex.Message}"
                });
            }
        }

        private IQueryable<Report> BuildReportsQuery(
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
            var isAdmin = User.IsInRole("Admin");

            var query = _appDbContext.Reports
                                     .Include(s => s.Student).ThenInclude(s => s!.School)
                                     .Include(r => r.SocialWorkerReport)
                                     .Include(r => r.PsychologistReport).ThenInclude(p => p.PsychologistQuestionnaire)
                                     .Include(r => r.PsychologistReport).ThenInclude(p => p.SnapIVQuestionnaire)
                                     .Include(r => r.PsychologistReport).ThenInclude(p => p.AdaptiveBehaviorScaleDI)
                                     .Include(r => r.PsychopedagogueReport).ThenInclude(p => p.PsychopedagogueQuestionnaire).ThenInclude(q => q.TypeOfDisability)
                                     .Include(r => r.PsychopedagogueReport).ThenInclude(p => p.PsychopedagogueQuestionnaire).ThenInclude(q => q.TypeOfDisorder)
                                     .Include(r => r.PsychopedagogueReport).ThenInclude(p => p.CARSQuestionnaire)
                                     .Include(r => r.PsychopedagogueReport).ThenInclude(p => p.MCHATQuestionnaire)
                                     .Include(r => r.PsychopedagogueReport).ThenInclude(p => p.CognitiveAssessment)
                                     .AsNoTracking()
                                     .AsSplitQuery();

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
                if ((isCoordinator && currentUser != null) || (isPrincipal && currentUser != null) || (isTeacher && currentUser != null) || (isMunicipalManager && currentUser != null) || (isSocialWorker && currentUser != null) || (isPsychologist && currentUser != null) || (isPsychopedagogue && currentUser != null))
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

        private IQueryable<Student> ApplyMyReportsFilter(
            IQueryable<Student> query,
            ApplicationUser user,
            string myStatus)
        {
            var isPsychologist = User.IsInRole("Psychologist");
            var isPsychopedagogue = User.IsInRole("Psychopedagogue");
            var isSocialWorker = User.IsInRole("SocialWork");
            var isAdmin = User.IsInRole("Admin");

            var checkPsychologist = isPsychologist || isAdmin;
            var checkPsychopedagogue = isPsychopedagogue || isAdmin;
            var checkSocialWorker = isSocialWorker || isAdmin;

            var cutoffDate = new DateTime(2026, 8, 18, 0, 0, 0, DateTimeKind.Utc);

            query = query.Where(student => _appDbContext.Reports.Any(r =>
                r.StudentID == student.ID && (
                    (checkPsychologist && r.PsychologistReport != null && (
                        r.PsychologistReport.UserID == CurrentUserId || 
                        (r.PsychologistReport.PsychologistQuestionnaire != null && r.PsychologistReport.PsychologistQuestionnaire.UserID == CurrentUserId)
                    )) ||
                    (checkPsychopedagogue && r.PsychopedagogueReport != null && (
                        r.PsychopedagogueReport.UserID == CurrentUserId || 
                        (r.PsychopedagogueReport.PsychopedagogueQuestionnaire != null && r.PsychopedagogueReport.PsychopedagogueQuestionnaire.UserID == CurrentUserId)
                    )) ||
                    (checkSocialWorker && r.SocialWorkerReport != null && r.SocialWorkerReport.UserID == CurrentUserId)
                )
            ));

            if (!string.IsNullOrWhiteSpace(myStatus) && !myStatus.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                if (myStatus.Equals("pending_fill", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(student => _appDbContext.Reports.Any(r =>
                        r.StudentID == student.ID &&
                        r.ReportStatus != Status.Complete && r.UnifiedAt == null &&
                        !r.SignatureBlocked && r.DocumentStatus != Status.Complete &&
                        !(r.SocialWorkerSign && r.PsychologistSign && r.PsychopedagogueSign) && (
                            (checkPsychologist && r.PsychologistReport != null && (
                                r.PsychologistReport.UserID == CurrentUserId || 
                                (r.PsychologistReport.PsychologistQuestionnaire != null && r.PsychologistReport.PsychologistQuestionnaire.UserID == CurrentUserId)
                            ) && (r.PsychologistReport.UnifiedAt == null || string.IsNullOrEmpty(r.PsychologistReport.Conclusion))) ||
                            (checkPsychopedagogue && r.PsychopedagogueReport != null && (
                                r.PsychopedagogueReport.UserID == CurrentUserId || 
                                (r.PsychopedagogueReport.PsychopedagogueQuestionnaire != null && r.PsychopedagogueReport.PsychopedagogueQuestionnaire.UserID == CurrentUserId)
                            ) && (r.PsychopedagogueReport.UnifiedAt == null || string.IsNullOrEmpty(r.PsychopedagogueReport.Conclusion))) ||
                            (checkSocialWorker && r.SocialWorkerReport != null && r.SocialWorkerReport.UserID == CurrentUserId && string.IsNullOrEmpty(r.SocialWorkerReport.Conclusion))
                        )
                    ));
                }
                else if (myStatus.Equals("pending_sign", StringComparison.OrdinalIgnoreCase))
                {
                    // Só é pendente de assinatura digital se o relatório foi criado A PARTIR de 18/08/2026 (ou já possui fluxo ativo de assinatura digital),
                    // não está com assinatura bloqueada e o documento não está marcado como Completo
                    query = query.Where(student => _appDbContext.Reports.Any(r =>
                        r.StudentID == student.ID &&
                        (r.ReportStatus == Status.Complete || r.UnifiedAt != null) &&
                        !r.SignatureBlocked &&
                        r.DocumentStatus != Status.Complete &&
                        !((r.SocialWorkerSign || r.SocialWorkerSignedAt != null) && (r.PsychologistSign || r.PsychologistSignedAt != null) && (r.PsychopedagogueSign || r.PsychopedagogueSignedAt != null)) &&
                        (r.CreatedAt >= cutoffDate || (r.DocumentToken != null && r.DocumentToken != "") || r.SocialWorkerSign || r.SocialWorkerSignedAt != null || r.PsychologistSign || r.PsychologistSignedAt != null || r.PsychopedagogueSign || r.PsychopedagogueSignedAt != null) && (
                            (checkPsychologist && r.PsychologistReport != null && (
                                r.PsychologistReport.UserID == CurrentUserId || 
                                (r.PsychologistReport.PsychologistQuestionnaire != null && r.PsychologistReport.PsychologistQuestionnaire.UserID == CurrentUserId)
                            ) && !r.PsychologistSign && r.PsychologistSignedAt == null) ||
                            (checkPsychopedagogue && r.PsychopedagogueReport != null && (
                                r.PsychopedagogueReport.UserID == CurrentUserId || 
                                (r.PsychopedagogueReport.PsychopedagogueQuestionnaire != null && r.PsychopedagogueReport.PsychopedagogueQuestionnaire.UserID == CurrentUserId)
                            ) && !r.PsychopedagogueSign && r.PsychopedagogueSignedAt == null) ||
                            (checkSocialWorker && r.SocialWorkerReport != null && r.SocialWorkerReport.UserID == CurrentUserId && !r.SocialWorkerSign && r.SocialWorkerSignedAt == null)
                        )
                    ));
                }
                else if (myStatus.Equals("signed", StringComparison.OrdinalIgnoreCase))
                {
                    // Considerado assinado se assinou digitalmente OU se é relatório unificado/completo legado (anterior a 18/08/2026 sem fluxo digital em aberto) / bloqueado / envelope completo
                    query = query.Where(student => _appDbContext.Reports.Any(r =>
                        r.StudentID == student.ID && (
                            (checkPsychologist && r.PsychologistReport != null && (
                                r.PsychologistReport.UserID == CurrentUserId || 
                                (r.PsychologistReport.PsychologistQuestionnaire != null && r.PsychologistReport.PsychologistQuestionnaire.UserID == CurrentUserId)
                            ) && (r.PsychologistSign || r.PsychologistSignedAt != null || ((r.ReportStatus == Status.Complete || r.UnifiedAt != null || r.SignatureBlocked) && (r.SignatureBlocked || r.DocumentStatus == Status.Complete || ((r.SocialWorkerSign || r.SocialWorkerSignedAt != null) && (r.PsychologistSign || r.PsychologistSignedAt != null) && (r.PsychopedagogueSign || r.PsychopedagogueSignedAt != null)) || (r.CreatedAt < cutoffDate && (r.DocumentToken == null || r.DocumentToken == "") && !r.SocialWorkerSign && r.SocialWorkerSignedAt == null && !r.PsychologistSign && r.PsychologistSignedAt == null && !r.PsychopedagogueSign && r.PsychopedagogueSignedAt == null))))) ||
                            (checkPsychopedagogue && r.PsychopedagogueReport != null && (
                                r.PsychopedagogueReport.UserID == CurrentUserId || 
                                (r.PsychopedagogueReport.PsychopedagogueQuestionnaire != null && r.PsychopedagogueReport.PsychopedagogueQuestionnaire.UserID == CurrentUserId)
                            ) && (r.PsychopedagogueSign || r.PsychopedagogueSignedAt != null || ((r.ReportStatus == Status.Complete || r.UnifiedAt != null || r.SignatureBlocked) && (r.SignatureBlocked || r.DocumentStatus == Status.Complete || ((r.SocialWorkerSign || r.SocialWorkerSignedAt != null) && (r.PsychologistSign || r.PsychologistSignedAt != null) && (r.PsychopedagogueSign || r.PsychopedagogueSignedAt != null)) || (r.CreatedAt < cutoffDate && (r.DocumentToken == null || r.DocumentToken == "") && !r.SocialWorkerSign && r.SocialWorkerSignedAt == null && !r.PsychologistSign && r.PsychologistSignedAt == null && !r.PsychopedagogueSign && r.PsychopedagogueSignedAt == null))))) ||
                            (checkSocialWorker && r.SocialWorkerReport != null && r.SocialWorkerReport.UserID == CurrentUserId && 
                                (r.SocialWorkerSign || r.SocialWorkerSignedAt != null || ((r.ReportStatus == Status.Complete || r.UnifiedAt != null || r.SignatureBlocked) && (r.SignatureBlocked || r.DocumentStatus == Status.Complete || ((r.SocialWorkerSign || r.SocialWorkerSignedAt != null) && (r.PsychologistSign || r.PsychologistSignedAt != null) && (r.PsychopedagogueSign || r.PsychopedagogueSignedAt != null)) || (r.CreatedAt < cutoffDate && (r.DocumentToken == null || r.DocumentToken == "") && !r.SocialWorkerSign && r.SocialWorkerSignedAt == null && !r.PsychologistSign && r.PsychologistSignedAt == null && !r.PsychopedagogueSign && r.PsychopedagogueSignedAt == null)))))
                        )
                    ));
                }
            }

            return query;
        }

        #region CREATE
        [NoSecretary]
        [HttpPost]
        public async Task<IActionResult> CreateReport([FromBody] ReportRequest model)
        {
            await using var tx = await _appDbContext.Database.BeginTransactionAsync();

            try
            {
                if (!ModelState.IsValid)
                {
                    return ValidationProblem();
                }

                var user = await _userManager.FindByIdAsync(CurrentUserId);

                if (user == null)
                {
                    return Unauthorized();
                }

                var reportAlreadyCreated = await _appDbContext.Reports.AsNoTracking()
                                                .AsSplitQuery()
                                                .Include(s => s.Student).ThenInclude(s => s!.School)
                                                .Include(r => r.SocialWorkerReport)
                                                .Include(r => r.PsychologistReport).ThenInclude(p => p!.PsychologistQuestionnaire).ThenInclude(u => u.User)
                                                .Include(r => r.PsychologistReport).ThenInclude(p => p!.SnapIVQuestionnaire).ThenInclude(u => u.User)
                                                .Include(r => r.PsychologistReport).ThenInclude(p => p!.AdaptiveBehaviorScaleDI).ThenInclude(u => u.User)
                                                .Include(r => r.PsychopedagogueReport).ThenInclude(p => p!.CARSQuestionnaire).ThenInclude(u => u!.User)
                                                .Include(r => r.PsychopedagogueReport).ThenInclude(p => p!.MCHATQuestionnaire).ThenInclude(u => u!.User)
                                                .Include(r => r.PsychopedagogueReport).ThenInclude(p => p!.PsychopedagogueQuestionnaire).ThenInclude(u => u.User)
                                                .Include(r => r.PsychopedagogueReport).ThenInclude(p => p!.CognitiveAssessment).ThenInclude(u => u.User)
                                                .Where(r => r.StudentID == model.StudentID)
                                                .OrderByDescending(r => r.CreatedAt)
                                                .FirstOrDefaultAsync();



                if (reportAlreadyCreated != null && (reportAlreadyCreated.SocialWorkerReport == null || reportAlreadyCreated.PsychopedagogueReport?.UnifiedAt == null || reportAlreadyCreated.PsychologistReport?.UnifiedAt == null))
                {
                    var response = await ReportMapper.ToResponse(reportAlreadyCreated, _userSummaryFactory);
                    return Ok(new ApiResponse<ReportResponse>()
                    {
                        Success = true,
                        Message = "Relatório unificado retornado com sucesso",
                        Data = response
                    });
                }

                var report = new Report
                {
                    StudentID = model.StudentID,
                    ReportStatus = Status.InProgress,
                };

                await _appDbContext.Reports.AddAsync(report);
                await _appDbContext.SaveChangesAsync();

                var isPsychologist = User.IsInRole("Psychologist");
                var isPsychopedagogue = User.IsInRole("Psychopedagogue");

                var psychopedagogueReport = new PsychopedagogueReport
                {
                    StudentID = model.StudentID,
                    UserID = isPsychopedagogue ? CurrentUserId : null,
                    ReportID = report.ID
                };

                var psychologistReport = new PsychologistReport
                {
                    StudentID = model.StudentID,
                    UserID = isPsychologist ? CurrentUserId : null,
                    ReportID = report.ID
                };

                await _appDbContext.PsychopedagogueReports.AddAsync(psychopedagogueReport);
                await _appDbContext.PsychologistReports.AddAsync(psychologistReport);
                
                report.PsychopedagogueReportID = psychopedagogueReport.ID;
                report.PsychologistReportID = psychologistReport.ID;

                await _appDbContext.SaveChangesAsync();
                await tx.CommitAsync();

                await _appDbContext.Logs.AddAsync(new Log
                {
                    LogLevel = Models.Enums.LogLevel.Info.ToString(),
                    Message = $"Relatório unificado criado com sucesso com ID {report.ID}",
                    Source = "ReportsController",
                    UserID = CurrentUserGuid,
                    EntityType = "Report",
                    EntityID = report.ID,
                    Details = "",
                    Action = "Create"
                });
                await _appDbContext.SaveChangesAsync();

                return StatusCode(201, new ApiResponse<object>()
                {
                    Success = true,
                    Message = "Relatório unificado criado com sucesso",
                    Data = new
                    {
                        report.ID
                    }
                });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, $"Erro ao criar relatório");
                return BadRequest(new ApiResponse<object>()
                {
                    Success = false,
                    Message = $"Ocorreu um erro ao criar o relatório unificado"
                });
            }
        }
        #endregion

        [HttpPost("sync-pending")]
        [Authorize]
        public async Task<IActionResult> SyncPendingReports()
        {
            try
            {
                var pendingReports = await _appDbContext.Reports
                    .Where(r => r.DocumentStatus != Status.Complete && !string.IsNullOrEmpty(r.DocumentToken))
                    .ToListAsync();

                var synced = 0;
                foreach (var report in pendingReports)
                {
                    try
                    {
                        if (await _zapSignService.SyncDocument(report.DocumentToken!))
                        {
                            synced++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Erro ao sincronizar relatório {ReportId}", report.ID);
                    }
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = $"{synced} de {pendingReports.Count} relatórios sincronizados.",
                    Data = new { Synced = synced, Total = pendingReports.Count }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao sincronizar relatórios pendentes.");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Erro ao sincronizar relatórios pendentes."
                });
            }
        }

        #region GET
        [HttpGet("my-summary")]
        public async Task<IActionResult> GetMyReportsSummary(
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
                if (user == null)
                {
                    return Unauthorized();
                }

                var baseQuery = BuildStudentBaseQuery(user, state, city, schoolID, searchTerm);
                if (year.HasValue)
                {
                    baseQuery = baseQuery.Where(s => s.CreatedAt.Year == year.Value);
                }
                if (month.HasValue)
                {
                    baseQuery = baseQuery.Where(s => s.CreatedAt.Month == month.Value);
                }

                var pendingSignQuery = ApplyMyReportsFilter(baseQuery, user, "pending_sign");
                var pendingSignCount = await pendingSignQuery.CountAsync();

                if (pendingSignCount > 0 && pendingSignCount <= 20)
                {
                    var studentIds = await pendingSignQuery.Select(s => s.ID).ToListAsync();
                    var pendingTokens = await _appDbContext.Reports
                        .Where(r => r.StudentID.HasValue && studentIds.Contains(r.StudentID.Value) && !string.IsNullOrEmpty(r.DocumentToken) && r.DocumentStatus != Status.Complete)
                        .Select(r => r.DocumentToken)
                        .ToListAsync();

                    foreach (var token in pendingTokens)
                    {
                        try
                        {
                            await _zapSignService.SyncDocument(token!);
                        }
                        catch { }
                    }
                }

                var total = await ApplyMyReportsFilter(baseQuery, user, "all").CountAsync();
                var pendingFill = await ApplyMyReportsFilter(baseQuery, user, "pending_fill").CountAsync();
                var pendingSign = await ApplyMyReportsFilter(baseQuery, user, "pending_sign").CountAsync();
                var signed = await ApplyMyReportsFilter(baseQuery, user, "signed").CountAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Resumo retornado com sucesso",
                    Data = new
                    {
                        total = total,
                        pendingFill = pendingFill,
                        pendingSign = pendingSign,
                        signed = signed
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter resumo de relatórios do usuário");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Erro ao obter resumo de relatórios do usuário"
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetReports([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string state = "", [FromQuery] string city = "", [FromQuery] string schoolID = "", [FromQuery] string searchTerm = "", [FromQuery] string status = "", [FromQuery] string pendingProfessional = "", [FromQuery] int? year = null, [FromQuery] int? month = null, [FromQuery] bool onlyMyReports = false, [FromQuery] string myStatus = "")
        {
            try
            {
                const int maxPageSize = 50;
                pageSize = Math.Min(pageSize, maxPageSize);
                page = Math.Max(page, 1);
                var skip = (page - 1) * pageSize;

                var user = await _userManager.FindByIdAsync(CurrentUserId);

                if (user == null)
                {
                    return Unauthorized();
                }

                var query = BuildStudentBaseQuery(user, state, city, schoolID, searchTerm);

                if (onlyMyReports)
                {
                    query = ApplyMyReportsFilter(query, user, myStatus);
                }

                if (!string.IsNullOrWhiteSpace(status) && !status.Equals("Todos", StringComparison.OrdinalIgnoreCase))
                {
                    if (status.Equals("Completo", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(student => _appDbContext.Reports.Any(r => r.StudentID == student.ID && (r.ReportStatus == Status.Complete || r.UnifiedAt != null || r.SignatureBlocked || r.DocumentStatus == Status.Complete || (r.SocialWorkerSign && r.PsychologistSign && r.PsychopedagogueSign))));
                    }
                    else if (status.Equals("Em Andamento", StringComparison.OrdinalIgnoreCase) || status.Equals("Incompleto", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!string.IsNullOrWhiteSpace(pendingProfessional) && !pendingProfessional.Equals("Todos", StringComparison.OrdinalIgnoreCase))
                        {
                            if (pendingProfessional.Equals("Psicologo", StringComparison.OrdinalIgnoreCase))
                            {
                                query = query.Where(student => _appDbContext.Reports.Any(r => r.StudentID == student.ID && (r.PsychologistReport == null || r.PsychologistReport.UnifiedAt == null)) && !_appDbContext.Reports.Any(r => r.StudentID == student.ID && (r.ReportStatus == Status.Complete || r.UnifiedAt != null || r.SignatureBlocked || r.DocumentStatus == Status.Complete || (r.SocialWorkerSign && r.PsychologistSign && r.PsychopedagogueSign))));
                            }
                            else if (pendingProfessional.Equals("Psicopedagogo", StringComparison.OrdinalIgnoreCase))
                            {
                                query = query.Where(student => _appDbContext.Reports.Any(r => r.StudentID == student.ID && (r.PsychopedagogueReport == null || r.PsychopedagogueReport.UnifiedAt == null)) && !_appDbContext.Reports.Any(r => r.StudentID == student.ID && (r.ReportStatus == Status.Complete || r.UnifiedAt != null || r.SignatureBlocked || r.DocumentStatus == Status.Complete || (r.SocialWorkerSign && r.PsychologistSign && r.PsychopedagogueSign))));
                            }
                            else if (pendingProfessional.Equals("AssistenteSocial", StringComparison.OrdinalIgnoreCase))
                            {
                                query = query.Where(student => _appDbContext.Reports.Any(r => r.StudentID == student.ID && r.SocialWorkerReportID == null) && !_appDbContext.Reports.Any(r => r.StudentID == student.ID && (r.ReportStatus == Status.Complete || r.UnifiedAt != null || r.SignatureBlocked || r.DocumentStatus == Status.Complete || (r.SocialWorkerSign && r.PsychologistSign && r.PsychopedagogueSign))));
                            }
                        }
                        else
                        {
                            query = query.Where(student => _appDbContext.Reports.Any(r => r.StudentID == student.ID) && !_appDbContext.Reports.Any(r => r.StudentID == student.ID && (r.ReportStatus == Status.Complete || r.UnifiedAt != null || r.SignatureBlocked || r.DocumentStatus == Status.Complete || (r.SocialWorkerSign && r.PsychologistSign && r.PsychopedagogueSign))));
                        }
                    }
                    else if (status.Equals("Não Iniciado", StringComparison.OrdinalIgnoreCase) || status.Equals("Nao Iniciado", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(student => !_appDbContext.Reports.Any(r => r.StudentID == student.ID));
                    }
                }
                else if (!string.IsNullOrWhiteSpace(pendingProfessional) && !pendingProfessional.Equals("Todos", StringComparison.OrdinalIgnoreCase))
                {
                    if (pendingProfessional.Equals("Psicologo", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(student => !_appDbContext.Reports.Any(r => r.StudentID == student.ID) || _appDbContext.Reports.Any(r => r.StudentID == student.ID && (r.PsychologistReport == null || r.PsychologistReport.UnifiedAt == null)));
                    }
                    else if (pendingProfessional.Equals("Psicopedagogo", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(student => !_appDbContext.Reports.Any(r => r.StudentID == student.ID) || _appDbContext.Reports.Any(r => r.StudentID == student.ID && (r.PsychopedagogueReport == null || r.PsychopedagogueReport.UnifiedAt == null)));
                    }
                    else if (pendingProfessional.Equals("AssistenteSocial", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(student => !_appDbContext.Reports.Any(r => r.StudentID == student.ID) || _appDbContext.Reports.Any(r => r.StudentID == student.ID && r.SocialWorkerReportID == null));
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
                    return Ok(new ApiResponse<List<ReportSummaryResponse>>()
                    {
                        Success = true,
                        Message = "Nenhum relatório encontrado",
                        Data = new List<ReportSummaryResponse>(),
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

                var reports = await _appDbContext.Reports
                                         .Include(s => s.Student).ThenInclude(s => s!.School)
                                         .Include(r => r.SocialWorkerReport)
                                         .Include(r => r.PsychologistReport).ThenInclude(p => p.PsychologistQuestionnaire)
                                         .Include(r => r.PsychologistReport).ThenInclude(p => p.SnapIVQuestionnaire)
                                         .Include(r => r.PsychologistReport).ThenInclude(p => p.AdaptiveBehaviorScaleDI)
                                         .Include(r => r.PsychopedagogueReport).ThenInclude(p => p.PsychopedagogueQuestionnaire).ThenInclude(q => q.TypeOfDisability)
                                         .Include(r => r.PsychopedagogueReport).ThenInclude(p => p.PsychopedagogueQuestionnaire).ThenInclude(q => q.TypeOfDisorder)
                                         .Include(r => r.PsychopedagogueReport).ThenInclude(p => p.CARSQuestionnaire)
                                         .Include(r => r.PsychopedagogueReport).ThenInclude(p => p.MCHATQuestionnaire)
                                         .Include(r => r.PsychopedagogueReport).ThenInclude(p => p.CognitiveAssessment)
                                         .AsNoTracking()
                                         .AsSplitQuery()
                                         .Where(r => r.StudentID.HasValue && studentIds.Contains(r.StudentID.Value))
                                         .OrderByDescending(r => r.CreatedAt)
                                         .ToListAsync();

                if (string.Equals(myStatus, "pending_sign", StringComparison.OrdinalIgnoreCase))
                {
                    var anySynced = false;
                    foreach (var r in reports.Where(x => !string.IsNullOrEmpty(x.DocumentToken) && x.DocumentStatus != Status.Complete))
                    {
                        try
                        {
                            if (await _zapSignService.SyncDocument(r.DocumentToken!))
                            {
                                anySynced = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Falha ao sincronizar documento pendente {Token}", r.DocumentToken);
                        }
                    }

                    if (anySynced)
                    {
                        reports = await _appDbContext.Reports
                            .Include(s => s.Student).ThenInclude(s => s!.School)
                            .Include(r => r.SocialWorkerReport)
                            .Include(r => r.PsychologistReport).ThenInclude(p => p.PsychologistQuestionnaire)
                            .Include(r => r.PsychologistReport).ThenInclude(p => p.SnapIVQuestionnaire)
                            .Include(r => r.PsychologistReport).ThenInclude(p => p.AdaptiveBehaviorScaleDI)
                            .Include(r => r.PsychopedagogueReport).ThenInclude(p => p.PsychopedagogueQuestionnaire).ThenInclude(q => q.TypeOfDisability)
                            .Include(r => r.PsychopedagogueReport).ThenInclude(p => p.PsychopedagogueQuestionnaire).ThenInclude(q => q.TypeOfDisorder)
                            .Include(r => r.PsychopedagogueReport).ThenInclude(p => p.CARSQuestionnaire)
                            .Include(r => r.PsychopedagogueReport).ThenInclude(p => p.MCHATQuestionnaire)
                            .Include(r => r.PsychopedagogueReport).ThenInclude(p => p.CognitiveAssessment)
                            .AsNoTracking()
                            .AsSplitQuery()
                            .Where(r => r.StudentID.HasValue && studentIds.Contains(r.StudentID.Value))
                            .OrderByDescending(r => r.CreatedAt)
                            .ToListAsync();
                    }
                }

                var creators = await LoadCreatorsAsync(reports);
                var response = new List<ReportSummaryResponse>();

                foreach (var student in pagedStudents)
                {
                    var report = reports.FirstOrDefault(r => r.StudentID == student.ID);
                    if (report != null)
                    {
                        response.Add(ReportMapper.ToSummaryResponse(report, creators));
                    }
                    else
                    {
                        var s = student;
                        s.School = student.School;
                        response.Add(ReportMapper.FromStudent(s));
                    }
                }

                return Ok(new ApiResponse<List<ReportSummaryResponse>>()
                {
                    Success = true,
                    Message = "Lista de relatórios retornada com sucesso",
                    Data = response,
                    Total = uniqueTotal,
                    UniqueTotal = uniqueTotal
                });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao retornar relatórios");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Ocorreu um erro ao retornar relatórios"
                });
            }
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllReportsNoPagination([FromQuery] string state = "", [FromQuery] string city = "", [FromQuery] string schoolID = "", [FromQuery] string searchTerm = "", [FromQuery] string status = "", [FromQuery] string pendingProfessional = "", [FromQuery] int? year = null, [FromQuery] int? month = null, [FromQuery] bool onlyMyReports = false, [FromQuery] string myStatus = "")
        {
            try
            {
                var user = await _userManager.FindByIdAsync(CurrentUserId);

                if (user == null)
                {
                    return Unauthorized();
                }

                var query = BuildStudentBaseQuery(user, state, city, schoolID, searchTerm);

                if (onlyMyReports)
                {
                    query = ApplyMyReportsFilter(query, user, myStatus);
                }

                if (!string.IsNullOrWhiteSpace(status) && !status.Equals("Todos", StringComparison.OrdinalIgnoreCase))
                {
                    if (status.Equals("Completo", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(student => _appDbContext.Reports.Any(r => r.StudentID == student.ID && (r.ReportStatus == Status.Complete || r.UnifiedAt != null || r.SignatureBlocked || r.DocumentStatus == Status.Complete || (r.SocialWorkerSign && r.PsychologistSign && r.PsychopedagogueSign))));
                    }
                    else if (status.Equals("Em Andamento", StringComparison.OrdinalIgnoreCase) || status.Equals("Incompleto", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!string.IsNullOrWhiteSpace(pendingProfessional) && !pendingProfessional.Equals("Todos", StringComparison.OrdinalIgnoreCase))
                        {
                            if (pendingProfessional.Equals("Psicologo", StringComparison.OrdinalIgnoreCase))
                            {
                                query = query.Where(student => _appDbContext.Reports.Any(r => r.StudentID == student.ID && (r.PsychologistReport == null || r.PsychologistReport.UnifiedAt == null)) && !_appDbContext.Reports.Any(r => r.StudentID == student.ID && (r.ReportStatus == Status.Complete || r.UnifiedAt != null || r.SignatureBlocked || r.DocumentStatus == Status.Complete || (r.SocialWorkerSign && r.PsychologistSign && r.PsychopedagogueSign))));
                            }
                            else if (pendingProfessional.Equals("Psicopedagogo", StringComparison.OrdinalIgnoreCase))
                            {
                                query = query.Where(student => _appDbContext.Reports.Any(r => r.StudentID == student.ID && (r.PsychopedagogueReport == null || r.PsychopedagogueReport.UnifiedAt == null)) && !_appDbContext.Reports.Any(r => r.StudentID == student.ID && (r.ReportStatus == Status.Complete || r.UnifiedAt != null || r.SignatureBlocked || r.DocumentStatus == Status.Complete || (r.SocialWorkerSign && r.PsychologistSign && r.PsychopedagogueSign))));
                            }
                            else if (pendingProfessional.Equals("AssistenteSocial", StringComparison.OrdinalIgnoreCase))
                            {
                                query = query.Where(student => _appDbContext.Reports.Any(r => r.StudentID == student.ID && r.SocialWorkerReportID == null) && !_appDbContext.Reports.Any(r => r.StudentID == student.ID && (r.ReportStatus == Status.Complete || r.UnifiedAt != null || r.SignatureBlocked || r.DocumentStatus == Status.Complete || (r.SocialWorkerSign && r.PsychologistSign && r.PsychopedagogueSign))));
                            }
                        }
                        else
                        {
                            query = query.Where(student => _appDbContext.Reports.Any(r => r.StudentID == student.ID) && !_appDbContext.Reports.Any(r => r.StudentID == student.ID && (r.ReportStatus == Status.Complete || r.UnifiedAt != null || r.SignatureBlocked || r.DocumentStatus == Status.Complete || (r.SocialWorkerSign && r.PsychologistSign && r.PsychopedagogueSign))));
                        }
                    }
                    else if (status.Equals("Não Iniciado", StringComparison.OrdinalIgnoreCase) || status.Equals("Nao Iniciado", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(student => !_appDbContext.Reports.Any(r => r.StudentID == student.ID));
                    }
                }
                else if (!string.IsNullOrWhiteSpace(pendingProfessional) && !pendingProfessional.Equals("Todos", StringComparison.OrdinalIgnoreCase))
                {
                    if (pendingProfessional.Equals("Psicologo", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(student => !_appDbContext.Reports.Any(r => r.StudentID == student.ID) || _appDbContext.Reports.Any(r => r.StudentID == student.ID && (r.PsychologistReport == null || r.PsychologistReport.UnifiedAt == null)));
                    }
                    else if (pendingProfessional.Equals("Psicopedagogo", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(student => !_appDbContext.Reports.Any(r => r.StudentID == student.ID) || _appDbContext.Reports.Any(r => r.StudentID == student.ID && (r.PsychopedagogueReport == null || r.PsychopedagogueReport.UnifiedAt == null)));
                    }
                    else if (pendingProfessional.Equals("AssistenteSocial", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(student => !_appDbContext.Reports.Any(r => r.StudentID == student.ID) || _appDbContext.Reports.Any(r => r.StudentID == student.ID && r.SocialWorkerReportID == null));
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
                    return Ok(new ApiResponse<List<ReportSummaryResponse>>()
                    {
                        Success = true,
                        Message = "Nenhum relatório encontrado",
                        Data = new List<ReportSummaryResponse>(),
                        Total = 0,
                        UniqueTotal = 0
                    });
                }

                var students = await query
                    .OrderBy(s => s.FullName)
                    .ToListAsync();

                var studentIds = students.Select(s => s.ID).ToList();

                var reports = await _appDbContext.Reports
                                         .Include(s => s.Student).ThenInclude(s => s!.School)
                                         .Include(r => r.SocialWorkerReport)
                                         .Include(r => r.PsychologistReport).ThenInclude(p => p.PsychologistQuestionnaire)
                                         .Include(r => r.PsychologistReport).ThenInclude(p => p.SnapIVQuestionnaire)
                                         .Include(r => r.PsychologistReport).ThenInclude(p => p.AdaptiveBehaviorScaleDI)
                                         .Include(r => r.PsychopedagogueReport).ThenInclude(p => p.PsychopedagogueQuestionnaire).ThenInclude(q => q.TypeOfDisability)
                                         .Include(r => r.PsychopedagogueReport).ThenInclude(p => p.PsychopedagogueQuestionnaire).ThenInclude(q => q.TypeOfDisorder)
                                         .Include(r => r.PsychopedagogueReport).ThenInclude(p => p.CARSQuestionnaire)
                                         .Include(r => r.PsychopedagogueReport).ThenInclude(p => p.MCHATQuestionnaire)
                                         .Include(r => r.PsychopedagogueReport).ThenInclude(p => p.CognitiveAssessment)
                                         .AsNoTracking()
                                         .AsSplitQuery()
                                         .Where(r => r.StudentID.HasValue && studentIds.Contains(r.StudentID.Value))
                                         .OrderByDescending(r => r.CreatedAt)
                                         .ToListAsync();

                var creators = await LoadCreatorsAsync(reports);
                var response = new List<ReportSummaryResponse>();

                foreach (var student in students)
                {
                    var report = reports.FirstOrDefault(r => r.StudentID == student.ID);
                    if (report != null)
                    {
                        response.Add(ReportMapper.ToSummaryResponse(report, creators));
                    }
                    else
                    {
                        var s = student;
                        s.School = student.School;
                        response.Add(ReportMapper.FromStudent(s));
                    }
                }

                return Ok(new ApiResponse<List<ReportSummaryResponse>>()
                {
                    Success = true,
                    Message = "Lista de relatórios retornada com sucesso",
                    Data = response,
                    Total = uniqueTotal,
                    UniqueTotal = uniqueTotal
                });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao retornar relatórios");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Ocorreu um erro ao retornar relatórios"
                });
            }
        }

        [NoSecretary]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetReport(Guid id)
        {
            try
            {
                Report? report = await _appDbContext.Reports.Include(s => s.Student).ThenInclude(s => s!.School)
                                                        .Include(r => r.SocialWorkerReport).ThenInclude(u => u!.User)
                                                        .Include(r => r.PsychologistReport).ThenInclude(u => u!.User)
                                                        .Include(r => r.PsychopedagogueReport).ThenInclude(u => u!.User)
                                                        .Include(r => r.SocialWorkerReport).ThenInclude(u => u!.Student)
                                                        .Include(r => r.PsychologistReport).ThenInclude(u => u!.Student)
                                                        .Include(r => r.PsychopedagogueReport).ThenInclude(u => u!.Student)
                                                        .Include(r => r.SocialWorkerReport).ThenInclude(m => m!.FamilyIncomeSource)
                                                        .Include(r => r.SocialWorkerReport).ThenInclude(m => m!.ReceivedGovernmentBenefits)
                                                        .Include(r => r.SocialWorkerReport).ThenInclude(m => m!.HealthConditions)
                                                        .Include(r => r.SocialWorkerReport).ThenInclude(m => m!.MainSchoolDifficulties)
                                                        .Include(r => r.SocialWorkerReport).ThenInclude(m => m!.ProceduresUsed)
                                                        .Include(r => r.SocialWorkerReport).ThenInclude(m => m!.Recommendations)
                                                        .Include(r => r.PsychologistReport).ThenInclude(p => p!.PsychologistQuestionnaire).ThenInclude(u => u.User)
                                                        .Include(r => r.PsychologistReport).ThenInclude(p => p!.SnapIVQuestionnaire)
                                                        .Include(r => r.PsychologistReport).ThenInclude(p => p!.AdaptiveBehaviorScaleDI)
                                                        .Include(r => r.PsychopedagogueReport).ThenInclude(p => p!.CARSQuestionnaire)
                                                        .Include(r => r.PsychopedagogueReport).ThenInclude(p => p!.MCHATQuestionnaire)
                                                        .Include(r => r.PsychopedagogueReport).ThenInclude(p => p!.PsychopedagogueQuestionnaire).ThenInclude(p => p.Recommendations)
                                                        .Include(r => r.PsychopedagogueReport).ThenInclude(p => p!.PsychopedagogueQuestionnaire).ThenInclude(p => p.TypeOfDisability)
                                                        .Include(r => r.PsychopedagogueReport).ThenInclude(p => p!.PsychopedagogueQuestionnaire).ThenInclude(p => p.TypeOfDisorder)
                                                        .Include(r => r.PsychopedagogueReport).ThenInclude(p => p!.PsychopedagogueQuestionnaire).ThenInclude(u => u.User)
                                                        .Include(r => r.PsychopedagogueReport).ThenInclude(p => p!.CognitiveAssessment)
                                                        .AsNoTracking()
                                                        .AsSplitQuery()
                                                        .FirstOrDefaultAsync(r => r.ID == id);

                if (report == null)
                {
                    return NotFound(new ApiResponse<object>()
                    {
                        Success = false,
                        Message = $"Relatório com ID {id} não encontrado"
                    });
                }

                // Security check
                var user = await _userManager.FindByIdAsync(CurrentUserId);
                if (user == null) return Unauthorized();

                var roles = await _userManager.GetRolesAsync(user);

                if (report.Student != null && !HasStudentAccess(user, report.Student, roles))
                {
                    return Forbid();
                }

                // Sincroniza o documento com o ZapSign se possuir token
                if (!string.IsNullOrEmpty(report.DocumentToken))
                {
                    var synced = await _zapSignService.SyncDocument(report.DocumentToken);
                    if (synced)
                    {
                        report = await _appDbContext.Reports.Include(s => s.Student).ThenInclude(s => s!.School)
                                                                .Include(r => r.SocialWorkerReport).ThenInclude(u => u!.User)
                                                                .Include(r => r.PsychologistReport).ThenInclude(u => u!.User)
                                                                .Include(r => r.PsychopedagogueReport).ThenInclude(u => u!.User)
                                                                .Include(r => r.SocialWorkerReport).ThenInclude(u => u!.Student)
                                                                .Include(r => r.PsychologistReport).ThenInclude(u => u!.Student)
                                                                .Include(r => r.PsychopedagogueReport).ThenInclude(u => u!.Student)
                                                                .Include(r => r.SocialWorkerReport).ThenInclude(m => m!.FamilyIncomeSource)
                                                                .Include(r => r.SocialWorkerReport).ThenInclude(m => m!.ReceivedGovernmentBenefits)
                                                                .Include(r => r.SocialWorkerReport).ThenInclude(m => m!.HealthConditions)
                                                                .Include(r => r.SocialWorkerReport).ThenInclude(m => m!.MainSchoolDifficulties)
                                                                .Include(r => r.SocialWorkerReport).ThenInclude(m => m!.ProceduresUsed)
                                                                .Include(r => r.SocialWorkerReport).ThenInclude(m => m!.Recommendations)
                                                                .Include(r => r.PsychologistReport).ThenInclude(p => p!.PsychologistQuestionnaire).ThenInclude(u => u.User)
                                                                .Include(r => r.PsychologistReport).ThenInclude(p => p!.SnapIVQuestionnaire)
                                                                .Include(r => r.PsychologistReport).ThenInclude(p => p!.AdaptiveBehaviorScaleDI)
                                                                .Include(r => r.PsychopedagogueReport).ThenInclude(p => p!.CARSQuestionnaire)
                                                                .Include(r => r.PsychopedagogueReport).ThenInclude(p => p!.MCHATQuestionnaire)
                                                                .Include(r => r.PsychopedagogueReport).ThenInclude(p => p!.PsychopedagogueQuestionnaire).ThenInclude(p => p.Recommendations)
                                                                .Include(r => r.PsychopedagogueReport).ThenInclude(p => p!.PsychopedagogueQuestionnaire).ThenInclude(p => p.TypeOfDisability)
                                                                .Include(r => r.PsychopedagogueReport).ThenInclude(p => p!.PsychopedagogueQuestionnaire).ThenInclude(p => p.TypeOfDisorder)
                                                                .Include(r => r.PsychopedagogueReport).ThenInclude(p => p!.PsychopedagogueQuestionnaire).ThenInclude(u => u.User)
                                                                .Include(r => r.PsychopedagogueReport).ThenInclude(p => p!.CognitiveAssessment)
                                                                .AsNoTracking()
                                                                .AsSplitQuery()
                                                                .FirstOrDefaultAsync(r => r.ID == id);
                        if (report == null)
                        {
                            return NotFound(new ApiResponse<object>()
                            {
                                Success = false,
                                Message = $"Relatório com ID {id} não encontrado após sincronização"
                            });
                        }
                    }
                }

                var cutoffDate = new DateTime(2026, 8, 18, 0, 0, 0, DateTimeKind.Utc);
                if (report.UnifiedAt != null && !report.SignatureBlocked && report.CreatedAt >= cutoffDate && report.DocumentStatus != Status.Complete && string.IsNullOrEmpty(report.DocumentToken))
                {
                    var resultDocCreation = await _zapSignService.CreateDoc(report.ID, "report");

                    if (!resultDocCreation)
                    {
                        _logger.LogWarning($"Documento no ZapSign não foi criado para o Relatório ID {report.ID}.");
                        var responseWithoutSign = ReportMapper.ToResponse(report, _userSummaryFactory);

                        return StatusCode(207, new ApiResponse<object>()
                        {
                            Success = true,
                            Message = "Relatório retornado com sucesso, mas não foi possível criar o documento para assinatura.",
                            Data = responseWithoutSign!
                        });
                    }

                    report = await _appDbContext.Reports.Include(s => s.Student).ThenInclude(s => s!.School)
                                                        .Include(r => r.SocialWorkerReport).ThenInclude(u => u!.User)
                                                        .Include(r => r.PsychologistReport)
                                                        .Include(r => r.PsychopedagogueReport)
                                                        .Include(r => r.SocialWorkerReport).ThenInclude(u => u!.Student)
                                                        .Include(r => r.PsychologistReport).ThenInclude(u => u!.Student)
                                                        .Include(r => r.PsychopedagogueReport).ThenInclude(u => u!.Student)
                                                        .Include(r => r.SocialWorkerReport).ThenInclude(m => m!.FamilyIncomeSource)
                                                        .Include(r => r.SocialWorkerReport).ThenInclude(m => m!.ReceivedGovernmentBenefits)
                                                        .Include(r => r.SocialWorkerReport).ThenInclude(m => m!.HealthConditions)
                                                        .Include(r => r.SocialWorkerReport).ThenInclude(m => m!.MainSchoolDifficulties)
                                                        .Include(r => r.SocialWorkerReport).ThenInclude(m => m!.ProceduresUsed)
                                                        .Include(r => r.SocialWorkerReport).ThenInclude(m => m!.Recommendations)
                                                        .Include(r => r.PsychologistReport).ThenInclude(p => p!.PsychologistQuestionnaire).ThenInclude(u => u.User)
                                                        .Include(r => r.PsychologistReport).ThenInclude(p => p!.SnapIVQuestionnaire)
                                                        .Include(r => r.PsychologistReport).ThenInclude(p => p!.AdaptiveBehaviorScaleDI)
                                                        .Include(r => r.PsychopedagogueReport).ThenInclude(p => p!.CARSQuestionnaire)
                                                        .Include(r => r.PsychopedagogueReport).ThenInclude(p => p!.MCHATQuestionnaire)
                                                        .Include(r => r.PsychopedagogueReport).ThenInclude(p => p!.PsychopedagogueQuestionnaire).ThenInclude(p => p.Recommendations)
                                                        .Include(r => r.PsychopedagogueReport).ThenInclude(p => p!.PsychopedagogueQuestionnaire).ThenInclude(p => p.TypeOfDisability)
                                                        .Include(r => r.PsychopedagogueReport).ThenInclude(p => p!.PsychopedagogueQuestionnaire).ThenInclude(p => p.TypeOfDisorder)
                                                        .Include(r => r.PsychopedagogueReport).ThenInclude(p => p!.PsychopedagogueQuestionnaire).ThenInclude(u => u.User)
                                                        .Include(r => r.PsychopedagogueReport).ThenInclude(p => p!.CognitiveAssessment)
                                                        .AsNoTracking()
                                                        .AsSplitQuery()
                                                        .FirstOrDefaultAsync(r => r.ID == id);

                    if (report == null)
                    {
                        return NotFound(new ApiResponse<object>()
                        {
                            Success = false,
                            Message = $"Relatório com ID {id} não encontrado"
                        });
                    }
                }

                var response = await ReportMapper.ToResponse(report, _userSummaryFactory);


                return Ok(new ApiResponse<ReportResponse>()
                {
                    Success = true,
                    Message = "Relatório unificado retornado com sucesso",
                    Data = response
                });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao retornar relatório");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Ocorreu um erro ao retornar relatório"
                });
            }
        }

        [NoSecretary]
        [HttpPatch("{id}/conclusions")]
        public async Task<IActionResult> UpdateConclusions(Guid id, [FromBody] UpdateReportConclusionsRequest model)
        {
            try
            {
                var isAdmin = User.IsInRole("Admin");

                var report = await _appDbContext.Reports
                    .Include(r => r.SocialWorkerReport)
                    .Include(r => r.PsychologistReport)
                    .Include(r => r.PsychopedagogueReport)
                    .FirstOrDefaultAsync(r => r.ID == id);

                if (report == null)
                {
                    return NotFound(new ApiResponse<object> { Success = false, Message = "Relatório não encontrado" });
                }

                if (model.SocialWorkerConclusion != null && report.SocialWorkerReport != null)
                {
                    if (!isAdmin && report.SocialWorkerReport.UserID != null && !string.Equals(report.SocialWorkerReport.UserID, CurrentUserId, StringComparison.Ordinal))
                        return Forbid();

                    report.SocialWorkerReport.Conclusion = model.SocialWorkerConclusion;
                    report.SocialWorkerReport.UserID = CurrentUserId;
                    report.SocialWorkerReport.UpdatedAt = DateTime.UtcNow;
                }

                if (model.PsychologistConclusion != null && report.PsychologistReport != null)
                {
                    if (!isAdmin && report.PsychologistReport.UserID != null && !string.Equals(report.PsychologistReport.UserID, CurrentUserId, StringComparison.Ordinal))
                        return Forbid();

                    report.PsychologistReport.Conclusion = model.PsychologistConclusion;
                    report.PsychologistReport.UserID = CurrentUserId;
                    report.PsychologistReport.UpdatedAt = DateTime.UtcNow;
                }

                if (model.PsychopedagogueConclusion != null && report.PsychopedagogueReport != null)
                {
                    if (!isAdmin && report.PsychopedagogueReport.UserID != null && !string.Equals(report.PsychopedagogueReport.UserID, CurrentUserId, StringComparison.Ordinal))
                        return Forbid();

                    report.PsychopedagogueReport.Conclusion = model.PsychopedagogueConclusion;
                    report.PsychopedagogueReport.UserID = CurrentUserId;
                    report.PsychopedagogueReport.UpdatedAt = DateTime.UtcNow;
                }

                await _appDbContext.SaveChangesAsync();

                return Ok(new ApiResponse<object> { Success = true, Message = "Conclusões atualizadas com sucesso" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar conclusões");
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Erro ao atualizar conclusões" });
            }
        }

        [NoSecretary]
        [HttpGet("student/{studentID}")]
        public async Task<IActionResult> GetReportsByStudent(Guid studentID)
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

                var reports = await _appDbContext.Reports.Include(p => p.PsychopedagogueReport).ThenInclude(u => u != null ? u.User : null)
                                                         .Include(p => p.PsychologistReport).ThenInclude(u => u != null ? u.User : null)
                                                         .Include(s => s.SocialWorkerReport).ThenInclude(u => u != null ? u.User : null)
                                                         .Include(s => s.Student).ThenInclude(s => s!.School)
                                                         .AsNoTracking()
                                                         .AsSplitQuery()
                                                         .Where(temp => temp.StudentID == studentID).ToListAsync();

                if (reports.Count == 0)
                {
                    return Ok(new ApiResponse<List<ReportSummaryResponse>>()
                    {
                        Success = true,
                        Message = "Nenhum relatório encontrado",
                        Data = new List<ReportSummaryResponse>(),
                        Total = 0
                    });
                }

                var response = await Task.WhenAll(reports.Select(r => ReportMapper.ToResponse(r, _userSummaryFactory)));

                return Ok(new ApiResponse<List<ReportResponse>>()
                {
                    Success = true,
                    Message = "Lista de relatórios unificados retornada com sucesso",
                    Data = response.ToList()
                });

            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message}");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Ocorreu um erro ao retornar relatório"
                });
            }
        }
        #endregion

        #region DELETE
        [NoSecretary]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReport(Guid id)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return ValidationProblem();
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var report = await _appDbContext.Reports.Include(p => p.PsychologistReport).ThenInclude(pr => pr!.AdaptiveBehaviorScaleDI)
                                                        .Include(p => p.PsychologistReport).ThenInclude(pr => pr!.SnapIVQuestionnaire)
                                                        .Include(p => p.PsychologistReport).ThenInclude(pr => pr!.PsychologistQuestionnaire)
                                                        .Include(p => p.PsychopedagogueReport).ThenInclude(pp => pp!.PsychopedagogueQuestionnaire)
                                                        .Include(p => p.PsychopedagogueReport).ThenInclude(pp => pp!.MCHATQuestionnaire)
                                                        .Include(p => p.PsychopedagogueReport).ThenInclude(pp => pp!.CARSQuestionnaire)
                                                        .Include(p => p.PsychopedagogueReport).ThenInclude(pp => pp!.CognitiveAssessment)
                                                        .Include(p => p.SocialWorkerReport).ThenInclude(sw => sw!.FamilyIncomeSource)
                                                        .Include(p => p.SocialWorkerReport).ThenInclude(sw => sw!.ReceivedGovernmentBenefits)
                                                        .Include(p => p.SocialWorkerReport).ThenInclude(sw => sw!.HealthConditions)
                                                        .Include(p => p.SocialWorkerReport).ThenInclude(sw => sw!.MainSchoolDifficulties)
                                                        .Include(p => p.SocialWorkerReport).ThenInclude(sw => sw!.ProceduresUsed)
                                                        .Include(p => p.SocialWorkerReport).ThenInclude(sw => sw!.Recommendations)
                                                        .FirstOrDefaultAsync(temp => temp.ID == id);

                if (report == null)
                {
                    return NotFound(new ApiResponse<object>()
                    {
                        Success = false,
                        Message = $"Relatório com ID {id} não encontrado"
                    });
                }

                if (!User.IsInRole("Admin"))
                {
                    var isCreator =
                        (report.SocialWorkerReport != null && string.Equals(report.SocialWorkerReport.UserID, CurrentUserId, StringComparison.Ordinal)) ||
                        (report.PsychologistReport != null && string.Equals(report.PsychologistReport.UserID, CurrentUserId, StringComparison.Ordinal)) ||
                        (report.PsychopedagogueReport != null && string.Equals(report.PsychopedagogueReport.UserID, CurrentUserId, StringComparison.Ordinal));

                    if (!isCreator)
                    {
                        return Forbid();
                    }
                }

                if (report.PsychologistReport != null)
                {
                    if (report.PsychologistReport.AdaptiveBehaviorScaleDI != null)
                        _appDbContext.AdaptiveBehaviorScalesDI.Remove(report.PsychologistReport.AdaptiveBehaviorScaleDI);

                    if (report.PsychologistReport.SnapIVQuestionnaire != null)
                        _appDbContext.SnapIVQuestionnaires.Remove(report.PsychologistReport.SnapIVQuestionnaire);

                    if (report.PsychologistReport.PsychologistQuestionnaire != null)
                        _appDbContext.PsychologistQuestionnaires.Remove(report.PsychologistReport.PsychologistQuestionnaire);

                    _appDbContext.PsychologistReports.Remove(report.PsychologistReport);
                }  
                    

                if (report.PsychopedagogueReport != null)
                {
                    if (report.PsychopedagogueReport.CognitiveAssessment != null)
                        _appDbContext.CognitiveAssessments.Remove(report.PsychopedagogueReport.CognitiveAssessment);

                    if (report.PsychopedagogueReport.MCHATQuestionnaire != null)
                        _appDbContext.MCHATQuestionnaires.Remove(report.PsychopedagogueReport.MCHATQuestionnaire);

                    if (report.PsychopedagogueReport.CARSQuestionnaire != null)
                        _appDbContext.CARSQuestionnaires.Remove(report.PsychopedagogueReport.CARSQuestionnaire);

                    if (report.PsychopedagogueReport.PsychopedagogueQuestionnaire != null)
                        _appDbContext.PsychopedagogueQuestionnaires.Remove(report.PsychopedagogueReport.PsychopedagogueQuestionnaire);

                    _appDbContext.PsychopedagogueReports.Remove(report.PsychopedagogueReport);
                }
                   

                if (report.SocialWorkerReport != null)
                {
                    _appDbContext.SocialWorkerReport_FamilyIncomeSources.RemoveRange(report.SocialWorkerReport.FamilyIncomeSource);
                    _appDbContext.SocialWorkerReport_GovernmentBenefits.RemoveRange(report.SocialWorkerReport.ReceivedGovernmentBenefits);
                    _appDbContext.SocialWorkerReport_HealthConditions.RemoveRange(report.SocialWorkerReport.HealthConditions);
                    _appDbContext.SocialWorkerReport_MainSchoolDifficulties.RemoveRange(report.SocialWorkerReport.MainSchoolDifficulties);
                    _appDbContext.SocialWorkerReport_Procedures.RemoveRange(report.SocialWorkerReport.ProceduresUsed);
                    _appDbContext.SocialWorkerReport_Recommendations.RemoveRange(report.SocialWorkerReport.Recommendations);

                    _appDbContext.SocialWorkerReports.Remove(report.SocialWorkerReport);
                }
                    

                _appDbContext.Reports.Remove(report);
                await _appDbContext.SaveChangesAsync();

                await _appDbContext.Logs.AddAsync(new Log
                {
                    LogLevel = Models.Enums.LogLevel.Info.ToString(),
                    Message = $"Relatório unificado excluido com sucesso com ID {report.ID}",
                    Source = "ReportsController",
                    UserID = Guid.Parse(userId!),
                    EntityType = "Report",
                    EntityID = report.ID,
                    Details = "",
                    Action = "Delete"
                });
                await _appDbContext.SaveChangesAsync();

                var resultDocDelete = await _zapSignService.DeleteDoc(report.ID, "report");

                if (!resultDocDelete)
                {
                    _logger.LogWarning($"Documento no ZapSign não foi excluído para o relatório ID {report.ID}.");

                    return StatusCode(207, new ApiResponse<object>()
                    {
                        Success = true,
                        Message = "Relatório unificado excluído com sucesso, mas não foi possível excluir o documento.",
                        Data = new
                        {
                            report.ID
                        }
                    });
                }

                _logger.LogInformation($"Documento no ZapSign excluído com sucesso para o relatório ID {report.ID}.");

                return Ok(new ApiResponse<object>()
                {
                    Success = true,
                    Message = "Relatório unificado exclúido com sucesso",
                    Data = new
                    {
                        report.ID
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao excluir relatório com id {id}");
                return BadRequest(new ApiResponse<object>()
                {
                    Success = false,
                    Message = $"Ocorreu um erro ao excluir o relatório unificado com id {id}"
                });
            }
        }
        #endregion
    }
}
