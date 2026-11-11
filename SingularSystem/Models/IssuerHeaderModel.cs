using System.Collections.Generic;
using SingularSystem.Models.Entities;

namespace SingularSystem.Models;

public sealed record IssuerHeaderModel(
    string DisplayName,
    string? ProfessionalRegistration,
    string? Phone,
    string? Email,
    byte[]? LogoBytes,
    IReadOnlyList<string> Lines);

public static class IssuerHeaderModelFactory
{
    public const string DefaultBrandName = "Singular System";

    public static IssuerHeaderModel Build(
        IssuerProfile? profile,
        byte[]? logoBytes,
        byte[] singularLogo)
    {
        var lines = new List<string>();

        var displayName = !string.IsNullOrWhiteSpace(profile?.DisplayName)
            ? profile.DisplayName.Trim()
            : DefaultBrandName;

        lines.Add(displayName);

        string? reg = !string.IsNullOrWhiteSpace(profile?.ProfessionalRegistration)
            ? profile.ProfessionalRegistration.Trim()
            : null;

        if (reg != null)
        {
            lines.Add(reg);
        }

        string? phone = !string.IsNullOrWhiteSpace(profile?.Phone)
            ? profile.Phone.Trim()
            : null;

        if (phone != null)
        {
            lines.Add(phone);
        }

        string? email = !string.IsNullOrWhiteSpace(profile?.Email)
            ? profile.Email.Trim()
            : null;

        if (email != null)
        {
            lines.Add(email);
        }

        var effectiveLogo = (logoBytes != null && logoBytes.Length > 0)
            ? logoBytes
            : singularLogo;

        return new IssuerHeaderModel(
            DisplayName: displayName,
            ProfessionalRegistration: reg,
            Phone: phone,
            Email: email,
            LogoBytes: effectiveLogo,
            Lines: lines.AsReadOnly());
    }
}
