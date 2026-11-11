using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace SingularSystem.Tests;

/// <summary>
/// Feature: modulo-2-rebranding-pdfs, Property 2: O detector de marca legada encontra qualquer variação
/// Validates: Requirements 3.1, 17.1, 17.2
/// </summary>
public class LegacyScannerPropertyTests
{
    private static readonly string[] Terms = LegacyScanner.LegacyTerms;
    private static readonly string[] HexColors = LegacyScanner.LegacyHexColors;

    private static string ApplyRandomCase(string input, int seed)
    {
        var random = new System.Random(seed);
        var chars = input.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            chars[i] = random.Next(2) == 0 ? char.ToUpperInvariant(chars[i]) : char.ToLowerInvariant(chars[i]);
        }
        return new string(chars);
    }

    [Property(MaxTest = 100)]
    public Property Detector_Encontra_Qualquer_Variacao_De_Termo_Legado(PositiveInt termIdx, int seed, PositiveInt padLines)
    {
        string baseTerm = Terms[termIdx.Get % Terms.Length];
        string casedTerm = ApplyRandomCase(baseTerm, seed);

        int padding = 1 + (padLines.Get % 15);
        var lines = Enumerable.Repeat("int safeValue = 42;", padding).ToList();
        lines.Add($"string title = \"{casedTerm}\";");
        string fullText = string.Join("\n", lines);

        var violations = LegacyScanner.Scan(fullText, "Controllers/SampleController.cs", Array.Empty<BrandException>());

        bool foundTerm = violations.Any(v =>
            v.TermType == "term" &&
            v.Line == padding + 1 &&
            v.Match.IndexOf(baseTerm, StringComparison.OrdinalIgnoreCase) >= 0);

        return foundTerm.Label($"Deveria detectar termo '{casedTerm}' na linha {padding + 1}");
    }

    [Property(MaxTest = 100)]
    public Property Detector_Encontra_Qualquer_Hex_Legado_Com_Ou_Sem_Hash(PositiveInt hexIdx, int seed, bool withHash)
    {
        string baseHex = HexColors[hexIdx.Get % HexColors.Length];
        string casedHex = ApplyRandomCase(baseHex, seed);
        string colorToken = withHash ? $"#{casedHex}" : casedHex;

        string text = $"string color = \"{colorToken}\";";
        var violations = LegacyScanner.Scan(text, "Templates/Sample.html", Array.Empty<BrandException>());

        bool foundColor = violations.Any(v =>
            v.TermType == "color" &&
            v.Match.IndexOf(baseHex, StringComparison.OrdinalIgnoreCase) >= 0);

        return foundColor.Label($"Deveria detectar cor '{colorToken}'");
    }

    [Property(MaxTest = 100)]
    public Property Excecao_Valida_Suprime_Violacao_Correspondente(PositiveInt termIdx, int seed)
    {
        string baseTerm = Terms[termIdx.Get % Terms.Length];
        string casedTerm = ApplyRandomCase(baseTerm, seed);

        string text = $"string history = \"{casedTerm}\";";
        string filePath = "Resources/PEI/Historia/4ano.json";

        var exceptions = new List<BrandException>
        {
            new("Resources/PEI/Historia/4ano.json", baseTerm, "Conteúdo pedagógico permitido")
        };

        var violations = LegacyScanner.Scan(text, filePath, exceptions);
        bool suppressed = !violations.Any(v => v.Match.IndexOf(baseTerm, StringComparison.OrdinalIgnoreCase) >= 0);

        return suppressed.Label($"Exceção deveria suprimir ocorrência de '{casedTerm}'");
    }
}
