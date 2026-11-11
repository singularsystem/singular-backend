using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SingularSystem.DTOs;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Services;

namespace SingularSystem.Controllers;

[ApiController]
[Route("api/v1/issuer-profile")]
[Authorize]
public class IssuerProfileController : BaseController
{
    private readonly AppDbContext _context;
    private readonly IIssuerLogoStore _logoStore;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IssuerProfileController> _logger;

    public IssuerProfileController(
        AppDbContext context,
        IIssuerLogoStore logoStore,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        ILogger<IssuerProfileController> logger)
    {
        _context = context;
        _logoStore = logoStore;
        _userManager = userManager;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IssuerProfileResponse>> GetProfile(CancellationToken ct)
    {
        var userId = CurrentUserId;
        var profile = await _context.IssuerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);

        if (profile == null)
        {
            var user = await _userManager.FindByIdAsync(userId);
            var defaultName = user?.FullName ?? user?.UserName ?? IssuerHeaderModelFactory.DefaultBrandName;

            return Ok(new IssuerProfileResponse(
                Id: Guid.Empty,
                DisplayName: defaultName,
                ProfessionalRegistration: null,
                Phone: user?.PhoneNumber,
                Email: user?.Email,
                LogoUrl: null,
                UpdatedAt: DateTime.UtcNow));
        }

        string? logoUrl = null;
        if (!string.IsNullOrWhiteSpace(profile.LogoObjectKey))
        {
            logoUrl = _logoStore.GetPreviewUrl(profile.LogoObjectKey, TimeSpan.FromMinutes(60));
        }

        return Ok(new IssuerProfileResponse(
            Id: profile.Id,
            DisplayName: profile.DisplayName,
            ProfessionalRegistration: profile.ProfessionalRegistration,
            Phone: profile.Phone,
            Email: profile.Email,
            LogoUrl: logoUrl,
            UpdatedAt: profile.UpdatedAt));
    }

    [HttpPut]
    public async Task<ActionResult<IssuerProfileResponse>> UpdateProfile(
        [FromBody] UpdateIssuerProfileRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            return BadRequest(new { message = "O nome de exibição (DisplayName) é obrigatório." });
        }

        var userId = CurrentUserId;
        var profile = await _context.IssuerProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);

        if (profile == null)
        {
            profile = new IssuerProfile
            {
                UserId = userId,
                DisplayName = request.DisplayName.Trim(),
                ProfessionalRegistration = string.IsNullOrWhiteSpace(request.ProfessionalRegistration) ? null : request.ProfessionalRegistration.Trim(),
                Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
                Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
                UpdatedAt = DateTime.UtcNow
            };
            _context.IssuerProfiles.Add(profile);
        }
        else
        {
            profile.DisplayName = request.DisplayName.Trim();
            profile.ProfessionalRegistration = string.IsNullOrWhiteSpace(request.ProfessionalRegistration) ? null : request.ProfessionalRegistration.Trim();
            profile.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
            profile.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
            profile.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(ct);

        string? logoUrl = null;
        if (!string.IsNullOrWhiteSpace(profile.LogoObjectKey))
        {
            logoUrl = _logoStore.GetPreviewUrl(profile.LogoObjectKey, TimeSpan.FromMinutes(60));
        }

        return Ok(new IssuerProfileResponse(
            Id: profile.Id,
            DisplayName: profile.DisplayName,
            ProfessionalRegistration: profile.ProfessionalRegistration,
            Phone: profile.Phone,
            Email: profile.Email,
            LogoUrl: logoUrl,
            UpdatedAt: profile.UpdatedAt));
    }

    [HttpPost("logo")]
    public async Task<ActionResult<IssuerLogoUploadResponse>> UploadLogo(
        IFormFile? file,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "Arquivo de logotipo ausente ou vazio." });
        }

        long maxBytes = LogoValidator.DefaultMaxBytes;
        if (long.TryParse(_configuration["Issuer:LogoMaxBytes"], out var cfgMax) && cfgMax > 0)
        {
            maxBytes = cfgMax;
        }

        int minWidthPx = LogoValidator.DefaultMinWidthPx;
        if (int.TryParse(_configuration["Issuer:LogoMinWidthPx"], out var cfgMinW) && cfgMinW > 0)
        {
            minWidthPx = cfgMinW;
        }

        byte[] fileBytes;
        using (var ms = new MemoryStream())
        {
            await file.CopyToAsync(ms, ct);
            fileBytes = ms.ToArray();
        }

        var validation = LogoValidator.Validate(fileBytes, file.ContentType, maxBytes, minWidthPx);
        if (!validation.IsValid || validation.Logo == null)
        {
            return BadRequest(new { message = validation.Error ?? "Logotipo inválido." });
        }

        var userId = CurrentUserId;
        var profile = await _context.IssuerProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);

        if (profile == null)
        {
            var user = await _userManager.FindByIdAsync(userId);
            var defaultName = user?.FullName ?? user?.UserName ?? IssuerHeaderModelFactory.DefaultBrandName;

            profile = new IssuerProfile
            {
                UserId = userId,
                DisplayName = defaultName,
                UpdatedAt = DateTime.UtcNow
            };
            _context.IssuerProfiles.Add(profile);
            await _context.SaveChangesAsync(ct);
        }

        var objectKey = await _logoStore.SaveAsync(profile.Id, validation.Logo, ct);
        profile.LogoObjectKey = objectKey;
        profile.LogoContentType = validation.Logo.ContentType;
        profile.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        var previewUrl = _logoStore.GetPreviewUrl(objectKey, TimeSpan.FromMinutes(60));
        return Ok(new IssuerLogoUploadResponse(previewUrl));
    }
}
