using System;
using System.IO;
using FsCheck;
using FsCheck.Xunit;
using SingularSystem.Services;
using Xunit;

namespace SingularSystem.Tests;

/// <summary>
/// Feature: modulo-2-rebranding-pdfs, Property 9: Validação do arquivo de logo
/// Validates: Requirements 8.6, 8.7
/// </summary>
public class LogoValidatorPropertyTests
{
    private static byte[] CreatePng(int width, int height, int extraPadding = 0)
    {
        using var ms = new MemoryStream();
        ms.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }); // Magic
        ms.Write(new byte[] { 0, 0, 0, 13 }); // IHDR length
        ms.Write(new byte[] { 0x49, 0x48, 0x44, 0x52 }); // "IHDR"
        ms.Write(new byte[] { (byte)(width >> 24), (byte)(width >> 16), (byte)(width >> 8), (byte)width });
        ms.Write(new byte[] { (byte)(height >> 24), (byte)(height >> 16), (byte)(height >> 8), (byte)height });
        ms.Write(new byte[] { 8, 2, 0, 0, 0, 0, 0, 0, 0 }); // 5 info bytes + 4 CRC bytes
        if (extraPadding > 0)
        {
            ms.Write(new byte[extraPadding]);
        }
        return ms.ToArray();
    }

    private static byte[] CreateJpeg(int width, int height, int extraPadding = 0)
    {
        using var ms = new MemoryStream();
        ms.Write(new byte[] { 0xFF, 0xD8 }); // SOI
        ms.Write(new byte[] { 0xFF, 0xC0 }); // SOF0
        ms.Write(new byte[] { 0x00, 0x11 }); // Segment length = 17
        ms.WriteByte(8); // Precision
        ms.Write(new byte[] { (byte)(height >> 8), (byte)height });
        ms.Write(new byte[] { (byte)(width >> 8), (byte)width });
        ms.Write(new byte[9]); // 3 components (3 * 3 bytes)
        if (extraPadding > 0)
        {
            ms.Write(new byte[extraPadding]);
        }
        return ms.ToArray();
    }

    [Property(MaxTest = 100)]
    public Property Valida_Png_Valido_Com_Dimensoes_E_Limites_Aceito(PositiveInt w, PositiveInt h)
    {
        // Width >= 300
        int width = 300 + (w.Get % 3000);
        int height = 1 + (h.Get % 3000);

        var data = CreatePng(width, height);
        long maxBytes = data.Length + 1024;
        int minWidth = 300;

        var result = LogoValidator.Validate(data, declaredContentType: "image/png", maxBytes: maxBytes, minWidthPx: minWidth);

        bool valid = result.IsValid &&
                     result.Logo != null &&
                     result.Logo.WidthPx == width &&
                     result.Logo.HeightPx == height &&
                     result.Logo.ContentType == "image/png" &&
                     result.Error == null;

        return valid.Label($"PNG {width}x{height} deveria ser aceito");
    }

    [Property(MaxTest = 100)]
    public Property Valida_Jpeg_Valido_Com_Dimensoes_E_Limites_Aceito(PositiveInt w, PositiveInt h)
    {
        // Width >= 300, within 16-bit JPEG limits
        int width = 300 + (w.Get % 3000);
        int height = 1 + (h.Get % 3000);

        var data = CreateJpeg(width, height);
        long maxBytes = data.Length + 1024;
        int minWidth = 300;

        var result = LogoValidator.Validate(data, declaredContentType: "image/jpeg", maxBytes: maxBytes, minWidthPx: minWidth);

        bool valid = result.IsValid &&
                     result.Logo != null &&
                     result.Logo.WidthPx == width &&
                     result.Logo.HeightPx == height &&
                     result.Logo.ContentType == "image/jpeg" &&
                     result.Error == null;

        return valid.Label($"JPEG {width}x{height} deveria ser aceito");
    }

    [Property(MaxTest = 100)]
    public Property Rejeita_Se_Largura_Menor_Que_Minimo(PositiveInt w, PositiveInt h)
    {
        int minWidth = 300;
        int width = 1 + (w.Get % (minWidth - 1)); // strictly < minWidth
        int height = 1 + (h.Get % 1000);

        var data = CreatePng(width, height);
        var result = LogoValidator.Validate(data, maxBytes: 10 * 1024 * 1024, minWidthPx: minWidth);

        bool rejected = !result.IsValid &&
                        result.Logo == null &&
                        !string.IsNullOrWhiteSpace(result.Error) &&
                        result.Error.Contains("largura", StringComparison.OrdinalIgnoreCase);

        return rejected.Label($"PNG com largura {width} < {minWidth} deveria ser rejeitado");
    }

    [Property(MaxTest = 100)]
    public Property Rejeita_Se_Tamanho_Maior_Que_MaxBytes(PositiveInt pad)
    {
        int maxBytes = 1000;
        int padding = maxBytes + (pad.Get % 5000);
        var data = CreatePng(400, 200, extraPadding: padding);

        var result = LogoValidator.Validate(data, maxBytes: maxBytes, minWidthPx: 300);

        bool rejected = !result.IsValid &&
                        result.Logo == null &&
                        !string.IsNullOrWhiteSpace(result.Error) &&
                        result.Error.Contains("limite", StringComparison.OrdinalIgnoreCase);

        return rejected.Label($"PNG com {data.Length} bytes > {maxBytes} deveria ser rejeitado");
    }

    [Property(MaxTest = 100)]
    public Property Rejeita_Bytes_Aleatorios_Sem_Assinatura_Valida(byte[] randomBytes)
    {
        // Se calhar de gerar assinatura PNG ou JPEG (extremamente improvável), ignora
        if (randomBytes == null || randomBytes.Length < 8)
        {
            var res = LogoValidator.Validate(randomBytes);
            return (!res.IsValid).Label("Array nulo ou muito curto deve ser rejeitado");
        }

        // Garante que não começa com magic de PNG nem JPEG
        if (randomBytes[0] == 0x89 && randomBytes[1] == 0x50)
            randomBytes[0] = 0x00;
        if (randomBytes[0] == 0xFF && randomBytes[1] == 0xD8)
            randomBytes[0] = 0x00;

        var result = LogoValidator.Validate(randomBytes, maxBytes: 10 * 1024 * 1024, minWidthPx: 10);

        bool rejected = !result.IsValid &&
                        result.Logo == null &&
                        !string.IsNullOrWhiteSpace(result.Error);

        return rejected.Label("Bytes sem magic válido devem ser rejeitados");
    }
}
