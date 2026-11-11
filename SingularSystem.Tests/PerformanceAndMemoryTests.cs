using QuestPDF.Infrastructure;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;

namespace SingularSystem.Tests;

/// <summary>
/// Feature: modulo-2-rebranding-pdfs
/// Tasks 10.9 & 10.10: Benchmark de desempenho e teste de memória
/// Validates: Requirements 14.1, 14.2, 14.3, 17.8, 18.4
/// </summary>
public class PerformanceAndMemoryTests
{
    static PerformanceAndMemoryTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private static RegularTeacherEvaluationDocument CreateSyntheticDocument()
    {
        var student = new Student
        {
            FullName = "Aluno Teste Benchmark Performance",
            DateOfBirth = new DateTime(2013, 10, 5),
            School = new School { Name = "Escola Municipal de Teste de Performance" }
        };

        var doc = new RegularTeacherEvaluationDocument
        {
            ID = Guid.NewGuid(),
            Student = student,
            User = new ApplicationUser { FullName = "Professora Especialista Benchmark" },
            RegularTeacherQuestionnaire = new RegularTeacherQuestionnaire
            {
                ClassroomDevelopment = string.Join(" ", Enumerable.Repeat("Desenvolvimento detalhado com metas claras e avaliações contínuas.", 30)),
                ClassroomBehavior = string.Join(" ", Enumerable.Repeat("Comportamento cooperativo com colegas e professores em sala de aula.", 30)),
                MainDifficulties = string.Join(" ", Enumerable.Repeat("Acompanhamento focado em atividades pedagógicas adaptadas.", 30)),
                LearningCharacteristics = string.Join(" ", Enumerable.Repeat("Aprendizagem com suporte visual e recursos multissensoriais.", 30))
            },
            RegularTeacherScreening = new RegularTeacherScreening()
        };

        return doc;
    }

    [Fact]
    public void PerformanceTests_SyntheticReport_MedianCompilationUnder4Seconds()
    {
        // Requirements: 14.1, 17.8, 18.4
        // 3 execuções de aquecimento e mediana de 5 compilações abaixo de 4,0 s; registrar a primeira fria sem incluí-la
        var generator = new RegularTeacherCaseStudyPDFGenerator();
        var context = PdfRenderContext.Default(PdfProfile.View);
        var doc = CreateSyntheticDocument();

        // 1 execução fria
        var swCold = Stopwatch.StartNew();
        generator.GenerateAsync(doc, context).GetAwaiter().GetResult();
        swCold.Stop();

        // 3 aquecimentos
        for (int i = 0; i < 3; i++)
        {
            generator.GenerateAsync(doc, context).GetAwaiter().GetResult();
        }

        // 5 compilações medidas
        var timings = new List<double>();
        for (int i = 0; i < 5; i++)
        {
            var sw = Stopwatch.StartNew();
            generator.GenerateAsync(doc, context).GetAwaiter().GetResult();
            sw.Stop();
            timings.Add(sw.Elapsed.TotalSeconds);
        }

        timings.Sort();
        var median = timings[2]; // mediana dos 5 valores

        Assert.True(median < 4.0, $"Median generation time was {median:F3}s, exceeding the 4.0s limit.");
    }

    [Fact]
    public void MemoryTests_100ConsecutivePdfs_MemoryGrowthUnder50MB()
    {
        // Requirements: 14.2, 14.3
        // Gerar 100 PDFs em sequência, forçar GC.Collect() e exigir crescimento de memória abaixo de 50 MB
        var generator = new RegularTeacherCaseStudyPDFGenerator();
        var context = PdfRenderContext.Default(PdfProfile.View);
        var doc = CreateSyntheticDocument();

        // Limpeza inicial
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        long initialMemory = GC.GetTotalMemory(true);

        for (int i = 0; i < 100; i++)
        {
            var bytes = generator.GenerateAsync(doc, context).GetAwaiter().GetResult();
            Assert.NotEmpty(bytes);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        long finalMemory = GC.GetTotalMemory(true);

        long growthBytes = Math.Max(0, finalMemory - initialMemory);
        double growthMb = growthBytes / (1024.0 * 1024.0);

        Assert.True(growthMb < 50.0, $"Memory growth after 100 PDFs was {growthMb:F2} MB, exceeding the 50 MB limit.");
    }
}
