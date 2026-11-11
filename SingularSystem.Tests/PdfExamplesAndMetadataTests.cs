using Microsoft.AspNetCore.Mvc;
using QuestPDF.Infrastructure;
using SingularSystem.Controllers;
using SingularSystem.DTOs.Tracking;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UglyToad.PdfPig;
using Xunit;

namespace SingularSystem.Tests;

/// <summary>
/// Feature: modulo-2-rebranding-pdfs, Task 10.12: Escrever testes de exemplo dos PDFs
/// Validates: Requirements 5.5, 6.5, 4.2, 17.3
/// </summary>
public class PdfExamplesAndMetadataTests
{
    static PdfExamplesAndMetadataTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [Fact]
    public void BiopsychosocialTracking_MaintainsExpectedColumns()
    {
        // Req 4.2 / 17.3: Trackers mantêm as mesmas colunas
        var rows = new List<BiopsychosocialTrackingPDFRow>
        {
            new()
            {
                StudentName = "Aluno Exemplo Tracker",
                SchoolName = "Escola Municipal Singular",
                Status = "Completo",
                PendingProfessionals = "-"
            }
        };

        var bytes = BiopsychosocialTrackingPDFGenerator.Generate(rows);
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);

        var fullText = PdfTextExtractor.ExtractFullText(bytes);

        // Deve conter os cabeçalhos de coluna esperados
        Assert.Contains("Aluno", fullText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Escola", fullText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Status Geral", fullText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Profissionais Pendentes", fullText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Aluno Exemplo Tracker", fullText);
    }

    [Fact]
    public void CaseStudiesTracking_MaintainsExpectedColumns()
    {
        var rows = new List<CaseStudiesTrackingPDFRow>
        {
            new()
            {
                StudentName = "Aluno Exemplo Caso",
                SchoolName = "Escola Exemplo",
                Status = "Em andamento"
            }
        };

        var bytes = CaseStudiesTrackingPDFGenerator.Generate(rows);
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);

        var fullText = PdfTextExtractor.ExtractFullText(bytes);
        Assert.Contains("Aluno", fullText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Escola", fullText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Status", fullText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Aluno Exemplo Caso", fullText);
    }

    [Fact]
    public async Task CaseStudyGenerator_ExecutesWithoutException_WithAndWithoutSchoolAndCity()
    {
        // Req 17.3: Geração sem exceção com e sem escola e município
        var generator = new RegularTeacherCaseStudyPDFGenerator();
        var context = PdfRenderContext.Default(PdfProfile.View);

        // 1. Sem escola nem município
        var docNoSchool = new RegularTeacherEvaluationDocument
        {
            ID = Guid.NewGuid(),
            Student = new Student
            {
                FullName = "Aluno Sem Escola",
                DateOfBirth = new DateTime(2015, 1, 1),
                School = null
            },
            User = new ApplicationUser { FullName = "Profissional AEE" },
            RegularTeacherQuestionnaire = new RegularTeacherQuestionnaire(),
            RegularTeacherScreening = new RegularTeacherScreening()
        };

        var bytes1 = await generator.GenerateAsync(docNoSchool, context);
        Assert.NotNull(bytes1);
        Assert.True(bytes1.Length > 0);

        // 2. Com escola e com município
        var docWithSchool = new RegularTeacherEvaluationDocument
        {
            ID = Guid.NewGuid(),
            Student = new Student
            {
                FullName = "Aluno Com Escola",
                DateOfBirth = new DateTime(2015, 1, 1),
                School = new School { Name = "Escola Municipal Darcy Ribeiro" }
            },
            User = new ApplicationUser { FullName = "Profissional AEE" },
            RegularTeacherQuestionnaire = new RegularTeacherQuestionnaire(),
            RegularTeacherScreening = new RegularTeacherScreening()
        };

        var bytes2 = await generator.GenerateAsync(docWithSchool, context);
        Assert.NotNull(bytes2);
        Assert.True(bytes2.Length > 0);
    }

    [Fact]
    public void GeneratedPdf_CanBeOpenedByPdfPig_AndContainsText()
    {
        var doc = new RegularTeacherEvaluationDocument
        {
            ID = Guid.NewGuid(),
            Student = new Student
            {
                FullName = "Aluno Fontes",
                DateOfBirth = new DateTime(2015, 1, 1)
            },
            User = new ApplicationUser { FullName = "Profissional Singular" },
            RegularTeacherQuestionnaire = new RegularTeacherQuestionnaire
            {
                ClassroomDevelopment = "Texto padrão para teste de fontes."
            },
            RegularTeacherScreening = new RegularTeacherScreening()
        };

        var generator = new RegularTeacherCaseStudyPDFGenerator();
        var context = PdfRenderContext.Default(PdfProfile.View);
        var bytes = generator.GenerateAsync(doc, context).GetAwaiter().GetResult();

        using var ms = new MemoryStream(bytes);
        using var pdf = PdfDocument.Open(ms);
        Assert.True(pdf.NumberOfPages >= 1);

        var firstPage = pdf.GetPage(1);
        Assert.NotNull(firstPage);
        Assert.NotEmpty(firstPage.GetWords());
    }
}
