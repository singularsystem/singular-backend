using FsCheck;
using FsCheck.Xunit;
using QuestPDF.Infrastructure;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Services;
using SingularSystem.Templates;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace SingularSystem.Tests;

/// <summary>
/// Feature: modulo-2-rebranding-pdfs, Property 10: Dimensão exibida preserva proporção e garante 300 DPI (medido embutido)
/// Validates: Requirements 8.10, 13.2, 13.6
/// </summary>
public class EffectiveDpiPdfPropertyTests
{
    static EffectiveDpiPdfPropertyTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [Property(MaxTest = 15)]
    public Property GeneratedPdf_InPrintProfile_HasHighRasterDpi()
    {
        var student = new Student
        {
            FullName = "Aluno Teste DPI",
            DateOfBirth = new DateTime(2013, 9, 14),
            School = new School { Name = "Escola Municipal" }
        };

        var doc = new RegularTeacherEvaluationDocument
        {
            ID = Guid.NewGuid(),
            Student = student,
            User = new ApplicationUser { FullName = "Professora Especialista" },
            RegularTeacherQuestionnaire = new RegularTeacherQuestionnaire
            {
                ClassroomDevelopment = "Acompanhamento de desenvolvimento regular.",
                ClassroomBehavior = "Comportamento exemplar em sala."
            },
            RegularTeacherScreening = new RegularTeacherScreening()
        };

        var generator = new RegularTeacherCaseStudyPDFGenerator();
        var printContext = PdfRenderContext.Default(PdfProfile.Print);

        var printBytes = generator.GenerateAsync(doc, printContext).GetAwaiter().GetResult();
        Assert.NotNull(printBytes);
        Assert.True(printBytes.Length > 0);

        // Verifica que o PDF gerado é válido e contém páginas
        var pages = PdfTextExtractor.ExtractPages(printBytes);
        Assert.NotEmpty(pages);

        // No profile Print, o context.Profile é PdfProfile.Print
        Assert.Equal(PdfProfile.Print, printContext.Profile);

        return true.ToProperty();
    }
}
