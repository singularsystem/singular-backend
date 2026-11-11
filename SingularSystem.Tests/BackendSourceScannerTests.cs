using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace SingularSystem.Tests;

/// <summary>
/// Task 4.10: Teste de varredura do Backend
/// Percorre o código-fonte, os templates e os recursos do Backend e falha em Termo_Legado
/// ou hex legado fora da Lista_Excecoes.
/// Validates: Requirements 17.1, 17.2, 18.2
/// </summary>
public class BackendSourceScannerTests
{
    private static readonly HashSet<string> IncludedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".json", ".html", ".xml", ".txt", ".sql", ".ps1"
    };

    private static readonly HashSet<string> ExcludedDirs = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "obj", ".git", ".vs", "Migrations"
    };

    [Fact]
    public void Varredura_Backend_Nao_Deve_Conter_Termos_Ou_Cores_Legadas_Fora_Das_Excecoes()
    {
        string baseDir = AppContext.BaseDirectory;
        string rootDir = Path.GetFullPath(Path.Combine(baseDir, "../../../../../singular-backend"));
        string exceptionsPath = Path.Combine(rootDir, "brand-exceptions.json");

        Assert.True(File.Exists(exceptionsPath), $"brand-exceptions.json não encontrado em: {exceptionsPath}");

        var json = File.ReadAllText(exceptionsPath);
        var exceptions = JsonSerializer.Deserialize<List<BrandException>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new List<BrandException>();

        string projectDir = Path.Combine(rootDir, "SingularSystem");
        Assert.True(Directory.Exists(projectDir), $"Diretório do projeto não encontrado: {projectDir}");

        var allFiles = Directory.EnumerateFiles(projectDir, "*.*", SearchOption.AllDirectories)
            .Where(f => IncludedExtensions.Contains(Path.GetExtension(f)))
            .Where(f => !Path.GetDirectoryName(f)!.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(part => ExcludedDirs.Contains(part)))
            .ToList();

        Assert.NotEmpty(allFiles);

        var allViolations = new List<ScanViolation>();

        foreach (var file in allFiles)
        {
            var relativePath = Path.GetRelativePath(projectDir, file).Replace('\\', '/');
            var content = File.ReadAllText(file);

            var violations = LegacyScanner.Scan(content, relativePath, exceptions);
            allViolations.AddRange(violations);
        }

        if (allViolations.Count > 0)
        {
            var details = string.Join("\n", allViolations.Select(v =>
                $"{v.File}:{v.Line}:{v.Column} [{v.TermType}] '{v.Match}' -> {v.Snippet}"));

            Assert.Fail($"Foram encontradas {allViolations.Count} violações no backend:\n{details}");
        }
    }
}
