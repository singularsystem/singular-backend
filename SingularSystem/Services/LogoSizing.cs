using System;

namespace SingularSystem.Services;

public sealed record LogoDisplaySize(float WidthMm, float HeightMm);

public static class LogoSizing
{
    public const float DefaultMaxWidthMm = 55f;
    public const float DefaultMaxHeightMm = 18f;
    public const float DefaultMinDpi = 300f;
    public const float MmPerInch = 25.4f;

    /// <summary>
    /// Calcula as dimensões de exibição do logotipo no PDF preservando rigorosamente a proporção.
    /// No perfil de impressão (Print), assegura que a imagem não seja ampliada além de 300 DPI efetivo
    /// e caiba dentro do bounding box (padrão 55 mm x 18 mm).
    /// </summary>
    public static LogoDisplaySize ComputeDisplaySize(
        int pxW,
        int pxH,
        float maxWmm = DefaultMaxWidthMm,
        float maxHmm = DefaultMaxHeightMm,
        float minDpi = DefaultMinDpi)
    {
        if (pxW <= 0 || pxH <= 0 || maxWmm <= 0 || maxHmm <= 0 || minDpi <= 0)
        {
            return new LogoDisplaySize(0, 0);
        }

        // Limite físico máximo permitido para garantir DPI >= minDpi
        // mm = (pixels / DPI) * 25.4
        var maxDpiWmm = (pxW / minDpi) * MmPerInch;
        var maxDpiHmm = (pxH / minDpi) * MmPerInch;

        // Bounding box efetivo: não excede a caixa do layout nem a dimensão permitida para 300 DPI
        var boundW = Math.Min(maxWmm, maxDpiWmm);
        var boundH = Math.Min(maxHmm, maxDpiHmm);

        // Fator de escala proporcional
        var scale = Math.Min(boundW / pxW, boundH / pxH);

        var finalW = pxW * scale;
        var finalH = pxH * scale;

        return new LogoDisplaySize(finalW, finalH);
    }
}
