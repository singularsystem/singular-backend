using System;
using System.IO;
using SingularSystem.Extensions;

namespace SingularSystem.Models;

public sealed record PdfRenderContext(
    PdfProfile Profile,
    IssuerHeaderModel Issuer,
    byte[] SingularLogo,
    string? ValidationUrl,
    DateTime GeneratedAtBrt)
{
    public static PdfRenderContext Default(
        PdfProfile profile = PdfProfile.View,
        IssuerHeaderModel? issuer = null,
        byte[]? singularLogo = null,
        string? validationUrl = null,
        DateTime? generatedAtBrt = null)
    {
        var logo = singularLogo ?? LoadDefaultLogo();
        var iss = issuer ?? IssuerHeaderModelFactory.Build(null, null, logo);
        var dt = generatedAtBrt ?? DateTime.UtcNow.ToBrazilianTime();
        return new PdfRenderContext(profile, iss, logo, validationUrl, dt);
    }

    private static byte[] LoadDefaultLogo()
    {
        var basePath = AppContext.BaseDirectory;
        var path = Path.Combine(basePath, "Resources", "Brand", "logomarca-laranja.png");
        if (File.Exists(path))
        {
            return File.ReadAllBytes(path);
        }

        var fallback = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "Brand", "logomarca-laranja.png");
        if (File.Exists(fallback))
        {
            return File.ReadAllBytes(fallback);
        }

        return Array.Empty<byte>();
    }
}
