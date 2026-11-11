using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SingularSystem.Models;
using SingularSystem.Models.Entities;

namespace SingularSystem.Services;

public class IssuerResolver : IIssuerResolver
{
    private readonly AppDbContext _context;
    private readonly IIssuerLogoStore _logoStore;
    private readonly ILogger<IssuerResolver> _logger;
    private static readonly Lazy<byte[]> _singularLogo = new(LoadDefaultSingularLogo);

    public IssuerResolver(
        AppDbContext context,
        IIssuerLogoStore logoStore,
        ILogger<IssuerResolver> logger)
    {
        _context = context;
        _logoStore = logoStore;
        _logger = logger;
    }

    public async Task<IssuerHeaderModel> ResolveAsync(
        BaseDocument document,
        PdfProfile profile,
        CancellationToken ct = default)
    {
        IssuerProfile? issuerProfile = null;

        if (!string.IsNullOrWhiteSpace(document.UserID))
        {
            issuerProfile = await _context.IssuerProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == document.UserID, ct);
        }

        byte[]? logoBytes = null;

        if (issuerProfile != null && !string.IsNullOrWhiteSpace(issuerProfile.LogoObjectKey))
        {
            logoBytes = await _logoStore.TryReadAsync(issuerProfile.LogoObjectKey, ct);
            if (logoBytes == null)
            {
                _logger.LogWarning("Falha ao obter logotipo para IssuerProfile {IssuerProfileId}. Usando logotipo padrão Singular System.", issuerProfile.Id);
            }
        }

        return IssuerHeaderModelFactory.Build(issuerProfile, logoBytes, _singularLogo.Value);
    }

    private static byte[] LoadDefaultSingularLogo()
    {
        var basePath = AppContext.BaseDirectory;
        var path = Path.Combine(basePath, "Resources", "Brand", "logomarca-laranja.png");
        if (System.IO.File.Exists(path))
        {
            return System.IO.File.ReadAllBytes(path);
        }

        var fallback = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "Brand", "logomarca-laranja.png");
        if (System.IO.File.Exists(fallback))
        {
            return System.IO.File.ReadAllBytes(fallback);
        }

        return Array.Empty<byte>();
    }
}
