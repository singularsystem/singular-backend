using System;
using SingularSystem.Models;

namespace SingularSystem.Services;

public sealed record PdfProfileParseResult(
    bool IsSuccess,
    PdfProfile Profile,
    string? ErrorMessage)
{
    public static PdfProfileParseResult Success(PdfProfile profile) =>
        new(true, profile, null);

    public static PdfProfileParseResult Fail(string error) =>
        new(false, PdfProfile.View, error);
}

public static class PdfProfileParser
{
    public const string InvalidProfileMessage = "Perfis aceitos: view, print";

    public static PdfProfileParseResult Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return PdfProfileParseResult.Success(PdfProfile.View);
        }

        var trimmed = raw.Trim();
        if (string.Equals(trimmed, "view", StringComparison.OrdinalIgnoreCase))
        {
            return PdfProfileParseResult.Success(PdfProfile.View);
        }

        if (string.Equals(trimmed, "print", StringComparison.OrdinalIgnoreCase))
        {
            return PdfProfileParseResult.Success(PdfProfile.Print);
        }

        return PdfProfileParseResult.Fail(InvalidProfileMessage);
    }
}
