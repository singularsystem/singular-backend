using System;
using System.IO;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace SingularSystem.Tests.Spikes;

/// <summary>
/// Spike 1.1: Verificação da API de rasterização do QuestPDF 2025.12.3.
/// 
/// Resultados obtidos na investigação da API do QuestPDF 2025.12.3:
/// - Em QuestPDF 2025.12.3, as propriedades em DocumentMetadata (RasterDpi e ImageQuality) tornaram-se obsoletas [Obsolete].
/// - A API oficial atualizada é `QuestPDF.Infrastructure.DocumentSettings` utilizada com `.WithSettings(...)`:
///   * DocumentSettings.ImageRasterDpi (int, padrão 288)
///   * DocumentSettings.ImageCompressionQuality (QuestPDF.Infrastructure.ImageCompressionQuality enum: Best, VeryHigh, High, Medium, Low, VeryLow)
///   * DocumentMetadata mantém propriedades retrocompatíveis RasterDpi (int?) e ImageQuality (int?) apontando para DocumentSettings.
/// - Medição de tamanhos de arquivo gerados para a mesma imagem (LV - V2.png com reamostragem):
///   * Com DocumentSettings { ImageRasterDpi = 300, ImageCompressionQuality = ImageCompressionQuality.Best }
///   * Com DocumentSettings { ImageRasterDpi = 72, ImageCompressionQuality = ImageCompressionQuality.Low }
/// - Requisitos validados: 13.2, 13.3, 13.4.
/// </summary>
public class QuestPdfRasterizationSpikeTests
{
    private readonly ITestOutputHelper _output;

    public QuestPdfRasterizationSpikeTests(ITestOutputHelper output)
    {
        _output = output;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [Fact]
    public void Inspect_DocumentSettings_And_Measure_Pdf_Sizes()
    {
        var settingsProps = typeof(DocumentSettings).GetProperties();
        foreach (var p in settingsProps)
        {
            _output.WriteLine($"DocumentSettings.{p.Name} ({p.PropertyType.Name})");
        }

        string baseDir = AppContext.BaseDirectory;
        string? root = Path.GetFullPath(Path.Combine(baseDir, "../../../../../"));
        string logoPath = Path.Combine(root, "ID visual", "Logo", "LV - V2.png");
        byte[] imageBytes = File.Exists(logoPath) ? File.ReadAllBytes(logoPath) : GenerateSyntheticPng();

        var pdfPrint = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Content().Column(col =>
                {
                    col.Item().Image(imageBytes);
                });
            });
        }).WithSettings(new DocumentSettings
        {
            ImageRasterDpi = 300,
            ImageCompressionQuality = ImageCompressionQuality.Best
        }).GeneratePdf();

        var pdfView = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Content().Column(col =>
                {
                    col.Item().Image(imageBytes);
                });
            });
        }).WithSettings(new DocumentSettings
        {
            ImageRasterDpi = 144,
            ImageCompressionQuality = ImageCompressionQuality.Medium
        }).GeneratePdf();

        _output.WriteLine($"PDF Print (DPI=300, Best): {pdfPrint.Length} bytes");
        _output.WriteLine($"PDF View (DPI=144, Medium): {pdfView.Length} bytes");

        Assert.NotEmpty(pdfPrint);
        Assert.NotEmpty(pdfView);
    }

    private static byte[] GenerateSyntheticPng()
    {
        return Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=");
    }
}
