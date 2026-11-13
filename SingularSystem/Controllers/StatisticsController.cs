using SingularSystem.Domain.Tenancy;
using SingularSystem.DTOs.Reports;
using SingularSystem.DTOs.Statistics;
using SingularSystem.Extensions;
using SingularSystem.Mappers;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Linq;
using System.Security.Claims;

namespace SingularSystem.Controllers
{
    [Authorize]
    [Route("api/v1/[controller]")]
    [ApiController]
    public class StatisticsController : ControllerBase
    {
        private readonly AppDbContext _appDbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<StatisticsController> _logger;
        private readonly ITenantContext _tenantContext;

        public StatisticsController(AppDbContext appDbContext, ILogger<StatisticsController> logger, UserManager<ApplicationUser> userManager, ITenantContext tenantContext)
        {
            _appDbContext = appDbContext;
            _userManager = userManager;
            _logger = logger;
            _tenantContext = tenantContext;
        }

        private int CalculateAge(DateTime birthDate)
        {
            var today = DateTime.Today;
            var age = today.Year - birthDate.Year;
            if (birthDate.Date > today.AddYears(-age)) age--;
            return age;
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardSummary([FromQuery] DashboardFilter filter)
        {
            try
            {
                var referenceDate = DateTime.Now;
                int? targetYear = filter.Year;
                int? targetMonth = filter.Month;

                if (targetYear.HasValue)
                {
                    if (targetMonth.HasValue)
                    {
                        referenceDate = new DateTime(targetYear.Value, targetMonth.Value, 1).AddMonths(1).AddTicks(-1);
                    }
                    else
                    {
                        if (targetYear.Value == DateTime.Now.Year)
                        {
                            referenceDate = DateTime.Now;
                        }
                        else
                        {
                            referenceDate = new DateTime(targetYear.Value, 12, 31, 23, 59, 59);
                        }
                    }
                }
                else if (targetMonth.HasValue)
                {
                    targetYear = DateTime.Now.Year;
                    referenceDate = new DateTime(targetYear.Value, targetMonth.Value, 1).AddMonths(1).AddTicks(-1);
                }

                var currentYear = targetYear ?? DateTime.Now.Year;
                var fullMonths = Enumerable.Range(1, 12)
                           .Select(m => new DateTime(1, m, 1).ToString("MMMM", new CultureInfo("pt-BR")))
                           .ToList();

                var schools = await _appDbContext.Schools.ToListAsync();
                var students = await _appDbContext.Students.Include(s => s.School).Where(s => s.IsActive).ToListAsync();
                var reports = await _appDbContext.Reports
                    .AsSplitQuery()
                    .Include(s => s.Student).ThenInclude(s => s!.School)
                    .Include(r => r.SocialWorkerReport)
                    .Include(r => r.PsychologistReport).ThenInclude(pr => pr!.PsychologistQuestionnaire)
                    .Include(r => r.PsychopedagogueReport).ThenInclude(p => p!.PsychopedagogueQuestionnaire).ThenInclude(q => q.TypeOfDisability)
                    .Include(r => r.PsychopedagogueReport).ThenInclude(p => p!.PsychopedagogueQuestionnaire).ThenInclude(q => q.TypeOfDisorder)
                    .ToListAsync();
                var municipalityTargets = await _appDbContext.MunicipalityStudentTargets.ToListAsync();
                var paees = await _appDbContext.PAEEs.Include(s => s.Student).Include(s => s.Student!.School).ToListAsync();
                var peis = await _appDbContext.PEIs.Include(s => s.Student).Include(s => s.Student!.School).ToListAsync();
                var regularTeacherScreenings = await _appDbContext.RegularTeacherScreenings.Include(s => s.Student).Include(s => s.Student!.School).ToListAsync();
                var regularTeacherQuestionnaires = await _appDbContext.RegularTeacherQuestionnaires.Include(s => s.Student).Include(s => s.Student!.School).ToListAsync();
                var regularTeacherCaseStudies = await _appDbContext.RegularTeacherEvaluationDocuments.Include(s => s.Student).Include(s => s.Student!.School).ToListAsync();

                if (_tenantContext.IsPrivatePlan)
                {
                    students = students.Where(s => s.TenantId == _tenantContext.TenantId).ToList();
                    reports = reports.Where(s => s.Student != null && s.Student.TenantId == _tenantContext.TenantId).ToList();
                    paees = paees.Where(s => s.Student != null && s.Student.TenantId == _tenantContext.TenantId).ToList();
                    peis = peis.Where(s => s.Student != null && s.Student.TenantId == _tenantContext.TenantId).ToList();
                    regularTeacherScreenings = regularTeacherScreenings.Where(s => s.Student != null && s.Student.TenantId == _tenantContext.TenantId).ToList();
                    regularTeacherQuestionnaires = regularTeacherQuestionnaires.Where(s => s.Student != null && s.Student.TenantId == _tenantContext.TenantId).ToList();
                    regularTeacherCaseStudies = regularTeacherCaseStudies.Where(s => s.Student != null && s.Student.TenantId == _tenantContext.TenantId).ToList();
                    var schoolIds = students.Where(s => s.SchoolID.HasValue).Select(s => s.SchoolID!.Value).ToHashSet();
                    schools = schools.Where(s => schoolIds.Contains(s.ID)).ToList();
                    municipalityTargets = new List<MunicipalityStudentTarget>();
                }

                if (filter.Sex != null)
                {
                    students = students
                            .Where(s => s.Sex.GetDisplayName() == filter.Sex)
                            .ToList();
                }

                if (filter.State != null)
                {
                    students = students
                            .Where(s => Helpers.StateMatches(s.School?.State, filter.State))
                            .ToList();

                    schools = schools.Where(s => Helpers.StateMatches(s.State, filter.State))
                            .ToList();
                    municipalityTargets = municipalityTargets
                            .Where(m => Helpers.StateMatches(m.State, filter.State))
                            .ToList();

                    reports = reports.Where(s => Helpers.StateMatches(s.Student?.School?.State, filter.State)).ToList();

                    paees = paees.Where(s => Helpers.StateMatches(s.Student?.School?.State, filter.State)).ToList();
                }

                if (filter.City != null)
                {
                    students = students
                            .Where(s => Helpers.CityMatches(s.School?.City, filter.City))
                            .ToList();

                    schools = schools.Where(s => Helpers.CityMatches(s.City, filter.City))
                            .ToList();
                    municipalityTargets = municipalityTargets
                            .Where(m => Helpers.CityMatches(m.City, filter.City))
                            .ToList();

                    reports = reports.Where(s => Helpers.CityMatches(s.Student?.School?.City, filter.City)).ToList();

                    paees = paees.Where(s => Helpers.CityMatches(s.Student?.School?.City, filter.City)).ToList();
                }

                if (filter.SchoolID != null)
                {
                    students = students.Where(s => s.SchoolID == filter.SchoolID).ToList();

                    reports = reports.Where(s => s.Student != null && s.Student.SchoolID.Equals(filter.SchoolID)).ToList();

                    paees = paees.Where(s => s.Student != null && s.Student.SchoolID.Equals(filter.SchoolID)).ToList();
                }

                if (filter.MinimalAge != null)
                {
                    students = students.Where(s => (DateTime.Now.Year - s.DateOfBirth.Year -
                         (DateTime.Now.DayOfYear < s.DateOfBirth.DayOfYear ? 1 : 0)) >= filter.MinimalAge).ToList();
                }

                if (filter.MaximumAge != null)
                {
                    students = students.Where(s => (DateTime.Now.Year - s.DateOfBirth.Year -
                         (DateTime.Now.DayOfYear < s.DateOfBirth.DayOfYear ? 1 : 0)) < filter.MaximumAge).ToList();
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

                // If user is linked to a school but City or State is null/empty, fallback to the school's location
                if (user.SchoolID.HasValue && (string.IsNullOrWhiteSpace(user.City) || string.IsNullOrWhiteSpace(user.State)))
                {
                    var userSchool = await _appDbContext.Schools.FindAsync(user.SchoolID.Value);
                    if (userSchool != null)
                    {
                        if (string.IsNullOrWhiteSpace(user.City)) user.City = userSchool.City;
                        if (string.IsNullOrWhiteSpace(user.State)) user.State = userSchool.State;
                    }
                }

                // Normalize user state/city to match database format in memory
                if (!string.IsNullOrWhiteSpace(user.State))
                {
                    user.State = Helpers.NormalizeState(user.State);
                }
                if (!string.IsNullOrWhiteSpace(user.City))
                {
                    user.City = Helpers.NormalizeCity(user.City);
                }

                var userRoles = await _userManager.GetRolesAsync(user);
                var isAdmin = userRoles.Contains("Admin");
                var isSocialWorkUser = userRoles.Contains("SocialWork");
                var isPsychologistUser = userRoles.Contains("Psychologist");
                var isPsychopedagogueUser = userRoles.Contains("Psychopedagogue");

                if (_tenantContext.IsPrivatePlan)
                {
                    students = students.Where(s => s.TenantId == _tenantContext.TenantId).ToList();
                    reports = reports.Where(s => s.Student != null && s.Student.TenantId == _tenantContext.TenantId).ToList();
                    paees = paees.Where(s => s.Student != null && s.Student.TenantId == _tenantContext.TenantId).ToList();
                    var schoolIds = students.Where(s => s.SchoolID.HasValue).Select(s => s.SchoolID!.Value).ToHashSet();
                    schools = schools.Where(s => schoolIds.Contains(s.ID)).ToList();
                    municipalityTargets = new List<MunicipalityStudentTarget>();
                }
                else if (!isAdmin)
                {
                    if (userRoles.Contains("Principal") || userRoles.Contains("Psychologist") || userRoles.Contains("SocialWork") ||
                             userRoles.Contains("MunicipalManager") || userRoles.Contains("Secretary") ||
                             userRoles.Contains("Psychopedagogue") || userRoles.Contains("Teacher") || userRoles.Contains("Coordinator"))
                    {
                        schools = schools.Where(s => Helpers.StateMatches(s.State, user.State) && Helpers.CityMatches(s.City, user.City)).ToList();
                        students = students.Where(s => 
                            (Helpers.CityMatches(s.City, user.City) && Helpers.StateMatches(s.State, user.State)) || 
                            (s.School != null && Helpers.CityMatches(s.School.City, user.City) && Helpers.StateMatches(s.School.State, user.State))
                        ).ToList();
                        reports = reports.Where(s => s.Student != null && (
                            (Helpers.CityMatches(s.Student.City, user.City) && Helpers.StateMatches(s.Student.State, user.State)) || 
                            (s.Student.School != null && Helpers.CityMatches(s.Student.School.City, user.City) && Helpers.StateMatches(s.Student.School.State, user.State))
                        )).ToList();
                        paees = paees.Where(s => s.Student != null && (
                            (Helpers.CityMatches(s.Student.City, user.City) && Helpers.StateMatches(s.Student.State, user.State)) || 
                            (s.Student.School != null && Helpers.CityMatches(s.Student.School.City, user.City) && Helpers.StateMatches(s.Student.School.State, user.State))
                        )).ToList();
                        municipalityTargets = municipalityTargets.Where(m =>
                            Helpers.CityMatches(m.City, user.City) &&
                            Helpers.StateMatches(m.State, user.State)).ToList();
                    }
                    else
                    {
                        // Default security filter for other roles
                        schools = schools.Where(s => Helpers.StateMatches(s.State, user.State) && Helpers.CityMatches(s.City, user.City)).ToList();
                        students = students.Where(s => 
                            (Helpers.CityMatches(s.City, user.City) && Helpers.StateMatches(s.State, user.State)) || 
                            (s.School != null && Helpers.CityMatches(s.School.City, user.City) && Helpers.StateMatches(s.School.State, user.State))
                        ).ToList();
                        reports = reports.Where(s => s.Student != null && (
                            (Helpers.CityMatches(s.Student.City, user.City) && Helpers.StateMatches(s.Student.State, user.State)) || 
                            (s.Student.School != null && Helpers.CityMatches(s.Student.School.City, user.City) && Helpers.StateMatches(s.Student.School.State, user.State))
                        )).ToList();
                        paees = paees.Where(s => s.Student != null && (
                            (Helpers.CityMatches(s.Student.City, user.City) && Helpers.StateMatches(s.Student.State, user.State)) || 
                            (s.Student.School != null && Helpers.CityMatches(s.Student.School.City, user.City) && Helpers.StateMatches(s.Student.School.State, user.State))
                        )).ToList();
                    }
                }

                var intrinsicStudentIds = students.Select(s => s.ID).ToHashSet();

                // Transitively filter other entities by matching students
                reports = reports.Where(r => r.StudentID != null && intrinsicStudentIds.Contains(r.StudentID.Value)).ToList();
                paees = paees.Where(p => p.StudentID != null && intrinsicStudentIds.Contains(p.StudentID.Value)).ToList();
                peis = peis.Where(p => p.StudentID != null && intrinsicStudentIds.Contains(p.StudentID.Value)).ToList();
                regularTeacherScreenings = regularTeacherScreenings.Where(r => r.StudentID != null && intrinsicStudentIds.Contains(r.StudentID.Value)).ToList();
                regularTeacherQuestionnaires = regularTeacherQuestionnaires.Where(r => r.StudentID != null && intrinsicStudentIds.Contains(r.StudentID.Value)).ToList();
                regularTeacherCaseStudies = regularTeacherCaseStudies.Where(r => r.StudentID != null && intrinsicStudentIds.Contains(r.StudentID.Value)).ToList();

                // Apply temporal filters up to referenceDate (cumulative snapshot)
                students = students.Where(s => s.CreatedAt <= referenceDate).ToList();
                reports = reports.Where(r => r.CreatedAt <= referenceDate).ToList();
                paees = paees.Where(p => (p.SignedAt ?? p.UnifiedAt ?? p.UpdatedAt ?? p.CreatedAt) <= referenceDate).ToList();
                peis = peis.Where(p => (p.SignedAt ?? p.UnifiedAt ?? p.UpdatedAt ?? p.CreatedAt) <= referenceDate).ToList();
                regularTeacherScreenings = regularTeacherScreenings.Where(r => r.CreatedAt <= referenceDate).ToList();
                regularTeacherQuestionnaires = regularTeacherQuestionnaires.Where(r => r.CreatedAt <= referenceDate).ToList();
                regularTeacherCaseStudies = regularTeacherCaseStudies.Where(cs => cs.CreatedAt <= referenceDate).ToList();

                var schoolsCount = schools.Count();
                var studentsCount = students.Count();

                int studentsLastMonthCount;
                if (targetMonth.HasValue && targetYear.HasValue)
                {
                    studentsLastMonthCount = students.Count(s => s.CreatedAt.Year == targetYear.Value && s.CreatedAt.Month == targetMonth.Value);
                }
                else if (targetYear.HasValue)
                {
                    studentsLastMonthCount = students.Count(s => s.CreatedAt.Year == targetYear.Value);
                }
                else
                {
                    studentsLastMonthCount = students.Count(s => s.CreatedAt.Year == DateTime.Now.Year && s.CreatedAt.Month == DateTime.Now.Month);
                }

                var femaleStudents = students.Where(s => s.Sex == Sex.Female).Count();
                var maleStudents = students.Where(s => s.Sex == Sex.Male).Count();

                var distributionPerGenre = new Dictionary<string, int>()
                {
                    { "Feminino", femaleStudents } ,
                    { "Masculino", maleStudents }
                };

                // Carregar TODOS os PsychopedagogueQuestionnaires por aluno (mesma lógica do Relatório Geral AEE)
                var studentIds = students.Select(s => s.ID).ToHashSet();
                var allQuestionnaires = await _appDbContext.PsychopedagogueQuestionnaires
                    .Include(q => q.TypeOfDisability)
                    .Include(q => q.TypeOfDisorder)
                    .Where(q => q.StudentID != null && studentIds.Contains(q.StudentID.Value) && q.CreatedAt <= referenceDate)
                    .ToListAsync();

                var allSnapQuestionnaires = await _appDbContext.SnapIVQuestionnaires
                    .Where(q => q.StudentID != null && studentIds.Contains(q.StudentID.Value) && q.CreatedAt <= referenceDate)
                    .ToListAsync();

                var allAdaptiveScales = await _appDbContext.AdaptiveBehaviorScalesDI
                    .Include(s => s.PsychologistReport)
                    .Where(s => s.PsychologistReport != null && s.PsychologistReport.StudentID != null && studentIds.Contains(s.PsychologistReport.StudentID.Value) && s.CreatedAt <= referenceDate)
                    .ToListAsync();

                var allCognitiveAssessments = await _appDbContext.CognitiveAssessments
                    .Include(c => c.PsychopedagogueReport)
                    .Where(c => c.PsychopedagogueReport != null && c.PsychopedagogueReport.StudentID != null && studentIds.Contains(c.PsychopedagogueReport.StudentID.Value) && c.CreatedAt <= referenceDate)
                    .ToListAsync();

                var allCars = await _appDbContext.CARSQuestionnaires
                    .Include(c => c.PsychopedagogueReport)
                    .Where(c => c.PsychopedagogueReport != null && c.PsychopedagogueReport.StudentID != null && studentIds.Contains(c.PsychopedagogueReport.StudentID.Value) && c.CreatedAt <= referenceDate)
                    .ToListAsync();

                var allMchat = await _appDbContext.MCHATQuestionnaires
                    .Include(m => m.PsychopedagogueReport)
                    .Where(m => m.PsychopedagogueReport != null && m.PsychopedagogueReport.StudentID != null && studentIds.Contains(m.PsychopedagogueReport.StudentID.Value) && m.CreatedAt <= referenceDate)
                    .ToListAsync();

                var adaptiveByStudent = allAdaptiveScales
                    .Where(s => s.PsychologistReport?.StudentID != null)
                    .GroupBy(s => s.PsychologistReport!.StudentID!.Value)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CreatedAt).First());

                var cognitiveByStudent = allCognitiveAssessments
                    .Where(c => c.PsychopedagogueReport?.StudentID != null)
                    .GroupBy(c => c.PsychopedagogueReport!.StudentID!.Value)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CreatedAt).First());

                var carsByStudent = allCars
                    .Where(c => c.PsychopedagogueReport?.StudentID != null)
                    .GroupBy(c => c.PsychopedagogueReport!.StudentID!.Value)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CreatedAt).First());

                var mchatByStudent = allMchat
                    .Where(m => m.PsychopedagogueReport?.StudentID != null)
                    .GroupBy(m => m.PsychopedagogueReport!.StudentID!.Value)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CreatedAt).First());

                bool IsDIPositive(Guid studentId)
                {
                    if (!adaptiveByStudent.TryGetValue(studentId, out var adap) || adap == null) return false;
                    if (!cognitiveByStudent.TryGetValue(studentId, out var cog) || cog == null) return false;

                    bool adapPositive = adap.Score <= 11 || adap.Result == ScaleDIResult.SevereDevelopmentalDelay;
                    bool cogPositive = (cog.Percentage > 0 && cog.Percentage < 55) || cog.Result == CognitiveResult.ModerateToSevereIntellectualDisability;

                    return adapPositive && cogPositive;
                }

                bool IsTEAPositive(Guid studentId)
                {
                    if (carsByStudent.TryGetValue(studentId, out var cars) && cars != null)
                    {
                        if (cars.Result != CARSResult.NoAutism && cars.Result != CARSResult.None) return true;
                    }
                    if (mchatByStudent.TryGetValue(studentId, out var mchat) && mchat != null)
                    {
                        if (mchat.Result != MCHATResult.LowRisk && mchat.Result != MCHATResult.None) return true;
                    }
                    return false;
                }

                var snapQuestionnairesByStudent = allSnapQuestionnaires
                    .GroupBy(q => q.StudentID!.Value)
                    .ToDictionary(g => g.Key, g => g.ToList());

                var questionnairesByStudent = allQuestionnaires
                    .GroupBy(q => q.StudentID!.Value)
                    .ToDictionary(g => g.Key, g => g.ToList());

                var latestQuestionnaireByStudent = allQuestionnaires
                    .GroupBy(q => q.StudentID!.Value)
                    .ToDictionary(
                        g => g.Key,
                        g => g.OrderByDescending(q => q.CreatedAt).First()
                    );

                // Identificar alunos com deficiência (AEE) - Exigir validação dupla para Deficiência Intelectual ou CARS/MCHAT para TEA
                var studentsWithDisabilityIds = new HashSet<Guid>();
                foreach (var student in students)
                {
                    bool hasDisability = false;
                    if (questionnairesByStudent.TryGetValue(student.ID, out var qs))
                    {
                        hasDisability = qs.Any(q => q.TypeOfDisability?.Any(d => {
                            if (d.Disability == TypeOfDisability.None) return false;
                            if (d.Disability == TypeOfDisability.Intellectual || (int)d.Disability == 1)
                            {
                                return IsDIPositive(student.ID);
                            }
                            return true;
                        }) == true);
                    }

                    if (!hasDisability && IsDIPositive(student.ID))
                    {
                        hasDisability = true;
                    }

                    if (!hasDisability && IsTEAPositive(student.ID))
                    {
                        hasDisability = true;
                    }

                    if (hasDisability)
                        studentsWithDisabilityIds.Add(student.ID);
                }

                // Self-healing: atualizar RegistrationType baseado nos BaseDocuments (PsychopedagogueQuestionnaire)
                var studentsToUpdate = new List<Student>();
                foreach (var student in students)
                {
                    if (!latestQuestionnaireByStudent.ContainsKey(student.ID)) continue;

                    var targetType = studentsWithDisabilityIds.Contains(student.ID) ? "AEE" : "Geral";
                    if (student.RegistrationType != targetType)
                    {
                        student.RegistrationType = targetType;
                        student.UpdatedAt = DateTime.UtcNow;
                        studentsToUpdate.Add(student);
                    }
                }

                if (studentsToUpdate.Any())
                {
                    _appDbContext.Students.UpdateRange(studentsToUpdate);
                    await _appDbContext.SaveChangesAsync();
                }

                // Identificar alunos com transtornos
                var studentsWithDisorderOnlyIds = new HashSet<Guid>();
                var studentsWithDisorderAndDisabilityIds = new HashSet<Guid>();
                foreach (var student in students)
                {
                    bool hasDisorder = false;
                    if (questionnairesByStudent.TryGetValue(student.ID, out var qs))
                    {
                        hasDisorder = qs.Any(q => q.TypeOfDisorder?.Any(d => d.Disorder != TypeOfDisorder.None) == true);
                    }
                    if (!hasDisorder && snapQuestionnairesByStudent.TryGetValue(student.ID, out var sqs))
                    {
                        hasDisorder = sqs.Any(q => q.Result != SnapIVResult.None);
                    }

                    if (hasDisorder)
                    {
                        if (studentsWithDisabilityIds.Contains(student.ID))
                        {
                            studentsWithDisorderAndDisabilityIds.Add(student.ID);
                        }
                        else
                        {
                            studentsWithDisorderOnlyIds.Add(student.ID);
                        }
                    }
                }

                var typeOfRegistration = new Dictionary<string, int>()
                {
                    { "AEE", studentsWithDisabilityIds.Count },
                    { "Transtornos", studentsWithDisorderOnlyIds.Count },
                    { "TranstornosSemDeficiencia", studentsWithDisorderOnlyIds.Count },
                    { "TranstornosComDeficiencia", studentsWithDisorderAndDisabilityIds.Count },
                    { "Geral", students.Count - studentsWithDisabilityIds.Count - studentsWithDisorderOnlyIds.Count }
                };

                var studentsByAge = students.GroupBy(s => CalculateAge(s.DateOfBirth))
                                            .OrderBy(g => g.Key)
                                            .ToDictionary(
                                                g => g.Key.ToString(),
                                                g => g.Count()
                                            );

                for (int age = 4; age <= 17; age++)
                {
                    var key = age.ToString();
                    if (!studentsByAge.ContainsKey(key))
                    {
                        studentsByAge[key] = 0;
                    }
                }

                var numberOfStudentsPerSchool = students.Where(s => s.School != null)
                                                        .GroupBy(s => s.School!.Name)
                                                        .ToDictionary(
                                                            g => g.Key,
                                                            g => g.Count()
                                                        );

                var numberOfStudentsPerCity = students.Where(s => s.School != null)
                                                        .GroupBy(s => s.School!.City)
                                                        .ToDictionary(
                                                            g => g.Key,
                                                            g => g.Count()
                                                        );

                var numberOfConfiguredTotalStudentsPerCity = municipalityTargets
                    .GroupBy(m => $"{m.City} - {m.State}")
                    .ToDictionary(
                        g => g.Key,
                        g => g.OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt).First().TotalStudents
                    );

                var totalConfiguredStudentsAcrossCities = numberOfConfiguredTotalStudentsPerCity.Values.Sum();

                var selectedCityRegisteredStudents = 0;
                var selectedCityConfiguredStudents = 0;

                if (!string.IsNullOrWhiteSpace(filter.City))
                {
                    var selectedState = filter.State;

                    if (string.IsNullOrWhiteSpace(selectedState))
                    {
                        selectedState = schools.FirstOrDefault(s =>
                            s.City.Equals(filter.City, StringComparison.OrdinalIgnoreCase))?.State;
                    }

                    selectedCityRegisteredStudents = students.Count(s =>
                        s.School != null &&
                        s.School.City.Equals(filter.City, StringComparison.OrdinalIgnoreCase) &&
                        (string.IsNullOrWhiteSpace(selectedState) ||
                         s.School.State.Equals(selectedState, StringComparison.OrdinalIgnoreCase)));

                    if (!string.IsNullOrWhiteSpace(selectedState))
                    {
                        var selectedCityKey = $"{filter.City} - {selectedState}";
                        numberOfConfiguredTotalStudentsPerCity.TryGetValue(selectedCityKey, out selectedCityConfiguredStudents);
                    }
                }

                var selectedCityCoveragePercentage = selectedCityConfiguredStudents > 0
                    ? Math.Round((double)selectedCityRegisteredStudents / selectedCityConfiguredStudents * 100, 2)
                    : 0;


                var reportsCount = reports.Count();
                var reportsLast30DaysCount = reports.Count(r => r.CreatedAt >= referenceDate.AddDays(-30) && r.CreatedAt <= referenceDate);

                var paeesCount = paees.Count();
                var paeesCompletedLast30DaysCount = paees
                    .Where(p => p.DocumentStatus == Status.Complete || p.TeacherSign)
                    .Count(p =>
                    {
                        var pDate = p.SignedAt ?? p.UnifiedAt ?? p.UpdatedAt ?? p.CreatedAt;
                        return pDate >= referenceDate.AddDays(-30) && pDate <= referenceDate;
                    });

                var peisCompletedLast30DaysCount = peis
                    .Where(p => p.DocumentStatus == Status.Complete || p.TeacherSign)
                    .Count(p =>
                    {
                        var pDate = p.SignedAt ?? p.UnifiedAt ?? p.UpdatedAt ?? p.CreatedAt;
                        return pDate >= referenceDate.AddDays(-30) && pDate <= referenceDate;
                    });

                var caseStudyCount = regularTeacherCaseStudies.Count();
                var caseStudyCompletedCount = regularTeacherCaseStudies.Count(cs => cs.DocumentStatus == Status.Complete || cs.TeacherSign);
                var caseStudyInProgressCount = caseStudyCount - caseStudyCompletedCount;

                var caseStudyScreeningIds = regularTeacherCaseStudies.Select(cs => cs.RegularTeacherScreeningID).ToHashSet();
                var caseStudyQuestionnaireIds = regularTeacherCaseStudies.Select(cs => cs.RegularTeacherQuestionnaireID).ToHashSet();
                var screeningsInProgressCount = regularTeacherScreenings.Count(s => !caseStudyScreeningIds.Contains(s.ID));
                var questionnairesInProgressCount = regularTeacherQuestionnaires.Count(q => !caseStudyQuestionnaireIds.Contains(q.ID));
                var totalProcessesInProgress = screeningsInProgressCount + questionnairesInProgressCount + caseStudyInProgressCount;

                static bool IsSignedHistorical(bool signFlag, DateTime? signedAt, DateTime refDate) =>
                    signFlag && signedAt != null && signedAt.Value <= refDate;

                bool IsReportCompleted(Report r, DateTime refDate) =>
                    (r.DocumentStatus == Status.Complete && (r.UpdatedAt ?? r.CreatedAt) <= refDate) ||
                    (IsSignedHistorical(r.PsychologistSign, r.PsychologistSignedAt, refDate) &&
                     IsSignedHistorical(r.PsychopedagogueSign, r.PsychopedagogueSignedAt, refDate) &&
                     IsSignedHistorical(r.SocialWorkerSign, r.SocialWorkerSignedAt, refDate));

                bool IsReportUnified(Report r, DateTime refDate) =>
                    (r.ReportStatus == Status.Complete && (r.UpdatedAt ?? r.CreatedAt) <= refDate) ||
                    (r.UnifiedAt != null && r.UnifiedAt.Value <= refDate);

                var reportsCompletedCount = reports.Count(r => IsReportCompleted(r, referenceDate) || r.SignatureBlocked);
                var reportsPendingSignatureCount = reports.Count(r => IsReportUnified(r, referenceDate) && !IsReportCompleted(r, referenceDate) && !r.SignatureBlocked);
                var reportsInProgressCount = reportsCount - reportsCompletedCount - reportsPendingSignatureCount;

                static string? GetPsychologistUserId(Report r) =>
                    r.PsychologistReport?.PsychologistQuestionnaire?.UserID ?? r.PsychologistReport?.UserID;

                static string? GetPsychopedagogueUserId(Report r) =>
                    r.PsychopedagogueReport?.PsychopedagogueQuestionnaire?.UserID ?? r.PsychopedagogueReport?.UserID;

                var psychologistReports = reports.Where(r => r.PsychologistReport != null).ToList();
                if (isPsychologistUser)
                {
                    psychologistReports = psychologistReports.Where(r =>
                        string.Equals(GetPsychologistUserId(r), user.Id, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                var psychologistSignedCount = psychologistReports.Count(r =>
                    IsSignedHistorical(r.PsychologistSign, r.PsychologistSignedAt, referenceDate)
                );
                var psychologistFinishedCount = psychologistReports.Count(r =>
                    r.PsychologistReport!.UnifiedAt != null && r.PsychologistReport.UnifiedAt.Value <= referenceDate &&
                    !IsSignedHistorical(r.PsychologistSign, r.PsychologistSignedAt, referenceDate) &&
                    !r.SignatureBlocked
                );
                var psychologistInProgressCount = psychologistReports.Count(r =>
                    !IsReportUnified(r, referenceDate) && !IsReportCompleted(r, referenceDate) &&
                    (r.PsychologistReport!.UnifiedAt == null || r.PsychologistReport.UnifiedAt.Value > referenceDate) &&
                    !IsSignedHistorical(r.PsychologistSign, r.PsychologistSignedAt, referenceDate)
                );

                var psychopedagogueReports = reports.Where(r => r.PsychopedagogueReport != null).ToList();
                if (isPsychopedagogueUser)
                {
                    psychopedagogueReports = psychopedagogueReports.Where(r =>
                        string.Equals(GetPsychopedagogueUserId(r), user.Id, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                var psychopedagogueSignedCount = psychopedagogueReports.Count(r =>
                    IsSignedHistorical(r.PsychopedagogueSign, r.PsychopedagogueSignedAt, referenceDate)
                );
                var psychopedagogueFinishedCount = psychopedagogueReports.Count(r =>
                    r.PsychopedagogueReport!.UnifiedAt != null && r.PsychopedagogueReport.UnifiedAt.Value <= referenceDate &&
                    !IsSignedHistorical(r.PsychopedagogueSign, r.PsychopedagogueSignedAt, referenceDate) &&
                    !r.SignatureBlocked
                );
                var psychopedagogueInProgressCount = psychopedagogueReports.Count(r =>
                    !IsReportUnified(r, referenceDate) && !IsReportCompleted(r, referenceDate) &&
                    (r.PsychopedagogueReport!.UnifiedAt == null || r.PsychopedagogueReport.UnifiedAt.Value > referenceDate) &&
                    !IsSignedHistorical(r.PsychopedagogueSign, r.PsychopedagogueSignedAt, referenceDate)
                );

                var socialWorkerReports = reports;
                if (isSocialWorkUser)
                {
                    socialWorkerReports = socialWorkerReports.Where(r =>
                        r.SocialWorkerReport == null || string.Equals(r.SocialWorkerReport.UserID, user.Id, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                var socialWorkerSignedCount = socialWorkerReports.Count(r =>
                    IsSignedHistorical(r.SocialWorkerSign, r.SocialWorkerSignedAt, referenceDate)
                );
                var socialWorkerFinishedCount = socialWorkerReports.Count(r =>
                    r.SocialWorkerReportID != null && (r.SocialWorkerReport?.CreatedAt ?? r.SocialWorkerReport?.UpdatedAt ?? r.CreatedAt) <= referenceDate &&
                    !IsSignedHistorical(r.SocialWorkerSign, r.SocialWorkerSignedAt, referenceDate) &&
                    !r.SignatureBlocked
                );
                var socialWorkerInProgressCount = socialWorkerReports.Count(r =>
                    !IsReportUnified(r, referenceDate) && !IsReportCompleted(r, referenceDate) &&
                    (r.SocialWorkerReportID == null || (r.SocialWorkerReport?.CreatedAt ?? r.SocialWorkerReport?.UpdatedAt ?? r.CreatedAt) > referenceDate) &&
                    !IsSignedHistorical(r.SocialWorkerSign, r.SocialWorkerSignedAt, referenceDate)
                );

                var paeesCompletedCount = paees.Count(p => p.DocumentStatus == Status.Complete || p.TeacherSign);
                var peisCompletedCount = peis.Count(p => p.DocumentStatus == Status.Complete || p.TeacherSign);

                var typeOfDisability = Enum.GetValues(typeof(TypeOfDisability))
                    .Cast<TypeOfDisability>()
                    .ToDictionary(
                        type => type.GetDisplayName(),
                        type => 0
                    );

                // Distribuição por tipo de deficiência usando BaseDocuments (mesma fonte do Relatório Geral AEE)
                foreach (var student in students)
                {
                    List<TypeOfDisability>? disabilities = null;

                    if (latestQuestionnaireByStudent.TryGetValue(student.ID, out var latestQ))
                    {
                        disabilities = latestQ.TypeOfDisability?
                            .Where(d => d.Disability != TypeOfDisability.None)
                            .Where(d => {
                                if (d.Disability == TypeOfDisability.Intellectual || (int)d.Disability == 1)
                                {
                                    return IsDIPositive(student.ID);
                                }
                                return true;
                            })
                            .Select(d => d.Disability)
                            .ToList();
                    }

                    if (disabilities == null)
                    {
                        disabilities = new List<TypeOfDisability>();
                    }

                    if (!disabilities.Contains(TypeOfDisability.Intellectual) && IsDIPositive(student.ID))
                    {
                        disabilities.Add(TypeOfDisability.Intellectual);
                    }

                    if (!disabilities.Contains(TypeOfDisability.TEA) && IsTEAPositive(student.ID))
                    {
                        disabilities.Add(TypeOfDisability.TEA);
                    }

                    if (disabilities.Count == 0) continue;

                    if (disabilities.Count > 1)
                    {
                        var key = TypeOfDisability.Multiple.GetDisplayName();
                        if (string.IsNullOrWhiteSpace(key)) continue;

                        if (typeOfDisability.ContainsKey(key))
                            typeOfDisability[key]++;
                        else
                            typeOfDisability[key] = 1;
                    }
                    else
                    {
                        var key = disabilities.First().GetDisplayName();
                        if (string.IsNullOrWhiteSpace(key)) continue;

                        if (typeOfDisability.ContainsKey(key))
                            typeOfDisability[key]++;
                        else
                            typeOfDisability[key] = 1;
                    }
                }

                var numberOfReportsPerMonth = reports
                    .Where(r => r.CreatedAt.Year == currentYear)
                    .GroupBy(r => r.CreatedAt.Month)
                    .Select(g => new
                    {
                        Month = g.Key,
                        Count = g.Count()
                    })
                    .ToDictionary(
                        g => new DateTime(currentYear, g.Month, 1).ToString("MMMM", new CultureInfo("pt-BR")),
                        g => g.Count
                    );

                foreach (var month in fullMonths)
                {
                    if (!numberOfReportsPerMonth.ContainsKey(month))
                    {
                        numberOfReportsPerMonth[month] = 0;
                    }
                }

                var numberOfTotalRegistrationsPerMonth = students
                    .Where(r => r.CreatedAt.Year == currentYear)
                    .GroupBy(r => r.CreatedAt.Month)
                    .Select(g => new
                    {
                        Month = g.Key,
                        Count = g.Count()
                    }).ToDictionary(
                        g => new DateTime(currentYear, g.Month, 1).ToString("MMMM", new CultureInfo("pt-BR")),
                        g => g.Count
                    );

                foreach (var month in fullMonths)
                {
                    if (!numberOfTotalRegistrationsPerMonth.ContainsKey(month))
                    {
                        numberOfTotalRegistrationsPerMonth[month] = 0;
                    }
                }

                // Cadastros AEE por mês usando BaseDocuments (mesma fonte do Relatório Geral AEE)
                var numberOfAEERegistrationsPerMonth = students
                    .Where(s =>
                    {
                        return studentsWithDisabilityIds.Contains(s.ID) &&
                               s.CreatedAt.Year == currentYear;
                    })
                    .GroupBy(s => s.CreatedAt.Month)
                    .Select(g => new
                    {
                        Month = g.Key,
                        Count = g.Select(s => s.ID).Distinct().Count()
                    })
                    .ToDictionary(
                        g => new DateTime(currentYear, g.Month, 1).ToString("MMMM", new CultureInfo("pt-BR")),
                        g => g.Count
                    );


                foreach (var month in fullMonths)
                {
                    if (!numberOfAEERegistrationsPerMonth.ContainsKey(month))
                    {
                        numberOfAEERegistrationsPerMonth[month] = 0;
                    }
                }

                var roles = new[] { "SocialWork", "Psychologist", "Psychopedagogue" };
                var numberOfProfessionalsPerArea = new Dictionary<string, int>();

                foreach (var role in roles)
                {
                    string roleString = "";
                    var usersInRole = await _userManager.GetUsersInRoleAsync(role);

                    if (role == "SocialWork") roleString = "Assistente Social";
                    if (role == "Psychologist") roleString = "Psicólogo(a)";
                    if (role == "Psychopedagogue") roleString = "Psicopedagogo(a)";

                    var filteredUsers = usersInRole.Where(user =>
                        (string.IsNullOrEmpty(filter.Sex) || user.Sex.GetDisplayName() == filter.Sex) &&
                        (string.IsNullOrEmpty(filter.State) || Helpers.StateMatches(user.State, filter.State)) &&
                        (string.IsNullOrEmpty(filter.City) || Helpers.CityMatches(user.City, filter.City))
                    ).ToList();

                    numberOfProfessionalsPerArea[roleString] = filteredUsers.Count;
                }

                var reportsList = reports.OrderByDescending(r => r.CreatedAt).Take(5).ToList();
                var reportsListResponse = new List<ReportResponse>();

                foreach (var rep in reportsList)
                {
                    var responseRep = new ReportResponse()
                    {
                        ID = rep.ID,
                        Status = rep.ReportStatus.GetDisplayName() ?? "",
                        Student = StudentMapper.ToSummaryResponse(rep.Student!),
                        CreatedAt = rep.CreatedAt,
                        UpdatedAt = rep.UpdatedAt,
                    };
                    reportsListResponse.Add(responseRep);
                }

                var response = new DashboardSummaryResponse()
                {
                    TotalStudents = studentsCount,
                    TotalReports = reportsCount,
                    TotalPAEEs = paeesCount,
                    TotalSchools = schoolsCount,
                    TotalPEIsCompleted = peisCompletedCount,
                    TotalPAEEsCompleted = paeesCompletedCount,
                    TotalCaseStudies = caseStudyCount,
                    TotalCaseStudiesCompleted = caseStudyCompletedCount,
                    TotalProcessesInProgress = totalProcessesInProgress,
                    TotalBiopsychosocialReportsCompleted = reportsCompletedCount,
                    TotalBiopsychosocialReportsInProgress = reportsInProgressCount,
                    TotalBiopsychosocialReportsPendingSignature = reportsPendingSignatureCount,
                    PsychologistFinished = psychologistFinishedCount,
                    PsychologistInProgress = psychologistInProgressCount,
                    PsychologistSigned = psychologistSignedCount,
                    PsychopedagogueFinished = psychopedagogueFinishedCount,
                    PsychopedagogueInProgress = psychopedagogueInProgressCount,
                    PsychopedagogueSigned = psychopedagogueSignedCount,
                    SocialWorkerFinished = socialWorkerFinishedCount,
                    SocialWorkerInProgress = socialWorkerInProgressCount,
                    SocialWorkerSigned = socialWorkerSignedCount,
                    TotalStudentsLastMonth = studentsLastMonthCount,
                    TotalReportsLast30Days = reportsLast30DaysCount,
                    TotalPAEEsLast30Days = paeesCompletedLast30DaysCount,
                    TotalPEIsLast30Days = peisCompletedLast30DaysCount,
                    DistributionPerGenre = distributionPerGenre,
                    TypeOfRegistration = typeOfRegistration,
                    TypeOfDisability = typeOfDisability,
                    StudentsByAge = studentsByAge,
                    NumberOfStudentsPerSchool = numberOfStudentsPerSchool,
                    NumberOfStudentsPerCity = numberOfStudentsPerCity,
                    NumberOfConfiguredTotalStudentsPerCity = numberOfConfiguredTotalStudentsPerCity,
                    NumberOfTotalRegistrationsPerMonth = numberOfTotalRegistrationsPerMonth,
                    NumberOfAEERegistrationsPerMonth = numberOfAEERegistrationsPerMonth,
                    NumberOfReportsPerMonth = numberOfReportsPerMonth,
                    NumberOfProfessionalsPerArea = numberOfProfessionalsPerArea,
                    TotalConfiguredStudentsAcrossCities = totalConfiguredStudentsAcrossCities,
                    SelectedCityRegisteredStudents = selectedCityRegisteredStudents,
                    SelectedCityConfiguredStudents = selectedCityConfiguredStudents,
                    SelectedCityCoveragePercentage = selectedCityCoveragePercentage,
                    Reports = reportsListResponse
                };

                return Ok(new ApiResponse<DashboardSummaryResponse>()
                {
                    Success = true,
                    Message = "Estatísticas gerais retornadas com sucesso",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                return BadRequest(new ApiResponse<object>()
                {
                    Success = false,
                    Message = $"Erro ao retornar estatísticas gerais do sistema. {ex.Message}"
                });
            }
        }

        [HttpPost("municipality-total")]
        public async Task<IActionResult> UpsertMunicipalityTotal([FromBody] MunicipalityStudentTargetRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Dados inválidos para cadastro da quantidade de alunos por município."
                    });
                }

                var state = request.State?.Trim() ?? string.Empty;
                var city = request.City?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(state) || string.IsNullOrWhiteSpace(city))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Estado e município são obrigatórios."
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
                        Message = "Usuário não encontrado."
                    });
                }

                var userRoles = await _userManager.GetRolesAsync(user);
                var isAdmin = userRoles.Contains("Admin");

                if (!isAdmin)
                {
                    // Fallback to school location if user's city/state is not set but school is
                    if (user.SchoolID.HasValue && (string.IsNullOrWhiteSpace(user.City) || string.IsNullOrWhiteSpace(user.State)))
                    {
                        var userSchool = await _appDbContext.Schools.FindAsync(user.SchoolID.Value);
                        if (userSchool != null)
                        {
                            if (string.IsNullOrWhiteSpace(user.City)) user.City = userSchool.City;
                            if (string.IsNullOrWhiteSpace(user.State)) user.State = userSchool.State;
                        }
                    }

                    // Enforce that the user can only change their own municipality
                    if (!Helpers.CityMatches(city, user.City) || !Helpers.StateMatches(state, user.State))
                    {
                        return StatusCode(403, new ApiResponse<object>
                        {
                            Success = false,
                            Message = "Você não tem permissão para alterar a quantidade de alunos de outro município."
                        });
                    }
                }

                var existingTarget = await _appDbContext.MunicipalityStudentTargets
                    .FirstOrDefaultAsync(m =>
                        m.State == state &&
                        m.City == city);

                if (existingTarget == null)
                {
                    await _appDbContext.MunicipalityStudentTargets.AddAsync(new MunicipalityStudentTarget
                    {
                        State = state,
                        City = city,
                        TotalStudents = request.TotalStudents,
                        CreatedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    existingTarget.TotalStudents = request.TotalStudents;
                    existingTarget.UpdatedAt = DateTime.UtcNow;
                }

                await _appDbContext.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Quantidade total de alunos por município salva com sucesso."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao salvar quantidade total de alunos por município");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Erro ao salvar quantidade total de alunos por município. {ex.Message}"
                });
            }
        }

        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatisticsSummary([FromQuery] StatisticFilter filter)
        {
            var currentYear = DateTime.Now.Year;
            var fullMonths = Enumerable.Range(1, 12)
                       .Select(m => new DateTime(1, m, 1).ToString("MMMM", new CultureInfo("pt-BR")))
                       .ToList();

            var today = DateTime.Today;
            var firstDayOfThisMonth = new DateTime(today.Year, today.Month, 1);
            var lastDayOfLastMonth = firstDayOfThisMonth.AddDays(-1);
            var firstDayOfLastMonth = new DateTime(lastDayOfLastMonth.Year, lastDayOfLastMonth.Month, 1);

            var studentsQuery = _appDbContext.Students.Include(s => s.School).AsQueryable();

            if (_tenantContext.IsPrivatePlan)
            {
                studentsQuery = studentsQuery.Where(s => s.TenantId == _tenantContext.TenantId);
            }

            if (!string.IsNullOrWhiteSpace(filter.State))
            {
                studentsQuery = studentsQuery.Where(s => s.School != null && s.School.State == filter.State);
            }

            if (!string.IsNullOrWhiteSpace(filter.Sex))
            {
                studentsQuery = studentsQuery.Where(s => s.Sex == Helpers.GetEnumValueFromDisplayName<Sex>(filter.Sex));
            }

            if (!string.IsNullOrWhiteSpace(filter.City))
                studentsQuery = studentsQuery.Where(s => s.School != null && s.School.City == filter.City);

            if (filter.SchoolID.HasValue)
                studentsQuery = studentsQuery.Where(s => s.SchoolID == filter.SchoolID);

            if (!string.IsNullOrWhiteSpace(filter.RegistrationType) && filter.RegistrationType != "Todos")
                studentsQuery = studentsQuery.Where(s => s.RegistrationType == filter.RegistrationType);

            if (!string.IsNullOrWhiteSpace(filter.Disability))
            {
                var disabilityEnum =
                    Helpers.GetEnumValueFromDisplayName<TypeOfDisability>(filter.Disability);

                studentsQuery = studentsQuery.Where(s =>
                    s.BaseDocuments.OfType<Report>().Any(r =>
                        r.PsychopedagogueReport != null &&
                        r.PsychopedagogueReport.PsychopedagogueQuestionnaire.TypeOfDisability
                            .Any(d => d.Disability == disabilityEnum)
                    )
                );
            }

            if (!string.IsNullOrWhiteSpace(filter.Year))
            {
                if (int.TryParse(filter.Year, out int year))
                {
                    studentsQuery = studentsQuery.Where(s => s.CreatedAt.Year == year);
                }
            }

            if (!string.IsNullOrEmpty(filter.AgeGroup) && filter.AgeGroup != "todas")
            {
                DateTime minDate = DateTime.MinValue;
                DateTime maxDate = DateTime.MaxValue;

                switch (filter.AgeGroup)
                {
                    case "0-5":
                        minDate = today.AddYears(-5);
                        maxDate = today;
                        break;
                    case "6-10":
                        minDate = today.AddYears(-10);
                        maxDate = today.AddYears(-6);
                        break;
                    case "11-14":
                        minDate = today.AddYears(-14);
                        maxDate = today.AddYears(-11);
                        break;
                    case "15-18":
                        minDate = today.AddYears(-18);
                        maxDate = today.AddYears(-15);
                        break;
                }

                studentsQuery = studentsQuery.Where(s => s.DateOfBirth >= minDate && s.DateOfBirth <= maxDate);
            }

            var students = await studentsQuery.ToListAsync();
            var totalStudents = students.Count;

            var studentsLastMonthCount = students.Count(s => s.CreatedAt >= firstDayOfLastMonth && s.CreatedAt <= lastDayOfLastMonth);

            var totalCities = await _appDbContext.Schools.Select(s => s.City).Distinct().CountAsync();
            var totalCitiesThisMonth = await _appDbContext.Schools
                .Where(s => s.CreatedAt >= firstDayOfThisMonth && s.CreatedAt <= today)
                .Select(s => s.City).Distinct().CountAsync();

            var filteredStudentIds = students.Select(s => s.ID).ToList();

            var filteredReportsQuery = _appDbContext.Reports
                .AsSplitQuery()
                .Include(p => p.PsychopedagogueReport)
                    .ThenInclude(pr => pr!.PsychopedagogueQuestionnaire)
                    .ThenInclude(pq => pq!.TypeOfDisability)
                .Include(p => p.PsychopedagogueReport)
                    .ThenInclude(pr => pr!.PsychopedagogueQuestionnaire)
                    .ThenInclude(pq => pq!.TypeOfDisorder)
                .Where(r => r.StudentID.HasValue &&
                filteredStudentIds.Contains(r.StudentID.Value));

            var totalReports = filteredReportsQuery.Count();

            var reportsLastMonthCount = filteredReportsQuery.Count(r => r.CreatedAt >= firstDayOfLastMonth && r.CreatedAt <= lastDayOfLastMonth);

            var reportsCompletedCount = filteredReportsQuery.Count(r =>
                r.DocumentStatus == Status.Complete ||
                (r.PsychologistSign && r.PsychopedagogueSign && r.SocialWorkerSign)
            );
            var reportsCompletedLastMonthCount = filteredReportsQuery.Count(r =>
                (r.DocumentStatus == Status.Complete ||
                 (r.PsychologistSign && r.PsychopedagogueSign && r.SocialWorkerSign)) &&
                r.CreatedAt >= firstDayOfLastMonth &&
                r.CreatedAt <= lastDayOfLastMonth
            );

            float reportsPercentage = totalReports == 0 ? 0f : (float)reportsCompletedCount / totalReports;
            float lastMonthReportsPercentage = reportsLastMonthCount == 0 ? 0f : (float)reportsCompletedLastMonthCount / reportsLastMonthCount;

            double percentualVariacaoStudents = studentsLastMonthCount > 0
                ? ((double)(totalStudents - studentsLastMonthCount) / studentsLastMonthCount) * 100
                : (totalStudents > 0 ? 100 : 0);

            double percentualVariacaoReports = reportsLastMonthCount > 0
                ? ((double)(totalReports - reportsLastMonthCount) / reportsLastMonthCount) * 100
                : (totalReports > 0 ? 100 : 0);

            double percentualVariacaoReportsCompletion = lastMonthReportsPercentage > 0
                ? ((reportsPercentage - lastMonthReportsPercentage) / lastMonthReportsPercentage) * 100
                : (reportsPercentage > 0 ? 100 : 0);

            var typeOfDisability = Enum.GetValues(typeof(TypeOfDisability))
                    .Cast<TypeOfDisability>()
                    .ToDictionary(
                        type => type.GetDisplayName(),
                        type => 0
                    );

            var latestReportsByStudent = filteredReportsQuery
                .Where(r => r.PsychopedagogueReport != null)
                .GroupBy(r => r.StudentID)
                .Select(group => group.OrderByDescending(r => r.CreatedAt).First())
                .ToList();

            foreach (var report in latestReportsByStudent)
            {
                var disabilities =
                    report.PsychopedagogueReport?.PsychopedagogueQuestionnaire?.TypeOfDisability?
                    .Where(d => d.Disability != TypeOfDisability.None)
                    .ToList();

                if (disabilities == null || disabilities.Count == 0) continue;

                if (disabilities.Count > 1)
                {
                    var key = TypeOfDisability.Multiple.GetDisplayName();
                    if (string.IsNullOrWhiteSpace(key)) continue;

                    if (typeOfDisability.ContainsKey(key))
                        typeOfDisability[key]++;
                    else
                        typeOfDisability[key] = 1;
                }
                else
                {
                    var key = disabilities.First().Disability.GetDisplayName();
                    if (string.IsNullOrWhiteSpace(key)) continue;

                    if (typeOfDisability.ContainsKey(key))
                        typeOfDisability[key]++;
                    else
                        typeOfDisability[key] = 1;
                }
            }

            var numberOfReportsPerMonth = filteredReportsQuery
                    .Where(pr => pr.CreatedAt.Year == currentYear)
                    .GroupBy(pr => pr.CreatedAt.Month)
                    .Select(g => new
                    {
                        Month = g.Key,
                        Count = g.Select(pr => pr.ID).Distinct().Count()
                    })
                    .ToDictionary(
                        g => new DateTime(currentYear, g.Month, 1).ToString("MMMM", new CultureInfo("pt-BR")),
                        g => g.Count
                    );

            foreach (var month in fullMonths)
            {
                if (!numberOfReportsPerMonth.ContainsKey(month))
                {
                    numberOfReportsPerMonth[month] = 0;
                }
            }

            var numberOfStudentsPerMonth = students
                    .Where(pr => pr.CreatedAt.Year == currentYear)
                    .GroupBy(pr => pr.CreatedAt.Month)
                    .Select(g => new
                    {
                        Month = g.Key,
                        Count = g.Select(pr => pr.ID).Distinct().Count()
                    })
                    .ToDictionary(
                        g => new DateTime(currentYear, g.Month, 1).ToString("MMMM", new CultureInfo("pt-BR")),
                        g => g.Count
                    );


            foreach (var month in fullMonths)
            {
                if (!numberOfStudentsPerMonth.ContainsKey(month))
                {
                    numberOfStudentsPerMonth[month] = 0;
                }
            }

            var allCities = students
                .Select(s => new
                {
                    City = (s.School != null ? s.School.City : s.City)?.Trim(),
                    State = (s.School != null ? s.School.State : s.State)?.Trim()
                })
                .Where(s => !string.IsNullOrWhiteSpace(s.City))
                .Distinct()
                .ToList();

            string KeyForCity(string? city, string? state)
            {
                var c = city?.Trim() ?? "";
                var st = state?.Trim() ?? "";
                return string.IsNullOrWhiteSpace(st) ? c : $"{c} - {st}";
            }

            var numberOfTotalRegistrationsPerCities = allCities.ToDictionary(
                c => KeyForCity(c.City, c.State),
                c => students.Count(s =>
                    (s.School != null ? s.School.City : s.City) == c.City &&
                    (s.School != null ? s.School.State : s.State) == c.State
                )
            );

            var numberOfAEERegistrationsPerCities = allCities.ToDictionary(
                c => KeyForCity(c.City, c.State),
                c => students.Count(s =>
                    (s.School != null ? s.School.City : s.City) == c.City &&
                    (s.School != null ? s.School.State : s.State) == c.State &&
                    s.RegistrationType == "AEE"
                )
            );

            var response = new StatisticsResponse()
            {
                TotalStudents = totalStudents,
                TotalStudentsComparedToLastMonth = percentualVariacaoStudents,
                TotalReports = totalReports,
                TotalReportsComparedToLastMonth = percentualVariacaoReports,
                TotalCities = totalCities,
                TotalCitiesThisMonth = totalCitiesThisMonth,
                ReportCompletionPercentage = reportsPercentage,
                ReportCompletionPercentageComparedToLastMonth = percentualVariacaoReportsCompletion,
                DistributionPerTypeOfDisability = typeOfDisability,
                NumberOfReportsPerMonth = numberOfReportsPerMonth,
                NumberOfStudentsPerMonth = numberOfStudentsPerMonth,
                NumberOfTotalRegistrationsPerCities = numberOfTotalRegistrationsPerCities,
                NumberOfAEERegistrationsPerCities = numberOfAEERegistrationsPerCities
            };

            return Ok(new ApiResponse<StatisticsResponse>()
            {
                Success = true,
                Message = "Estatísticas retornadas com sucesso",
                Data = response
            });
        }
    }
}
