using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using FsCheck.Xunit;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using Xunit;

namespace SingularSystem.Tests;

/// <summary>
/// Feature: modulo-2-rebranding-pdfs, Property 8: Cabeçalho do emissor mostra só o que existe e sempre tem logotipo
/// Validates: Requirements 8.1, 8.3, 8.4, 8.8, 8.9
/// </summary>
public class IssuerHeaderModelPropertyTests
{
    private static readonly byte[] FallbackLogo = new byte[] { 0x11, 0x1E, 0x42, 0xFF };

    [Property(MaxTest = 100)]
    public Property ModeloCabecalho_EmiteApenasCamposPreenchidos_ESelecionaLogotipoCorreto(
        string? displayName,
        string? reg,
        string? phone,
        string? email,
        byte[]? logoBytes)
    {
        var profile = new IssuerProfile
        {
            DisplayName = displayName,
            ProfessionalRegistration = reg,
            Phone = phone,
            Email = email
        };

        var model = IssuerHeaderModelFactory.Build(profile, logoBytes, FallbackLogo);

        // 1. Linhas esperadas
        var expectedLines = new List<string>();

        var expectedDisplay = !string.IsNullOrWhiteSpace(displayName)
            ? displayName.Trim()
            : IssuerHeaderModelFactory.DefaultBrandName;
        expectedLines.Add(expectedDisplay);

        if (!string.IsNullOrWhiteSpace(reg))
        {
            expectedLines.Add(reg.Trim());
        }

        if (!string.IsNullOrWhiteSpace(phone))
        {
            expectedLines.Add(phone.Trim());
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            expectedLines.Add(email.Trim());
        }

        bool linesMatch = model.Lines.SequenceEqual(expectedLines);

        // 2. Logotipo esperado
        byte[] expectedLogo = (logoBytes != null && logoBytes.Length > 0)
            ? logoBytes
            : FallbackLogo;

        bool logoMatches = ReferenceEquals(model.LogoBytes, expectedLogo) ||
                           (model.LogoBytes != null && expectedLogo != null && model.LogoBytes.SequenceEqual(expectedLogo));

        // 3. DisplayName do model bate
        bool displayMatches = model.DisplayName == expectedDisplay;

        return (linesMatch && logoMatches && displayMatches)
            .Label($"LinesMatch: {linesMatch}, LogoMatches: {logoMatches}, DisplayMatches: {displayMatches}");
    }

    [Property(MaxTest = 100)]
    public Property PerfilNulo_GeraCabecalhoSingularSystemPadrao(byte[]? logoBytes)
    {
        var model = IssuerHeaderModelFactory.Build(null, logoBytes, FallbackLogo);

        bool displayIsDefault = model.DisplayName == IssuerHeaderModelFactory.DefaultBrandName;
        bool singleLine = model.Lines.Count == 1 && model.Lines[0] == IssuerHeaderModelFactory.DefaultBrandName;

        byte[] expectedLogo = (logoBytes != null && logoBytes.Length > 0)
            ? logoBytes
            : FallbackLogo;

        bool logoMatches = ReferenceEquals(model.LogoBytes, expectedLogo) ||
                           (model.LogoBytes != null && expectedLogo != null && model.LogoBytes.SequenceEqual(expectedLogo));

        return (displayIsDefault && singleLine && logoMatches)
            .Label($"Display: {displayIsDefault}, SingleLine: {singleLine}, LogoMatches: {logoMatches}");
    }
}
