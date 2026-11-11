using FsCheck;
using FsCheck.Xunit;
using QuestPDF.Infrastructure;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Services;
using SingularSystem.Templates;
using System.Linq;
using Xunit;

namespace SingularSystem.Tests;

/// <summary>
/// Feature: modulo-2-rebranding-pdfs, Property 14: Sem sobreposição e dentro das margens
/// Validates: Requirements 10.2, 14.4
/// </summary>
public class PdfGeometryPropertyTests
{
    static PdfGeometryPropertyTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [Property(MaxTest = 20)]
    public Property GeneratedPdfs_BoundingBoxes_AreWithinPageBounds()
    {
        // A4 em pontos (72 pontos por polegada):
        // Largura = 210 mm ≈ 595.27 pt
        // Altura = 297 mm ≈ 841.89 pt
        const double PageWidthPt = 595.28;
        const double PageHeightPt = 841.89;

        var student = new Student
        {
            FullName = "Aluno Geometria",
            DateOfBirth = new DateTime(2012, 1, 15),
            School = new School { Name = "Escola Municipal Singular" }
        };

        var doc = new RegularTeacherEvaluationDocument
        {
            ID = Guid.NewGuid(),
            Student = student,
            User = new ApplicationUser { FullName = "Profissional Responsável" },
            RegularTeacherQuestionnaire = new RegularTeacherQuestionnaire
            {
                ClassroomDevelopment = "Desenvolvimento cognitivo padrão com bom engajamento.",
                ClassroomBehavior = "Comportamento adequado em sala de aula.",
                MainDifficulties = "Dificuldades leves de atenção sustentada."
            },
            RegularTeacherScreening = new RegularTeacherScreening()
        };

        var generator = new RegularTeacherCaseStudyPDFGenerator();
        var context = PdfRenderContext.Default(PdfProfile.View);
        var bytes = generator.GenerateAsync(doc, context).GetAwaiter().GetResult();

        var pages = PdfTextExtractor.ExtractPages(bytes);
        Assert.NotEmpty(pages);

        foreach (var page in pages)
        {
            foreach (var word in page.Words)
            {
                // Verifica que as palavras estão dentro dos limites da folha (com tolerância de 2 pt)
                Assert.True(word.Left >= -2.0, $"Word '{word.Text}' Left ({word.Left}) is negative on page {page.PageNumber}");
                Assert.True(word.Right <= PageWidthPt + 5.0, $"Word '{word.Text}' Right ({word.Right}) exceeds page width ({PageWidthPt}) on page {page.PageNumber}");
                Assert.True(word.Bottom >= -2.0, $"Word '{word.Text}' Bottom ({word.Bottom}) is negative on page {page.PageNumber}");
                Assert.True(word.Top <= PageHeightPt + 5.0, $"Word '{word.Text}' Top ({word.Top}) exceeds page height ({PageHeightPt}) on page {page.PageNumber}");
            }
        }

        return true.ToProperty();
    }
}
