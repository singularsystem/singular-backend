using System;
using System.IO;
using System.Threading.Tasks;
using FsCheck;
using FsCheck.Xunit;
using QuestPDF.Infrastructure;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;
using SingularSystem.Services;
using Xunit;

namespace SingularSystem.Tests;

/// <summary>
/// Feature: modulo-2-rebranding-pdfs, Property 7: Campos opcionais aparecem somente quando existem
/// Validates: Requirements 7.4, 7.5, 9.2, 11.3, 17.3
/// </summary>
public class OptionalFieldsPdfPropertyTests
{
    static OptionalFieldsPdfPropertyTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [Property(MaxTest = 50)]
    public Property RegularTeacher_Oculta_Campos_Vazios_E_Mostra_Campos_Preenchidos(bool includeSchool, bool includeMunicipality, bool includeState)
    {
        var schoolName = includeSchool ? "Escola Municipal Teste" : null;
        var city = includeMunicipality ? "Cidade Teste" : null;
        var state = includeState ? "Estado Teste" : null;

        var student = new Student
        {
            FullName = "Aluno Teste Opcional",
            DateOfBirth = new DateTime(2015, 5, 20),
            City = city,
            State = state,
            School = includeSchool ? new School { Name = schoolName! } : null
        };

        var doc = new RegularTeacherEvaluationDocument
        {
            ID = Guid.NewGuid(),
            Student = student,
            User = new ApplicationUser { FullName = "Professor Teste" },
            RegularTeacherQuestionnaire = new RegularTeacherQuestionnaire(),
            RegularTeacherScreening = new RegularTeacherScreening()
        };

        var generator = new RegularTeacherCaseStudyPDFGenerator();
        var context = PdfRenderContext.Default(PdfProfile.View);
        var bytes = generator.GenerateAsync(doc, context).GetAwaiter().GetResult();

        var extractedText = PdfTextExtractor.ExtractFullText(bytes);

        if (includeSchool)
        {
            Assert.Contains(schoolName!, extractedText);
            Assert.Contains("Escola:", extractedText);
        }
        else
        {
            Assert.DoesNotContain("Escola:", extractedText);
        }

        if (includeMunicipality)
        {
            Assert.Contains(city!, extractedText);
            Assert.Contains("Município:", extractedText);
        }
        else
        {
            Assert.DoesNotContain("Município:", extractedText);
        }

        if (includeState)
        {
            Assert.Contains(state!, extractedText);
            Assert.Contains("Estado:", extractedText);
        }
        else
        {
            Assert.DoesNotContain("Estado:", extractedText);
        }

        return true.ToProperty();
    }
}
