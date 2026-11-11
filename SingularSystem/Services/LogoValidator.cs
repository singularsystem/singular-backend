using System;

namespace SingularSystem.Services;

public sealed record ValidatedLogo(
    byte[] Data,
    string ContentType,
    int WidthPx,
    int HeightPx);

public sealed record LogoValidationResult(
    bool IsValid,
    string? Error,
    ValidatedLogo? Logo = null)
{
    public static LogoValidationResult Success(ValidatedLogo logo) =>
        new(true, null, logo);

    public static LogoValidationResult Fail(string error) =>
        new(false, error, null);
}

public static class LogoValidator
{
    public const long DefaultMaxBytes = 2 * 1024 * 1024; // 2 MB
    public const int DefaultMinWidthPx = 300; // 300 px

    // PNG signature: 89 50 4E 47 0D 0A 1A 0A
    private static readonly byte[] PngMagic = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    public static LogoValidationResult Validate(
        byte[]? data,
        string? declaredContentType = null,
        long maxBytes = DefaultMaxBytes,
        int minWidthPx = DefaultMinWidthPx)
    {
        if (data == null || data.Length == 0)
        {
            return LogoValidationResult.Fail("Arquivo de logotipo ausente ou vazio.");
        }

        if (data.Length > maxBytes)
        {
            return LogoValidationResult.Fail($"Tamanho do logotipo ({data.Length} bytes) excede o limite máximo permitido de {maxBytes} bytes.");
        }

        string contentType;
        int width;
        int height;

        if (IsPng(data))
        {
            contentType = "image/png";
            if (!TryReadPngDimensions(data, out width, out height))
            {
                return LogoValidationResult.Fail("Arquivo PNG corrompido ou dimensões inválidas.");
            }
        }
        else if (IsJpeg(data))
        {
            contentType = "image/jpeg";
            if (!TryReadJpegDimensions(data, out width, out height))
            {
                return LogoValidationResult.Fail("Arquivo JPEG corrompido ou dimensões inválidas.");
            }
        }
        else
        {
            return LogoValidationResult.Fail("Formato de imagem inválido. Somente PNG e JPEG são aceitos.");
        }

        if (width < minWidthPx)
        {
            return LogoValidationResult.Fail($"Largura da imagem ({width} px) é menor que a largura mínima permitida ({minWidthPx} px).");
        }

        return LogoValidationResult.Success(new ValidatedLogo(data, contentType, width, height));
    }

    public static bool IsPng(byte[] data)
    {
        if (data.Length < PngMagic.Length) return false;
        for (int i = 0; i < PngMagic.Length; i++)
        {
            if (data[i] != PngMagic[i]) return false;
        }
        return true;
    }

    public static bool IsJpeg(byte[] data)
    {
        if (data.Length < 3) return false;
        return data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF;
    }

    public static bool TryReadPngDimensions(byte[] data, out int width, out int height)
    {
        width = 0;
        height = 0;
        if (data.Length < 24) return false;

        width = (data[16] << 24) | (data[17] << 16) | (data[18] << 8) | data[19];
        height = (data[20] << 24) | (data[21] << 16) | (data[22] << 8) | data[23];
        return width > 0 && height > 0;
    }

    public static bool TryReadJpegDimensions(byte[] data, out int width, out int height)
    {
        width = 0;
        height = 0;
        if (data.Length < 4) return false;

        int pos = 2; // Skip 0xFF, 0xD8
        while (pos < data.Length - 1)
        {
            if (data[pos] != 0xFF)
            {
                pos++;
                continue;
            }

            byte marker = data[pos + 1];
            pos += 2;

            if (pos + 2 > data.Length) break;
            int length = (data[pos] << 8) | data[pos + 1];

            // SOF markers
            if (marker is 0xC0 or 0xC1 or 0xC2 or 0xC3 or 0xC5 or 0xC6 or 0xC7 or 0xC9 or 0xCA or 0xCB or 0xCD or 0xCE or 0xCF)
            {
                if (pos + 7 <= data.Length)
                {
                    height = (data[pos + 3] << 8) | data[pos + 4];
                    width = (data[pos + 5] << 8) | data[pos + 6];
                    return width > 0 && height > 0;
                }
                break;
            }

            if (marker is 0xDA or 0xD9)
            {
                break;
            }

            pos += length;
        }

        return false;
    }
}
