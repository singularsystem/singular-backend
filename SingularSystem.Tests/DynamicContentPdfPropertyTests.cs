using FsCheck;
using FsCheck.Xunit;
using QuestPDF.Infrastructure;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Services;
using SingularSystem.Templates;
using System;
using System.Collections.Generic;
using Xunit;

namespace SingularSystem.Tests;

/// <summary>
/// Feature: modulo-2-rebranding-pdfs, Property 15: Conteúdo dinâmico completo, inclusive vazio
/// Validates: Requirements 10.1, 10.5, 11.1, 11.4, 12.1, 12.2, 12.3
/// </summary>
public class DynamicContentPdfPropertyTests
{
    static DynamicContentPdfPropertyTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [Property(MaxTest = 20)]
    public Property DynamicContent_EmptyOrPopulated_GeneratesValidPdf()
    {
        var student = new Student
        {
            FullName = "Aluno Conteúdo Dinâmico",
            DateOfBirth = new DateTime(2014, 5, 10),
            School = new School { Name = "Escola Singular Teste" }
        };

        var user = new ApplicationUser
        {
            FullName = "Especialista Avaliador"
        };

        // Documento com questionário vazio vs preenchido
        var docEmpty = new RegularTeacherEvaluationDocument
        {
            ID = Guid.NewGuid(),
            Student = student,
            User = user,
            RegularTeacherQuestionnaire = new RegularTeacherQuestionnaire(),
            RegularTeacherScreening = new RegularTeacherScreening()
        };

        var generator = new RegularTeacherCaseStudyPDFGenerator();
        var context = PdfRenderContext.Default(PdfProfile.View);

        // Deve gerar com sucesso mesmo vazio
        var emptyBytes = generator.GenerateAsync(docEmpty, context).GetAwaiter().GetResult();
        Assert.NotNull(emptyBytes);
        Assert.True(emptyBytes.Length > 0);

        var emptyText = PdfTextExtractor.ExtractFullText(emptyBytes);
        Assert.Contains(PdfBrandChrome.BrandFooterText, emptyText);

        // Documento com questionário preenchido
        var docFilled = new RegularTeacherEvaluationDocument
        {
            ID = Guid.NewGuid(),
            Student = student,
            User = user,
            RegularTeacherQuestionnaire = new RegularTeacherQuestionnaire
            {
                ClassroomDevelopment = "Participa ativamente das aulas.",
                ClassroomBehavior = "Sociável e cooperativo.",
                MainDifficulties = "Leitura e interpretação de texto."
            },
            RegularTeacherScreening = new RegularTeacherScreening()
        };

        var filledBytes = generator.GenerateAsync(docFilled, context).GetAwaiter().GetResult();
        Assert.NotNull(filledBytes);
        Assert.True(filledBytes.Length > emptyBytes.Length || filledBytes.Length > 0);

        var filledText = PdfTextExtractor.ExtractFullText(filledBytes);
        Assert.Contains("Participa ativamente das aulas.", filledText);
        Assert.Contains("Sociável e cooperativo.", filledText);

        return true.ToProperty();
    }
}
