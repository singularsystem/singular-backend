using QuestPDF.Infrastructure;
using SingularSystem.Models;

namespace SingularSystem.Templates;

public static class PdfMetadataFactory
{
    public const string DefaultCreator = "Singular System";
    public const string DefaultProducer = "QuestPDF 2025.12.3";

    public static DocumentMetadata CreateMetadata(
        PdfProfile profile,
        string? title = null,
        string? author = null,
        string? subject = null)
    {
        return new DocumentMetadata
        {
            Title = title ?? string.Empty,
            Author = author ?? string.Empty,
            Subject = subject ?? string.Empty,
            Creator = DefaultCreator,
            Producer = DefaultProducer,
#pragma warning disable CS0618
            RasterDpi = profile == PdfProfile.Print ? 300 : 144
#pragma warning restore CS0618
        };
    }

    public static DocumentSettings CreateSettings(PdfProfile profile)
    {
        return new DocumentSettings
        {
            ImageRasterDpi = profile == PdfProfile.Print ? 300 : 144,
            ImageCompressionQuality = profile == PdfProfile.Print ? ImageCompressionQuality.Best : ImageCompressionQuality.Medium
        };
    }

    public static (DocumentMetadata Metadata, DocumentSettings Settings) Create(
        PdfProfile profile,
        string? title = null,
        string? author = null,
        string? subject = null)
    {
        return (CreateMetadata(profile, title, author, subject), CreateSettings(profile));
    }
}
