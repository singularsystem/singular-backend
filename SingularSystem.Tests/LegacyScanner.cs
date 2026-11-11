using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SingularSystem.Tests;

public sealed record BrandException(string Glob, string Pattern, string Reason);

public sealed record ScanViolation(
    string File,
    int Line,
    int Column,
    string Match,
    string TermType,
    string Snippet);

public static class LegacyScanner
{
    public static readonly string[] LegacyTerms = { "ipaag", "prefeitura" };

    public static readonly string[] LegacyHexColors =
    {
        "111c4e",
        "0a112f",
        "1c2c6e",
        "0d2a63",
        "0071ce",
        "19d3c5",
        "004f90",
        "005fb0",
        "0b6b6f",
        "5cc4c6",
        "e2f0f0",
        "090e27"
    };

    public static List<string> ValidateExceptions(IEnumerable<BrandException>? exceptions)
    {
        var errors = new List<string>();
        if (exceptions == null)
        {
            errors.Add("A lista de exceções não pode ser nula.");
            return errors;
        }

        int idx = 0;
        foreach (var exc in exceptions)
        {
            if (string.IsNullOrWhiteSpace(exc.Reason))
            {
                errors.Add($"Entrada {idx} ({exc.Glob}): 'Reason' é obrigatório e não pode ser vazio.");
            }

            if (string.IsNullOrWhiteSpace(exc.Pattern))
            {
                errors.Add($"Entrada {idx} ({exc.Glob}): 'Pattern' é obrigatório e não pode ser vazio.");
            }

            if (string.IsNullOrWhiteSpace(exc.Glob))
            {
                errors.Add($"Entrada {idx}: 'Glob' é obrigatório.");
            }
            else
            {
                var g = exc.Glob.Trim().Replace('\\', '/');
                if (g == "*" || g == "**" || g == "**/*")
                {
                    errors.Add($"Entrada {idx} ({exc.Glob}): 'Glob' universal não é permitido.");
                }
            }
            idx++;
        }

        return errors;
    }

    public static bool IsExceptionApplicable(string filePath, string matchedText, IEnumerable<BrandException> exceptions)
    {
        var normPath = filePath.Replace('\\', '/');

        foreach (var exc in exceptions)
        {
            try
            {
                var globRegex = GlobToRegex(exc.Glob);
                if (!globRegex.IsMatch(normPath))
                {
                    continue;
                }

                var patternRegex = new Regex(exc.Pattern, RegexOptions.IgnoreCase);
                if (patternRegex.IsMatch(matchedText))
                {
                    return true;
                }
            }
            catch
            {
                // Regex inválida não se aplica
            }
        }

        return false;
    }

    private static Regex GlobToRegex(string glob)
    {
        var normalized = glob.Replace('\\', '/');
        var escaped = Regex.Escape(normalized)
            .Replace(@"\*\*", ".*")
            .Replace(@"\*", "[^/]*");

        return new Regex($"(^|/){escaped}$", RegexOptions.IgnoreCase);
    }

    public static List<ScanViolation> Scan(string text, string filePath, IReadOnlyList<BrandException>? exceptions = null)
    {
        var effectiveExceptions = exceptions ?? Array.Empty<BrandException>();
        var violations = new List<ScanViolation>();
        if (string.IsNullOrEmpty(text))
        {
            return violations;
        }

        var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        var termRegex = new Regex(@"\b(ipaag[a-z0-9_-]*|prefeitura[a-z0-9_-]*)\b", RegexOptions.IgnoreCase);

        var hexPatternString = string.Join("|", LegacyHexColors);
        var colorRegex = new Regex(@"(?<=^|[^a-fA-F0-9])#?(" + hexPatternString + @")(?=[^a-fA-F0-9]|$)", RegexOptions.IgnoreCase);

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            int lineNum = i + 1;

            var termMatches = termRegex.Matches(line);
            foreach (Match tm in termMatches)
            {
                if (!IsExceptionApplicable(filePath, tm.Value, effectiveExceptions))
                {
                    violations.Add(new ScanViolation(
                        filePath,
                        lineNum,
                        tm.Index + 1,
                        tm.Value,
                        "term",
                        line.Trim()));
                }
            }

            var colorMatches = colorRegex.Matches(line);
            foreach (Match cm in colorMatches)
            {
                if (!IsExceptionApplicable(filePath, cm.Value, effectiveExceptions))
                {
                    violations.Add(new ScanViolation(
                        filePath,
                        lineNum,
                        cm.Index + 1,
                        cm.Value,
                        "color",
                        line.Trim()));
                }
            }
        }

        return violations;
    }
}
