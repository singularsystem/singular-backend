using FsCheck;
using FsCheck.Xunit;
using QuestPDF.Infrastructure;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Services;
using SingularSystem.Templates;
using System.Text.RegularExpressions;
using Xunit;

namespace SingularSystem.Tests;

/// <summary>
/// Feature: modulo-2-rebranding-pdfs, Property 13: Nenhum PDF gerado contém marca legada
/// Validates: Requirements 5.2, 11.5, 12.12, 18.2
/// </summary>
public class LegacyBrandAbsencePdfPropertyTests
{
    static LegacyBrandAbsencePdfPropertyTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private static readonly string[] LegacyTerms = new[]
    {
        "IPAAGS",
        "Neck System",
        "Neck Med",
        "NeckSign",
        "NeckBook",
        "Neck"
    };

    [Property(MaxTest = 20)]
    public Property GeneratedPdfs_NeverContain_LegacyBrandTerms()
    {
        var student = new Student
        {
            FullName = "Aluno Teste Auditoria",
            DateOfBirth = new DateTime(2013, 7, 10),
            School = new School { Name = "Escola Municipal Singular" }
        };

        var user = new ApplicationUser
        {
            FullName = "Profissional Singular",
            Email = "profissional@singular.com.br"
        };

        var doc = new RegularTeacherEvaluationDocument
        {
            ID = Guid.NewGuid(),
            Student = student,
            User = user,
            RegularTeacherQuestionnaire = new RegularTeacherQuestionnaire
            {
                ClassroomDevelopment = "Participação excelente em todas as atividades pedagógicas.",
                ClassroomBehavior = "Comportamento tranquilo e colaborativo.",
                MainDifficulties = "Nenhuma dificuldade reportada no momento."
            },
            RegularTeacherScreening = new RegularTeacherScreening()
        };

        var generator = new RegularTeacherCaseStudyPDFGenerator();
        var context = PdfRenderContext.Default(PdfProfile.View);
        var bytes = generator.GenerateAsync(doc, context).GetAwaiter().GetResult();

        var fullText = PdfTextExtractor.ExtractFullText(bytes);

        // Deve conter "Singular System"
        Assert.Contains("Singular System", fullText, StringComparison.OrdinalIgnoreCase);

        // NÃO deve conter termos legados
        foreach (var term in LegacyTerms)
        {
            // Use word boundary regex where appropriate or substring
            var isContained = Regex.IsMatch(fullText, $@"\b{Regex.Escape(term)}\b", RegexOptions.IgnoreCase);
            Assert.False(isContained, $"Extracted PDF text contains legacy term '{term}'. Text excerpt:\n{fullText.Substring(0, Math.Min(300, fullText.Length))}");
        }

        return true.ToProperty();
    }
}
