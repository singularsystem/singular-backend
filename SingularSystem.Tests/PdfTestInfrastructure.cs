using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace SingularSystem.Tests;

public sealed record ExtractedWord(string Text, double Left, double Bottom, double Right, double Top);

public sealed record ExtractedImage(double Left, double Bottom, double Right, double Top, int WidthPx, int HeightPx);

public sealed record ExtractedPdfPage(
    int PageNumber,
    string Text,
    IReadOnlyList<ExtractedWord> Words,
    IReadOnlyList<ExtractedImage> Images);

public static class PdfTextExtractor
{
    public static List<ExtractedPdfPage> ExtractPages(byte[] pdfBytes)
    {
        using var stream = new MemoryStream(pdfBytes);
        using var document = PdfDocument.Open(stream);

        var pages = new List<ExtractedPdfPage>();

        foreach (var page in document.GetPages())
        {
            var words = page.GetWords()
                .Select(w => new ExtractedWord(
                    w.Text,
                    w.BoundingBox.Left,
                    w.BoundingBox.Bottom,
                    w.BoundingBox.Right,
                    w.BoundingBox.Top))
                .ToList();

            var images = page.GetImages()
                .Select(img => new ExtractedImage(
                    img.Bounds.Left,
                    img.Bounds.Bottom,
                    img.Bounds.Right,
                    img.Bounds.Top,
                    img.WidthInSamples,
                    img.HeightInSamples))
                .ToList();

            pages.Add(new ExtractedPdfPage(
                page.Number,
                page.Text,
                words,
                images));
        }

        return pages;
    }

    public static string ExtractFullText(byte[] pdfBytes)
    {
        var pages = ExtractPages(pdfBytes);
        return string.Join("\n", pages.Select(p => p.Text));
    }
}
