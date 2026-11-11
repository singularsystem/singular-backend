using System;
using FsCheck;
using FsCheck.Xunit;
using SingularSystem.Services;
using Xunit;

namespace SingularSystem.Tests;

/// <summary>
/// Feature: modulo-2-rebranding-pdfs, Property 10: Dimensão exibida preserva proporção e garante 300 DPI
/// Validates: Requirements 8.10, 13.2, 13.6
/// </summary>
public class LogoSizingPropertyTests
{
    private const float MmPerInch = 25.4f;

    [Property(MaxTest = 100)]
    public Property Dimensionamento_PreservaProporcao_CabeNaCaixa_EGarante300Dpi(
        PositiveInt pxWGen,
        PositiveInt pxHGen,
        PositiveInt maxWGen,
        PositiveInt maxHGen)
    {
        int pxW = 1 + (pxWGen.Get % 5000);
        int pxH = 1 + (pxHGen.Get % 5000);
        float maxWmm = 10f + (maxWGen.Get % 100);
        float maxHmm = 5f + (maxHGen.Get % 50);
        float minDpi = 300f;

        var size = LogoSizing.ComputeDisplaySize(pxW, pxH, maxWmm, maxHmm, minDpi);

        // 1. Cabe na caixa máxima
        bool fitsWidth = size.WidthMm <= maxWmm + 1e-4f;
        bool fitsHeight = size.HeightMm <= maxHmm + 1e-4f;
        bool fitsBox = fitsWidth && fitsHeight;

        // 2. Maior que zero
        bool positive = size.WidthMm > 0 && size.HeightMm > 0;

        // 3. Proporção preservada
        double originalRatio = (double)pxW / pxH;
        double displayRatio = (double)size.WidthMm / size.HeightMm;
        double ratioDiff = Math.Abs(originalRatio - displayRatio);
        bool ratioPreserved = ratioDiff < 1e-3 || Math.Abs(ratioDiff / originalRatio) < 1e-3;

        // 4. DPI efetivo >= 300 (tolerância por float)
        float displayedInchesW = size.WidthMm / MmPerInch;
        float displayedInchesH = size.HeightMm / MmPerInch;
        float effectiveDpiW = pxW / displayedInchesW;
        float effectiveDpiH = pxH / displayedInchesH;
        bool dpiSufficient = effectiveDpiW >= (minDpi - 1e-2f) && effectiveDpiH >= (minDpi - 1e-2f);

        return (fitsBox && positive && ratioPreserved && dpiSufficient)
            .Label($"px: {pxW}x{pxH}, sizeMm: {size.WidthMm}x{size.HeightMm}, effectiveDPI: {effectiveDpiW:F1}, ratioPreserved: {ratioPreserved}, fits: {fitsBox}");
    }

    [Property(MaxTest = 100)]
    public Property DimensoesInvalidas_RetornamZero(int pxW, int pxH)
    {
        if (pxW <= 0 || pxH <= 0)
        {
            var size = LogoSizing.ComputeDisplaySize(pxW, pxH);
            return (size.WidthMm == 0 && size.HeightMm == 0)
                .Label("Dimensões inválidas devem retornar 0x0");
        }

        return true.ToProperty();
    }
}
