using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SingularSystem.Controllers;
using SingularSystem.Interfaces;
using SingularSystem.Models.Entities;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace SingularSystem.Tests;

/// <summary>
/// Feature: modulo-2-rebranding-pdfs, Task 10.11: Garantir liberação de recursos e tratamento de exceção na geração
/// Validates: Requirements 14.2, 14.6, 16.1
/// </summary>
public class PdfResourceReleaseAndExceptionTests
{
    private sealed class FaultyPdfService : IPDFService
    {
        public Task<SingularSystem.Models.DocumentPdfResult> GeneratePdfForUser(Guid id, Models.PdfProfile profile, Models.Entities.ApplicationUser user, IList<string> roles) => throw new InvalidOperationException("Falha simulada na renderização de PDF");
        public Task<byte[]> GeneratePdf(Guid id) => throw new InvalidOperationException("Falha simulada na renderização de PDF");
        public Task<byte[]> GeneratePdf(Guid id, Models.PdfProfile profile) => throw new InvalidOperationException("Falha simulada na renderização de PDF");
        public Task<string?> GetDocumentType(Guid id) => Task.FromResult<string?>("pei");
        public Task<byte[]> GenerateStudentsPdf(string state, string city, string schoolId, string serie) => throw new NotImplementedException();
        public Task<byte[]> GenerateReportsAEEPdf(string state, string city, string schoolId, string disability, string disorder) => throw new NotImplementedException();
        public Task<byte[]> GenerateCaseStudiesTrackingPdf(string state, string city, string schoolId, string searchTerm, string status, int? year, int? month) => throw new NotImplementedException();
        public Task<byte[]> GenerateBiopsychosocialTrackingPdf(string state, string city, string schoolId, string searchTerm, string status, string pendingProfessional, int? year, int? month) => throw new NotImplementedException();
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> Entries { get; } = new();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add(formatter(state, exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }

    [Fact]
    public async Task ViewDocumentPdf_WhenExceptionThrown_Returns500AndLogsWithoutPii()
    {
        var logger = new CapturingLogger<PDFController>();
        var faultyService = new FaultyPdfService();
        var controller = new PDFController(logger, null!, null!, faultyService, null!);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var docId = Guid.NewGuid();
        var result = await controller.ViewDocumentPdf(docId);

        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);

        // Verifica logs
        Assert.NotEmpty(logger.Entries);
        var loggedMessage = string.Join("\n", logger.Entries);

        // O ID do documento é logado, mas nenhum PII é logado
        Assert.Contains(docId.ToString(), loggedMessage);
        Assert.DoesNotContain("CPF", loggedMessage, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Aluno", loggedMessage, StringComparison.OrdinalIgnoreCase);
    }
}
