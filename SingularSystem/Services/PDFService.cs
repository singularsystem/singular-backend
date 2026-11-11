using SingularSystem.DTOs.ReportAEE;
using SingularSystem.DTOs.Students;
using SingularSystem.Extensions;
using SingularSystem.Interfaces;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using SingularSystem.Services;

public class PDFService : IPDFService
{
    private readonly AppDbContext _appDbContext;
    private readonly IEnumerable<IDocumentPdfGenerator> _generators;
    private readonly IStudentPDFGenerator _studentPDFGenerator;
    private readonly IReportAEEPDFGenerator _reportAEEPDFGenerator;
    private readonly IIssuerResolver _issuerResolver;
    private readonly IDocumentValidationService _documentValidationService;
    private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;
    private static readonly Lazy<byte[]> _singularLogo = new(LoadDefaultSingularLogo);

    public PDFService(
        AppDbContext appDbContext,
        IEnumerable<IDocumentPdfGenerator> generators,
        IStudentPDFGenerator studentPDFGenerator,
        IReportAEEPDFGenerator reportAEEPDFGenerator,
        IIssuerResolver issuerResolver,
        IDocumentValidationService documentValidationService,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        _appDbContext = appDbContext;
        _generators = generators;
        _studentPDFGenerator = studentPDFGenerator;
        _reportAEEPDFGenerator = reportAEEPDFGenerator;
        _issuerResolver = issuerResolver;
        _documentValidationService = documentValidationService;
        _configuration = configuration;
    }

    private static byte[] LoadDefaultSingularLogo()
    {
        var basePath = AppContext.BaseDirectory;
        var path = System.IO.Path.Combine(basePath, "Resources", "Brand", "logomarca-laranja.png");
        if (System.IO.File.Exists(path))
        {
            return System.IO.File.ReadAllBytes(path);
        }

        var fallback = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Resources", "Brand", "logomarca-laranja.png");
        if (System.IO.File.Exists(fallback))
        {
            return System.IO.File.ReadAllBytes(fallback);
        }

        return Array.Empty<byte>();
    }

    private static List<string> SplitCsv(string csv)
    {
        return (csv ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public Task<byte[]> GeneratePdf(Guid documentId) => GeneratePdf(documentId, PdfProfile.View);

    public async Task<byte[]> GeneratePdf(Guid documentId, PdfProfile profile)
    {
        var docType = await GetDocumentType(documentId);
        if (docType is null) throw new KeyNotFoundException("Documento não encontrado.");

        BaseDocument? document = docType switch
        {
            "PEI" => await _appDbContext.Set<PEI>()
                .AsNoTracking()
                .Include(x => x.User)
                .Include(x => x.Student).ThenInclude(x => x!.School)
                .Include(x => x.Subjects).ThenInclude(r => r.Responses)
                .FirstOrDefaultAsync(x => x.ID == documentId),

            "PAEE" => await _appDbContext.Set<PAEE>()
                .AsNoTracking()
                .Include(x => x.User)
                .Include(x => x.Student).ThenInclude(x => x!.School)
                .Include(x => x.Portage).ThenInclude(x => x.Responses)
                .Include(s => s.OrganizationOfCare)
                .Include(s => s.OtherProfessionals)
                .Include(s => s.DifferentiatedActivitiesInAEE)
                .Include(s => s.WorkMethodology)
                .Include(s => s.MaterialResourcesAndEquipment)
                .Include(s => s.CriteriaInEvaluation)
                .Include(s => s.ResourcesAndStrategies)
                .FirstOrDefaultAsync(x => x.ID == documentId),

            "Report" => await _appDbContext.Set<Report>()
                .AsNoTracking()
                .Include(x => x.User)
                .Include(x => x.Student).ThenInclude(x => x!.School)
                .Include(x => x.PsychologistReport).ThenInclude(x => x!.User)
                .Include(x => x.PsychologistReport).ThenInclude(x => x!.SnapIVQuestionnaire)
                .Include(x => x.PsychologistReport).ThenInclude(x => x!.AdaptiveBehaviorScaleDI)
                .Include(x => x.PsychologistReport).ThenInclude(x => x!.PsychologistQuestionnaire)
                .Include(x => x.PsychopedagogueReport).ThenInclude(x => x!.User)
                .Include(x => x.PsychopedagogueReport).ThenInclude(x => x!.PsychopedagogueQuestionnaire).ThenInclude(x => x.TypeOfDisability)
                .Include(x => x.PsychopedagogueReport).ThenInclude(x => x!.PsychopedagogueQuestionnaire).ThenInclude(x => x.TypeOfDisorder)
                .Include(x => x.PsychopedagogueReport).ThenInclude(x => x!.MCHATQuestionnaire)
                .Include(x => x.PsychopedagogueReport).ThenInclude(x => x!.CARSQuestionnaire)
                .Include(x => x.PsychopedagogueReport).ThenInclude(x => x!.CognitiveAssessment)
                .Include(x => x.SocialWorkerReport).ThenInclude(x => x!.User)
                .Include(x => x.SocialWorkerReport).ThenInclude(x => x!.ProceduresUsed)
                .Include(x => x.SocialWorkerReport).ThenInclude(x => x!.FamilyIncomeSource)
                .Include(x => x.SocialWorkerReport).ThenInclude(x => x!.ReceivedGovernmentBenefits)
                .Include(x => x.SocialWorkerReport).ThenInclude(x => x!.HealthConditions)
                .Include(x => x.SocialWorkerReport).ThenInclude(x => x!.MainSchoolDifficulties)
                .Include(x => x.SocialWorkerReport).ThenInclude(x => x!.Recommendations)
                .FirstOrDefaultAsync(x => x.ID == documentId),

            "RegularTeacherEvaluationDocument" => await _appDbContext.Set<RegularTeacherEvaluationDocument>()
                .AsNoTracking()
                .Include(x => x.User)
                .Include(x => x.Student).ThenInclude(x => x!.School)
                .Include(x => x.RegularTeacherQuestionnaire).ThenInclude(x => x.BehaviorTraits)
                .Include(x => x.RegularTeacherScreening)
                .FirstOrDefaultAsync(x => x.ID == documentId),

            _ => throw new NotSupportedException($"Tipo de documento '{docType}' não suportado.")
        };

        if (document is null) throw new KeyNotFoundException("Documento não encontrado.");

        var generator = _generators.FirstOrDefault(g => g.Discriminator == docType);
        if (generator is null) throw new NotSupportedException($"Sem generator para '{docType}'.");

        var issuer = await _issuerResolver.ResolveAsync(document, profile);

        string? validationUrl = null;
        if (string.Equals(docType, "Report", StringComparison.OrdinalIgnoreCase))
        {
            var baseUrl = _configuration["PublicValidation:BaseUrl"]?.TrimEnd('/');
            if (!string.IsNullOrWhiteSpace(baseUrl))
            {
                var token = await _documentValidationService.GetOrIssueAsync(documentId, docType);
                validationUrl = $"{baseUrl}/validar#{token}";
            }
        }

        var renderContext = new PdfRenderContext(
            Profile: profile,
            Issuer: issuer,
            SingularLogo: _singularLogo.Value,
            ValidationUrl: validationUrl,
            GeneratedAtBrt: DateTime.UtcNow.ToBrazilianTime());

        return await generator.GenerateAsync(document, renderContext);
    }

    public async Task<string?> GetDocumentType(Guid documentId)
    {
        var entity = await _appDbContext.BaseDocuments
        .AsNoTracking()
        .FirstOrDefaultAsync(d => d.ID == documentId);

        return entity?.GetType().Name;
    }

    public async Task<byte[]> GenerateStudentsPdf(string state, string city, string schoolID, string serie = "")
    {
        var query = _appDbContext.Students
        .AsNoTracking()
        .Include(s => s.School).Where(s => s.IsActive);

        if (!string.IsNullOrWhiteSpace(state))
        {
            var cleanState = state.Trim();
            query = query.Where(s => s.State == cleanState || (s.School != null && s.School.State == cleanState));
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            var cleanCity = city.Trim();
            query = query.Where(s => s.City == cleanCity || (s.School != null && s.School.City == cleanCity));
        }

        if (!string.IsNullOrWhiteSpace(schoolID))
            query = query.Where(s => s.School != null && s.School.ID == Guid.Parse(schoolID));

        if (!string.IsNullOrWhiteSpace(serie))
        {
            var serieEnum = Helpers.GetEnumValueFromDisplayName<Serie>(serie);
            if (serieEnum != Serie.None)
            {
                query = query.Where(s => s.Serie == serieEnum);
            }
        }

        var rows = await query
            .OrderBy(s => s.FullName)
            .Select(s => new StudentsPDFResponse
            {
                FullName = s.FullName,
                DateOfBirth = s.DateOfBirth,
                SchoolName = s.School != null ? s.School.Name : "",
                Serie = s.Serie.GetDisplayName(),
                City = s.School != null ? s.School.City : "",
                State = s.School != null ? s.School.State : ""
            })
            .ToListAsync();

        return await _studentPDFGenerator.GenerateStudentsPDFAsync(rows);
    }    public async Task<byte[]> GenerateReportsAEEPdf(string state, string city, string schoolID, string disabilities, string disorders)
    {
        var disabilityList = SplitCsv(disabilities);
        var disorderList = SplitCsv(disorders);

        List<TypeOfDisability> disabilityEnums = new();
        if (disabilityList.Count > 0)
        {
            foreach (var item in disabilityList)
            {
                try
                {
                    disabilityEnums.Add(
                        Helpers.GetEnumValueFromDisplayName<TypeOfDisability>(item)
                    );
                }
                catch
                {
                    // Ignore invalid enums
                }
            }
        }

        List<TypeOfDisorder> disorderEnums = new();
        List<SnapIVResult> snapIvEnums = new();
        if (disorderList.Count > 0)
        {
            foreach (var item in disorderList)
            {
                try
                {
                    if (item.StartsWith("TDAH Tipo ") || item == "TDAH Combinado")
                    {
                        snapIvEnums.Add(Helpers.GetEnumValueFromDisplayName<SnapIVResult>(item));
                    }
                    else
                    {
                        disorderEnums.Add(Helpers.GetEnumValueFromDisplayName<TypeOfDisorder>(item));
                    }
                }
                catch
                {
                    // Ignore invalid enums
                }
            }
        }

        var query = _appDbContext.Students.Include(s => s.School)
                                           .AsNoTracking()
                                           .AsSplitQuery()
                                           .Where(s => s.IsActive);

        if (!string.IsNullOrWhiteSpace(state))
        {
            var cleanState = state.Trim();
            query = query.Where(s => s.State == cleanState || (s.School != null && s.School.State == cleanState));
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            var cleanCity = city.Trim();
            query = query.Where(s => s.City == cleanCity || (s.School != null && s.School.City == cleanCity));
        }

        if (!string.IsNullOrWhiteSpace(schoolID))
            query = query.Where(s => s.School != null && s.School.ID == Guid.Parse(schoolID));

        if (disabilityEnums.Count > 0 || disorderEnums.Count > 0 || snapIvEnums.Count > 0)
        {
            query = query.Where(s =>
                (disabilityEnums.Count > 0 && s.BaseDocuments.OfType<PsychopedagogueQuestionnaire>()
                    .Any(q => q.TypeOfDisability.Any(td => disabilityEnums.Contains(td.Disability)))) ||
                (disorderEnums.Count > 0 && s.BaseDocuments.OfType<PsychopedagogueQuestionnaire>()
                    .Any(q => q.TypeOfDisorder.Any(td => disorderEnums.Contains(td.Disorder)))) ||
                (snapIvEnums.Count > 0 && s.BaseDocuments.OfType<SnapIVQuestionnaire>()
                    .Any(q => snapIvEnums.Contains(q.Result)))
            );
        }
        else
        {
            // Por padrão, se nenhum filtro for passado, mostrar apenas alunos que tenham ALGUMA deficiência ou transtorno (AEE)
            query = query.Where(s =>
                s.BaseDocuments.OfType<PsychopedagogueQuestionnaire>()
                    .Any(q => q.TypeOfDisability.Any(td => td.Disability != TypeOfDisability.None) || 
                              q.TypeOfDisorder.Any(td => td.Disorder != TypeOfDisorder.None)) ||
                s.BaseDocuments.OfType<SnapIVQuestionnaire>()
                    .Any(q => q.Result != SnapIVResult.None)
            );
        }

        var pageStudents = await query
            .OrderBy(s => s.FullName)
            .Select(s => new
            {
                Student = s,
                LastQuestionnaireId = s.BaseDocuments
                    .OfType<PsychopedagogueQuestionnaire>()
                    .OrderByDescending(q => q.CreatedAt)
                    .Select(q => (Guid?)q.ID)
                    .FirstOrDefault(),
                LastSnapIVId = s.BaseDocuments
                    .OfType<SnapIVQuestionnaire>()
                    .OrderByDescending(q => q.CreatedAt)
                    .Select(q => (Guid?)q.ID)
                    .FirstOrDefault()
            })
            .ToListAsync();

        var ids = pageStudents
            .Where(x => x.LastQuestionnaireId.HasValue)
            .Select(x => x.LastQuestionnaireId!.Value)
            .ToList();
        
        var snapIds = pageStudents
            .Where(x => x.LastSnapIVId.HasValue)
            .Select(x => x.LastSnapIVId!.Value)
            .ToList();

        var questionnaires = await _appDbContext.PsychopedagogueQuestionnaires
            .IgnoreQueryFilters()
            .Where(q => ids.Contains(q.ID))
            .Include(q => q.TypeOfDisability)
            .Include(q => q.TypeOfDisorder)
            .AsNoTracking()
            .ToListAsync();

        var snapQuestionnaires = await _appDbContext.SnapIVQuestionnaires
            .IgnoreQueryFilters()
            .Where(q => snapIds.Contains(q.ID))
            .AsNoTracking()
            .ToListAsync();

        var qById = questionnaires.ToDictionary(q => q.ID);
        var snapQById = snapQuestionnaires.ToDictionary(q => q.ID);

        var rows = pageStudents.Select(x =>
        {
            PsychopedagogueQuestionnaire? lastQ = null;
            if (x.LastQuestionnaireId.HasValue)
                qById.TryGetValue(x.LastQuestionnaireId.Value, out lastQ);

            SnapIVQuestionnaire? lastSnapQ = null;
            if (x.LastSnapIVId.HasValue)
                snapQById.TryGetValue(x.LastSnapIVId.Value, out lastSnapQ);

            var disabilities = lastQ?.TypeOfDisability?
                .Select(td => td.Disability.GetDisplayName())
                .Distinct()
                .ToList() ?? new List<string>();

            var disorders = lastQ?.TypeOfDisorder?
                .Select(td => td.Disorder.GetDisplayName())
                .Distinct()
                .ToList() ?? new List<string>();

            if (lastSnapQ != null && lastSnapQ.Result != SnapIVResult.None)
            {
                disorders.Add(lastSnapQ.Result.GetDisplayName());
            }

            return new ReportAEEPDFResponse
            {
                FullName = x.Student.FullName,
                DateOfBirth = x.Student.DateOfBirth,
                SchoolName = x.Student.School != null ? x.Student.School.Name : "",
                Serie = x.Student.Serie.GetDisplayName(),
                City = x.Student.School != null ? x.Student.School.City : "",
                State = x.Student.School != null ? x.Student.School.State : "",
                Disabilities = disabilities,
                Disorders = disorders
            };
        }).ToList();

        return await _reportAEEPDFGenerator.GenerateReportsAEEPDFAsync(rows);
    }

    public async Task<byte[]> GenerateCaseStudiesTrackingPdf(string state, string city, string schoolID, string searchTerm, string status, int? year, int? month)
    {
        var query = _appDbContext.Students.Include(s => s.School)
                                          .AsNoTracking()
                                          .AsSplitQuery()
                                          .Where(s => s.IsActive);

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

        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("Todos", StringComparison.OrdinalIgnoreCase))
        {
            if (status.Equals("Completo", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(student => _appDbContext.RegularTeacherEvaluationDocuments.Any(d => d.StudentID == student.ID && d.TeacherSign));
            }
            else if (status.Equals("Incompleto", StringComparison.OrdinalIgnoreCase) || status.Equals("Em andamento", StringComparison.OrdinalIgnoreCase) || status.Equals("EmAndamento", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(student => 
                    _appDbContext.RegularTeacherEvaluationDocuments.Any(d => d.StudentID == student.ID && !d.TeacherSign) ||
                    (!_appDbContext.RegularTeacherEvaluationDocuments.Any(d => d.StudentID == student.ID) && 
                     (_appDbContext.RegularTeacherScreenings.Any(s => s.StudentID == student.ID) || 
                      _appDbContext.RegularTeacherQuestionnaires.Any(q => q.StudentID == student.ID)))
                );
            }
            else if (status.Equals("Não Iniciado", StringComparison.OrdinalIgnoreCase) || status.Equals("Não iniciado", StringComparison.OrdinalIgnoreCase))
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

        var pagedStudents = await query
            .OrderBy(s => s.FullName)
            .ToListAsync();

        var studentIds = pagedStudents.Select(s => s.ID).ToList();

        var documents = await _appDbContext.RegularTeacherEvaluationDocuments.IgnoreQueryFilters()
            .Include(d => d.Student)
            .Where(d => d.StudentID.HasValue && studentIds.Contains(d.StudentID.Value))
            .ToListAsync();

        var screenings = await _appDbContext.RegularTeacherScreenings
            .Where(s => s.StudentID.HasValue && studentIds.Contains(s.StudentID.Value))
            .ToListAsync();

        var questionnaires = await _appDbContext.RegularTeacherQuestionnaires
            .Where(q => q.StudentID.HasValue && studentIds.Contains(q.StudentID.Value))
            .ToListAsync();

        var rows = new List<SingularSystem.DTOs.Tracking.CaseStudiesTrackingPDFRow>();
        foreach (var student in pagedStudents)
        {
            var doc = documents.FirstOrDefault(d => d.StudentID == student.ID);
            var hasDoc = doc != null;
            var isDocSigned = doc != null && doc.TeacherSign;
            var hasScreening = screenings.Any(s => s.StudentID == student.ID);
            var hasQuestionnaire = questionnaires.Any(q => q.StudentID == student.ID);

            string calculatedStatus;
            if (hasDoc && isDocSigned)
                calculatedStatus = "Completo";
            else if (hasDoc || hasScreening || hasQuestionnaire)
                calculatedStatus = "Em andamento";
            else
                calculatedStatus = "Não Iniciado";

            var lastUpdateDate = doc != null && doc.UpdatedAt.HasValue ? doc.UpdatedAt.Value : (doc != null ? doc.CreatedAt : student.CreatedAt);
            
            rows.Add(new SingularSystem.DTOs.Tracking.CaseStudiesTrackingPDFRow
            {
                StudentName = student.FullName,
                SchoolName = student.School != null ? student.School.Name : "-",
                Status = calculatedStatus,
                LastUpdate = lastUpdateDate.ToBrazilianTime().ToString("dd/MM/yyyy HH:mm", new CultureInfo("pt-BR"))
            });
        }

        return CaseStudiesTrackingPDFGenerator.Generate(rows);
    }

    public async Task<byte[]> GenerateBiopsychosocialTrackingPdf(string state, string city, string schoolID, string searchTerm, string status, string pendingProfessional, int? year, int? month)
    {
        var query = _appDbContext.Students.Include(s => s.School)
                                          .AsNoTracking()
                                          .AsSplitQuery()
                                          .Where(s => s.IsActive);

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

        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("Todos", StringComparison.OrdinalIgnoreCase))
        {
            if (status.Equals("Completo", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(student => _appDbContext.Reports.Any(r => r.StudentID == student.ID && (r.ReportStatus == Status.Complete || r.UnifiedAt != null)));
            }
            else if (status.Equals("Incompleto", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(pendingProfessional) && !pendingProfessional.Equals("Todos", StringComparison.OrdinalIgnoreCase))
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
                else
                {
                    query = query.Where(student => !_appDbContext.Reports.Any(r => r.StudentID == student.ID && (r.ReportStatus == Status.Complete || r.UnifiedAt != null)));
                }
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

        var pagedStudents = await query
            .OrderBy(s => s.FullName)
            .ToListAsync();

        var studentIds = pagedStudents.Select(s => s.ID).ToList();

        var reports = await _appDbContext.Reports
                                 .Include(r => r.SocialWorkerReport)
                                 .Include(r => r.PsychologistReport)
                                 .Include(r => r.PsychopedagogueReport)
                                 .AsNoTracking()
                                 .AsSplitQuery()
                                 .Where(r => r.StudentID.HasValue && studentIds.Contains(r.StudentID.Value))
                                 .ToListAsync();

        var rows = new List<SingularSystem.DTOs.Tracking.BiopsychosocialTrackingPDFRow>();
        foreach (var student in pagedStudents)
        {
            var report = reports.FirstOrDefault(r => r.StudentID == student.ID);
            
            var statusVal = report != null && (report.ReportStatus == Status.Complete || report.UnifiedAt != null) ? "Completo" : "Incompleto";
            if (report == null)
            {
                statusVal = "Não Iniciado";
            }

            var pendingList = new List<string>();
            bool swPending = report == null || report.SocialWorkerReport == null;
            bool psyPending = report == null || report.PsychologistReport == null || report.PsychologistReport.UnifiedAt == null;
            bool ppPending = report == null || report.PsychopedagogueReport == null || report.PsychopedagogueReport.UnifiedAt == null;

            if (psyPending) pendingList.Add("Psicólogo");
            if (ppPending) pendingList.Add("Psicopedagogo");
            if (swPending) pendingList.Add("Assistente Social");

            string pendingText;
            if (pendingList.Count == 0)
            {
                pendingText = "-";
            }
            else if (pendingList.Count == 3)
            {
                pendingText = "Todos pendentes";
            }
            else if (pendingList.Count == 2)
            {
                pendingText = $"{pendingList[0]} e {pendingList[1]} pendentes";
            }
            else
            {
                pendingText = $"{pendingList[0]} pendente";
            }

            rows.Add(new SingularSystem.DTOs.Tracking.BiopsychosocialTrackingPDFRow
            {
                StudentName = student.FullName,
                SchoolName = student.School != null ? student.School.Name : "-",
                Status = statusVal,
                PendingProfessionals = pendingText
            });
        }

        return BiopsychosocialTrackingPDFGenerator.Generate(rows);
    }
}
