using FsCheck;
using FsCheck.Xunit;
using QuestPDF.Infrastructure;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Services;
using System.Text.RegularExpressions;
using Xunit;

namespace SingularSystem.Tests;

public class PaginationPdfPropertyTests
{
    static PaginationPdfPropertyTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [Property(MaxTest = 30)]
    public Property GeneratedPdfs_AlwaysContain_PaginaKDeN_OnEveryPage()
    {
        // Property 12: Rodapé "Página k de N"
        // Validates: Requirements 5.4, 9.4, 10.4, 11.2, 12.12, 17.5
        var doc = new RegularTeacherEvaluationDocument
        {
            ID = Guid.NewGuid(),
            Student = new Student
            {
                FullName = "Aluno Teste Paginação",
                DateOfBirth = new DateTime(2015, 5, 20),
                School = new School { Name = "Escola Municipal" }
            },
            User = new ApplicationUser { FullName = "Professora de AEE" },
            RegularTeacherQuestionnaire = new RegularTeacherQuestionnaire
            {
                ClassroomDevelopment = "Desenvolvimento observado ao longo do ano letivo.",
                ClassroomBehavior = "Comportamento exemplar e participativo.",
                MainDifficulties = "Pequenas dificuldades com cálculos complexos."
            },
            RegularTeacherScreening = new RegularTeacherScreening()
        };

        var generator = new RegularTeacherCaseStudyPDFGenerator();
        var context = PdfRenderContext.Default(PdfProfile.View);
        var bytes = generator.GenerateAsync(doc, context).GetAwaiter().GetResult();

        var pages = PdfTextExtractor.ExtractPages(bytes);
        Assert.NotEmpty(pages);

        var totalPages = pages.Count;
        for (int k = 1; k <= totalPages; k++)
        {
            var text = pages[k - 1].Text;
            // Deve conter "Página k de N"
            var expectedPattern = $@"Página\s+{k}\s+de\s+{totalPages}";
            var match = Regex.IsMatch(text, expectedPattern, RegexOptions.IgnoreCase);
            Assert.True(match, $"Page {k} text did not match '{expectedPattern}'. Extracted text: {text}");
        }

        return true.ToProperty();
    }
}
