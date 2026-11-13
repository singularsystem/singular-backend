using Microsoft.AspNetCore.DataProtection;
using DinkToPdf;
using DinkToPdf.Contracts;
using SingularSystem.Configuration;
using SingularSystem.Controllers;
using SingularSystem.Security;
using SingularSystem.Extensions;
using SingularSystem.Interfaces;
using SingularSystem.Models.Entities;
using SingularSystem.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Drawing;
using QuestPDF.Infrastructure;
using System.Globalization;
using Hangfire;
using Hangfire.SqlServer;
using SingularSystem.Authorization;
using SingularSystem.Middleware;
using SingularSystem.Models;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

var MyAllowSpecificOrigins = "MyPolicy";
var builder = WebApplication.CreateBuilder(args);

// Fail-fast de inicialização do provedor de assinatura (task 9.3, Req 5.5/5.6).
// Executado logo após a construção do builder e antes de qualquer registro/escuta do servidor:
// valor de SignatureProvider inválido/vazio, ou flag "NeckSign" com NeckSign:* ausente/inválido,
// encerra o processo com Environment.Exit(1) antes de o servidor HTTP subir.
// Fail-fast consolidado de inicialização (Req 5.1-5.8, Property 5, Task 12.1)
ConfigValidator.ValidateOrExit(builder.Configuration);
NeckBookStartupValidator.ValidateOrExit(builder.Configuration);

QuestPDF.Settings.License = LicenseType.Community;
SingularSystem.Fonts.PdfFonts.EnsureRegistered();

builder.Services.AddHttpClient();
builder.Services.AddHealthChecks();

// Cliente HTTP nomeado "NeckBook" (Req 3.1–3.6): BaseAddress de NeckBook:BaseUrl e Timeout de 30s,
// sem header de autenticação estático (a chave de API é injetada por requisição conforme o município).
builder.Services.AddHttpClient(NeckBookBooksController.NeckBookHttpClientName, (sp, client) =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var baseUrl = configuration["NeckBook:BaseUrl"];
    if (!string.IsNullOrWhiteSpace(baseUrl) &&
        Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
    {
        client.BaseAddress = baseUri;
    }

    var timeoutSeconds = int.TryParse(configuration["NeckBook:Timeout"], out var parsedTimeout) && parsedTimeout > 0
        ? parsedTimeout
        : 30;
    client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
});

// Cliente HTTP nomeado "NeckSign" (Req 4.1, 4.5): BaseAddress de NeckSign:BaseUrl, Timeout de 30s e header X-Api-Key
builder.Services.AddHttpClient(NeckSignService.NeckSignHttpClientName, (sp, client) =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var baseUrl = configuration["NeckSign:BaseUrl"];
    if (!string.IsNullOrWhiteSpace(baseUrl) &&
        Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
    {
        client.BaseAddress = baseUri;
    }

    client.Timeout = TimeSpan.FromSeconds(30);

    var apiKey = configuration["NeckSign:ApiKey"];
    if (!string.IsNullOrWhiteSpace(apiKey))
    {
        client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
    }
});
// NeckSign exclusivo como única implementação ativa de ISignatureService (Req 4.1, ADR-001)
builder.Services.AddScoped<NeckSignService>();
builder.Services.AddScoped<ISignatureService, NeckSignService>();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("public-validation", httpContext =>
    {
        var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            remoteIp,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });
});

builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection(SmtpOptions.SectionName));
builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromSeconds(31536000); // 1 ano (Req 9.2)
});
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<StudentEvaluationDocumentService>();
builder.Services.AddScoped<IUserSummaryFactory, UserSummaryFactory>();

// Resolver reutilizável de mídia assinada (detecção de origem + presigned URL do S3 próprio).
// Extraído de LegacyMediaMigrationService (spec signed-document-access, task 1.2) para ser
// consumido pelo DocumentsSignatureController sem acoplar à rotina de backup inteira.
builder.Services.AddScoped<ISignedMediaResolver, SignedMediaResolver>();

builder.Services.AddHangfire(config =>
    config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            new SqlServerStorageOptions
            {
                CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                QueuePollInterval = TimeSpan.FromSeconds(15),
                UseRecommendedIsolationLevel = true,
                DisableGlobalLocks = true
            }
        )
);

builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = Environment.ProcessorCount * 2;
    options.Queues = new[] { "default", "necksign", "signatures" };
});


builder.Services.AddCORSOrigins(builder.Configuration, MyAllowSpecificOrigins);
builder.Services.AddSignalR();
builder.Services.AddControllers().ConfigureApiBehaviorOptions(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(kvp => kvp.Value?.Errors.Count > 0)
            .Select(kvp => new
            {
                Field = kvp.Key,
                Errors = kvp.Value!.Errors.Select(e => e.ErrorMessage).Where(m => !string.IsNullOrWhiteSpace(m)).ToList()
            })
            .ToList();

        var message = errors.Count > 0
            ? "Dados inválidos: " + string.Join("; ", errors.SelectMany(e => e.Errors).Take(10))
            : "Dados inválidos.";

        return new BadRequestObjectResult(new ApiResponse<object>
        {
            Success = false,
            Message = message,
            Data = errors
        });
    };
});

builder.Services.AddSwaggerExplorer(builder.Environment)
                .InjectDbContext(builder.Configuration)
                .AddAppConfig(builder.Configuration)
                .LowercaseURLs()
                .AddIdentityHandlersAndStores()
                .ConfigureIdentityOptions()
                .AddIdentityAuth(builder.Configuration);

builder.Services.AddScoped<IIBGEService, IBGEService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IIssuerLogoStore, S3IssuerLogoStore>();
builder.Services.AddScoped<IIssuerResolver, IssuerResolver>();
builder.Services.AddScoped<IDocumentValidationService, DocumentValidationService>();
builder.Services.AddSingleton<IConverter>(new SynchronizedConverter(new PdfTools()));
builder.Services.AddScoped<IPDFService, PDFService>();
builder.Services.AddScoped<IStudentPDFGenerator, StudentsListPDFGenerator>();
builder.Services.AddScoped<IDocumentPdfGenerator, PAEEPDFGenerator>();
builder.Services.AddScoped<IDocumentPdfGenerator, RegularTeacherCaseStudyPDFGenerator>();
builder.Services.AddScoped<IDocumentPdfGenerator, ReportsPDFGenerator>();
builder.Services.AddScoped<IReportAEEPDFGenerator, ReportsAEEPDFGenerator>();
builder.Services.AddScoped<IDocumentPdfGenerator, PEIPDFGenerator>();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IPEISuggestionLoader, PEISuggestionLoader>();
builder.Services.AddSingleton<IPortageSuggestionLoader, PortageSuggestionLoader>();



// Integração NeckBook Books Proxy (task 13.1): DataProtection, protetor de chave, resolver por município e assembler.
var dataProtectionBuilder = builder.Services.AddDataProtection();
var keysDirectory = new System.IO.DirectoryInfo("/root/.aspnet/DataProtection-Keys");
if (keysDirectory.Exists)
{
    dataProtectionBuilder.PersistKeysToFileSystem(keysDirectory);
}
builder.Services.AddScoped<ApiKeyProtector>();
builder.Services.AddScoped<IMunicipalityNeckBookKeyResolver, MunicipalityNeckBookKeyResolver>();
builder.Services.AddScoped<NeckBookPayloadAssembler>();
builder.Services.AddPlanServices();

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("pt-BR");
    options.SupportedCultures = new[] { new CultureInfo("pt-BR") };
    options.SupportedUICultures = new[] { new CultureInfo("pt-BR") };
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseRateLimiter();

app.ConfigureSwaggerExplorer();
app.UseHsts();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var feature = context.Features.Get<IExceptionHandlerPathFeature>();
        var traceId = context.TraceIdentifier;

        if (feature?.Error != null)
        {
            var sanitized = LogSanitizer.SanitizeException(feature.Error);
            app.Logger.LogError(feature.Error, "[GlobalException] Erro em {Path}: {Message}", feature.Path, sanitized);
        }

        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json; charset=utf-8";

        var payload = new ApiResponse<object>
        {
            Success = false,
            Message = $"Ocorreu um erro interno no servidor. Código: {traceId}",
            Data = new { TraceId = traceId, Path = feature?.Path }
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    });
});

app.UseStatusCodePages(async statusContext =>
{
    var httpContext = statusContext.HttpContext;
    var response = httpContext.Response;

    if (response.HasStarted)
    {
        return;
    }

    string message = response.StatusCode switch
    {
        StatusCodes.Status400BadRequest => "Requisição inválida.",
        StatusCodes.Status401Unauthorized => "Não autorizado. Faça login novamente.",
        StatusCodes.Status403Forbidden => "Sem permissão para realizar esta ação.",
        StatusCodes.Status404NotFound => "Recurso não encontrado.",
        _ => $"Ocorreu um erro. Status: {response.StatusCode}"
    };

    response.ContentType = "application/json; charset=utf-8";

    var payload = new ApiResponse<object>
    {
        Success = false,
        Message = message,
        Data = new { TraceId = httpContext.TraceIdentifier }
    };

    await response.WriteAsync(JsonSerializer.Serialize(payload));
});

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    try
    {
        var outOfSyncReports = db.Reports
            .Include(r => r.PsychologistReport).ThenInclude(pr => pr!.SnapIVQuestionnaire)
            .Include(r => r.PsychopedagogueReport).ThenInclude(pp => pp!.PsychopedagogueQuestionnaire).ThenInclude(pq => pq.TypeOfDisability)
            .Include(r => r.PsychopedagogueReport).ThenInclude(pp => pp!.PsychopedagogueQuestionnaire).ThenInclude(pq => pq.TypeOfDisorder)
            .Where(r => r.ReportStatus == SingularSystem.Models.Enums.Status.Complete)
            .ToList();

        bool anyUpdated = false;
        foreach (var r in outOfSyncReports)
        {
            var correctValue = r.GetCombinedDisabilitiesAndDisorders();
            if (r.TypeDisability != correctValue)
            {
                r.TypeDisability = correctValue;
                db.Reports.Update(r);
                anyUpdated = true;
            }
        }
        if (anyUpdated)
        {
            db.SaveChanges();
        }

        // Sincronizar relatórios completos / assinados / prontos para unificação que ainda constavam como InProgress
        var reportsToSync = db.Reports
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
                    .ThenInclude(q => q.TypeOfDisability)
            .Include(r => r.PsychopedagogueReport)
                .ThenInclude(p => p.PsychopedagogueQuestionnaire)
                    .ThenInclude(q => q.TypeOfDisorder)
            .Include(r => r.PsychopedagogueReport)
                .ThenInclude(p => p.CARSQuestionnaire)
            .Include(r => r.PsychopedagogueReport)
                .ThenInclude(p => p.MCHATQuestionnaire)
            .Include(r => r.PsychopedagogueReport)
                .ThenInclude(p => p.CognitiveAssessment)
            .Where(r => r.ReportStatus != SingularSystem.Models.Enums.Status.Complete || r.UnifiedAt == null)
            .ToList();

        bool anySynced = false;
        foreach (var r in reportsToSync)
        {
            bool isCompleteBySignatures = r.SignatureBlocked ||
                r.DocumentStatus == SingularSystem.Models.Enums.Status.Complete ||
                (r.SocialWorkerSign && r.PsychologistSign && r.PsychopedagogueSign) ||
                (r.SocialWorkerSignedAt != null && r.PsychologistSignedAt != null && r.PsychopedagogueSignedAt != null);

            if (isCompleteBySignatures || r.IsReadyForUnification())
            {
                r.ReportStatus = SingularSystem.Models.Enums.Status.Complete;
                r.ReportConfirmed = true;
                r.UnifiedAt ??= r.UpdatedAt ?? r.CreatedAt;
                if (string.IsNullOrEmpty(r.TypeDisability))
                {
                    r.TypeDisability = r.GetCombinedDisabilitiesAndDisorders();
                }
                db.Reports.Update(r);
                anySynced = true;
            }
        }
        if (anySynced)
        {
            db.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error migrating reports TypeDisability or syncing complete status: {ex.Message}");
    }
}

await app.SeedIdentityData();

app.ConfigureCORS(MyAllowSpecificOrigins)
   .IdentityAuthMiddlewares();

app.UseMiddleware<TenantResolutionMiddleware>();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

app.MapHub<NotificationHub>("/api/hub/notifications").RequireCors("MyPolicy");
app.MapControllers();
app.MapHealthChecks("/health");

// task 9.4: job Hangfire recorrente de sincronização a cada 300s (5 min) chamando
// SyncPendingDocuments() via ISignatureService (Req 6.5). O job resolve a implementação ativa
// (ZapSign ou NeckSign) conforme a flag, pois o DI binda ISignatureService ao provedor ativo.
RecurringJob.AddOrUpdate<ISignatureService>(
    "sync-signatures",
    "signatures",
    x => x.SyncPendingDocuments(),
    "*/5 * * * *"
);
RecurringJob.RemoveIfExists("sync-necksign");


app.Run();
