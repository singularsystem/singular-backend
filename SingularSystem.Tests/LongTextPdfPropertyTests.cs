using System;
using System.IO;
using System.Linq;
using FsCheck;
using FsCheck.Xunit;
using QuestPDF.Infrastructure;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Services;
using Xunit;

namespace SingularSystem.Tests;

/// <summary>
/// Feature: modulo-2-rebranding-pdfs, Property 11: Texto longo não é truncado e acentos sobrevivem
/// Validates: Requirements 6.6, 9.3, 10.3, 11.6, 17.4
/// </summary>
public class LongTextPdfPropertyTests
{
    static LongTextPdfPropertyTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private static readonly string[] SampleWords = new[]
    {
        "atenção", "avaliação", "pedagógica", "psicológico", "socioemocional",
        "intervenção", "habilidade", "aprendizagem", "desenvolvimento", "adaptação",
        "currículo", "médico", "comunicação", "inclusão", "especializada"
    };

    [Property(MaxTest = 20)]
    public Property Texto_Longo_PtBr_Nao_Truncado_E_Preserva_Acentuacao(PositiveInt wordCount)
    {
        int count = Math.Clamp(wordCount.Get % 50 + 15, 15, 60);
        var words = new string[count];
        for (int i = 0; i < count; i++)
        {
            words[i] = SampleWords[i % SampleWords.Length];
        }

        var longText = string.Join(" ", words);

        var student = new Student
        {
            FullName = "Aluno Teste Acentuação",
            DateOfBirth = new DateTime(2014, 3, 12),
            School = new School { Name = "Escola Acentuação" }
        };

        var doc = new RegularTeacherEvaluationDocument
        {
            ID = Guid.NewGuid(),
            Student = student,
            User = new ApplicationUser { FullName = "Professora Especialista" },
            RegularTeacherQuestionnaire = new RegularTeacherQuestionnaire
            {
                MainDifficulties = longText,
                LearningCharacteristics = "Aprendizagem regular com acompanhamento.",
                ClassroomDevelopment = "Desenvolvimento adequado para a faixa etária."
            },
            RegularTeacherScreening = new RegularTeacherScreening()
        };

        var generator = new RegularTeacherCaseStudyPDFGenerator();
        var context = PdfRenderContext.Default(PdfProfile.View);
        var bytes = generator.GenerateAsync(doc, context).GetAwaiter().GetResult();

        var extractedText = PdfTextExtractor.ExtractFullText(bytes);

        // Verifica que todas as palavras com acentos e cedilhas estão no texto extraído
        foreach (var word in words)
        {
            Assert.Contains(word, extractedText, StringComparison.OrdinalIgnoreCase);
        }

        return true.ToProperty();
    }
}
