using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace SingularSystem.Tests;

/// <summary>
/// Feature: modulo-2-rebranding-pdfs, Property 3: Toda exceção é justificada e restrita
/// Validates: Requirements 4.10
/// </summary>
public class BrandExceptionsPropertyTests
{
    [Fact]
    public void Arquivo_Oficial_BrandExceptions_Json_Passa_Sem_Erros()
    {
        string baseDir = AppContext.BaseDirectory;
        string rootDir = Path.GetFullPath(Path.Combine(baseDir, "../../../../../singular-backend"));
        string exceptionsPath = Path.Combine(rootDir, "brand-exceptions.json");

        Assert.True(File.Exists(exceptionsPath), $"brand-exceptions.json não encontrado em: {exceptionsPath}");

        var json = File.ReadAllText(exceptionsPath);
        var entries = JsonSerializer.Deserialize<List<BrandException>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(entries);
        Assert.NotEmpty(entries);

        var errors = LegacyScanner.ValidateExceptions(entries);
        Assert.Empty(errors);
    }

    [Property(MaxTest = 100)]
    public Property Rejeita_Entradas_Invalidas(PositiveInt seed)
    {
        var invalidReasons = new[] { "", "   ", "\t\r\n" };
        var universalGlobs = new[] { "*", "**", "**/*", "" };
        var validGlobs = new[] { "Resources/PEI/Historia/4ano.json", "Migrations/**", "Resources/term.txt" };
        var validPatterns = new[] { "prefeitura", "ipaag", "0071ce" };
        var validReasons = new[] { "Conteúdo pedagógico da BNCC", "Histórico de migrações EF" };

        int s = seed.Get;
        var invalidEntries = new List<BrandException>();

        int mode = s % 3;
        if (mode == 0)
        {
            // Razão inválida
            invalidEntries.Add(new BrandException(
                validGlobs[s % validGlobs.Length],
                validPatterns[s % validPatterns.Length],
                invalidReasons[s % invalidReasons.Length]));
        }
        else if (mode == 1)
        {
            // Glob universal
            invalidEntries.Add(new BrandException(
                universalGlobs[s % universalGlobs.Length],
                validPatterns[s % validPatterns.Length],
                validReasons[s % validReasons.Length]));
        }
        else
        {
            // Padrão vazio
            invalidEntries.Add(new BrandException(
                validGlobs[s % validGlobs.Length],
                invalidReasons[s % invalidReasons.Length],
                validReasons[s % validReasons.Length]));
        }

        var errors = LegacyScanner.ValidateExceptions(invalidEntries);
        return (errors.Count > 0).Label("Deveria conter erros de validação");
    }

    [Property(MaxTest = 100)]
    public Property Aceita_Entradas_Validas(PositiveInt seed)
    {
        var validGlobs = new[] { "Resources/PEI/Historia/4ano.json", "Migrations/**", "Resources/term.txt" };
        var validPatterns = new[] { "prefeitura", "ipaag", "0071ce" };
        var validReasons = new[] { "Conteúdo pedagógico da BNCC", "Histórico de migrações EF", "Validação jurídica" };

        int s = seed.Get;
        var validEntries = new List<BrandException>
        {
            new(
                validGlobs[s % validGlobs.Length],
                validPatterns[s % validPatterns.Length],
                validReasons[s % validReasons.Length])
        };

        var errors = LegacyScanner.ValidateExceptions(validEntries);
        return (errors.Count == 0).Label("Entrada válida não deveria conter erros");
    }
}
