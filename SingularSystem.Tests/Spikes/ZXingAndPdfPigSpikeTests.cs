using System;
using System.IO;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using Xunit;
using Xunit.Abstractions;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;
using ZXing.Rendering;

namespace SingularSystem.Tests.Spikes;

/// <summary>
/// Spike 1.2: Verificação de ZXing.Net e UglyToad.PdfPig.
/// 
/// Resultados confirmados:
/// - Versão estável fixada para ZXing.Net: 0.16.9 (no Backend e/ou Tests)
/// - Presença e funcionamento confirmados de:
///   * ZXing.Rendering.SvgRenderer (gera SVG vetorial limpo)
///   * ZXing.QrCode.QRCodeWriter / BarcodeWriterSvg com ErrorCorrectionLevel.M e Margin = 4
/// - Versão estável fixada para PdfPig: 0.1.9 (somente no SingularSystem.Tests)
/// - PdfPig lê com sucesso em PDFs gerados pelo QuestPDF:
///   * Texto por página (page.Text)
///   * Palavras com retângulos (page.GetWords() -> word.BoundingBox)
///   * Fontes (page.Letters -> letter.FontName)
///   * Imagens com retângulos (page.GetImages() -> image.Bounds)
/// - Requisitos validados: 12.10, 13.6, 16.4.
/// </summary>
public class ZXingAndPdfPigSpikeTests
{
    private readonly ITestOutputHelper _output;

    public ZXingAndPdfPigSpikeTests(ITestOutputHelper output)
    {
        _output = output;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [Fact]
    public void ZXing_QRCodeWriter_And_SvgRenderer_WorkCorrectly()
    {
        var writer = new BarcodeWriterSvg
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new QrCodeEncodingOptions
            {
                ErrorCorrection = ZXing.QrCode.Internal.ErrorCorrectionLevel.M,
                Margin = 4,
                Width = 200,
                Height = 200
            },
            Renderer = new SvgRenderer()
        };

        var svgResult = writer.Write("https://singularsystem.com.br/validar#test-token-12345");
        Assert.NotNull(svgResult);
        var svgContent = svgResult.Content;

        _output.WriteLine($"SVG Content snippet: {svgContent.Substring(0, Math.Min(120, svgContent.Length))}...");
        Assert.Contains("<svg", svgContent, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("</svg>", svgContent, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PdfPig_CanExtractTextWordsBoundingBoxesFontsAndImages()
    {
        // 1. Gerar um PDF com texto, fonte e imagem usando QuestPDF
        byte[] samplePng = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=");

        var pdfBytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20);
                page.Content().Column(col =>
                {
                    col.Item().Text("Singular System Spike Test").FontSize(18).Bold();
                    col.Item().Text("Relatório Biopsicossocial").FontSize(14);
                    col.Item().Image(samplePng).FitArea();
                });
            });
        }).GeneratePdf();

        // 2. Abrir com PdfPig e inspecionar
        using var pdfDocument = PdfDocument.Open(pdfBytes);
        Assert.Equal(1, pdfDocument.NumberOfPages);

        var page = pdfDocument.GetPage(1);
        string text = page.Text;
        _output.WriteLine($"Page text: {text}");
        Assert.Contains("Singular System", text);

        var words = page.GetWords().ToList();
        Assert.NotEmpty(words);
        var singularWord = words.FirstOrDefault(w => w.Text.Equals("Singular", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(singularWord);
        _output.WriteLine($"Word '{singularWord.Text}' BoundingBox: {singularWord.BoundingBox}");
        Assert.True(singularWord.BoundingBox.Width > 0);
        Assert.True(singularWord.BoundingBox.Height > 0);

        var letters = page.Letters.ToList();
        Assert.NotEmpty(letters);
        _output.WriteLine($"Sample letter font: {letters[0].FontName}");
        Assert.False(string.IsNullOrEmpty(letters[0].FontName));

        var images = page.GetImages().ToList();
        _output.WriteLine($"Images count: {images.Count}");
        Assert.NotEmpty(images);
        _output.WriteLine($"Image bounds: {images[0].Bounds}");
        Assert.True(images[0].Bounds.Width > 0);
    }
}
