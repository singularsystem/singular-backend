using System;
using FsCheck;
using FsCheck.Xunit;
using SingularSystem.Models;
using SingularSystem.Services;
using Xunit;

namespace SingularSystem.Tests;

/// <summary>
/// Feature: modulo-2-rebranding-pdfs, Property 20: Parser do perfil de exportação
/// Validates: Requirements 13.1, 13.7, 13.8
/// </summary>
public class PdfProfileParserPropertyTests
{
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

    [Fact]
    public void Nulo_Ou_Vazio_Retorna_View_Por_Padrao()
    {
        var resNull = PdfProfileParser.Parse(null);
        Assert.True(resNull.IsSuccess);
        Assert.Equal(PdfProfile.View, resNull.Profile);
        Assert.Null(resNull.ErrorMessage);

        var resEmpty = PdfProfileParser.Parse(string.Empty);
        Assert.True(resEmpty.IsSuccess);
        Assert.Equal(PdfProfile.View, resEmpty.Profile);

        var resWhitespace = PdfProfileParser.Parse("   \t\r\n  ");
        Assert.True(resWhitespace.IsSuccess);
        Assert.Equal(PdfProfile.View, resWhitespace.Profile);
    }

    [Property(MaxTest = 100)]
    public Property View_Em_Qualquer_Caixa_Ou_Espacamento_Retorna_View(int seed, PositiveInt padLeft, PositiveInt padRight)
    {
        string cased = ApplyRandomCase("view", seed);
        string padded = new string(' ', padLeft.Get % 5) + cased + new string(' ', padRight.Get % 5);

        var result = PdfProfileParser.Parse(padded);

        bool success = result.IsSuccess && result.Profile == PdfProfile.View && result.ErrorMessage == null;
        return success.Label($"'{padded}' deveria ser parseado como View");
    }

    [Property(MaxTest = 100)]
    public Property Print_Em_Qualquer_Caixa_Ou_Espacamento_Retorna_Print(int seed, PositiveInt padLeft, PositiveInt padRight)
    {
        string cased = ApplyRandomCase("print", seed);
        string padded = new string(' ', padLeft.Get % 5) + cased + new string(' ', padRight.Get % 5);

        var result = PdfProfileParser.Parse(padded);

        bool success = result.IsSuccess && result.Profile == PdfProfile.Print && result.ErrorMessage == null;
        return success.Label($"'{padded}' deveria ser parseado como Print");
    }

    [Property(MaxTest = 100)]
    public Property Outros_Valores_Falham_Com_Mensagem_Descritiva(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return true.ToProperty();
        }

        var trimmed = raw.Trim();
        if (trimmed.Equals("view", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Equals("print", StringComparison.OrdinalIgnoreCase))
        {
            return true.ToProperty();
        }

        var result = PdfProfileParser.Parse(raw);

        bool failed = !result.IsSuccess &&
                      result.ErrorMessage != null &&
                      result.ErrorMessage == PdfProfileParser.InvalidProfileMessage;

        return failed.Label($"'{raw}' deveria falhar com mensagem '{PdfProfileParser.InvalidProfileMessage}'");
    }
}
