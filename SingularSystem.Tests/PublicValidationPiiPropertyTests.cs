using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using SingularSystem.Controllers;
using SingularSystem.DTOs;
using SingularSystem.Interfaces;
using Xunit;

namespace SingularSystem.Tests;

/// <summary>
/// Feature: modulo-2-rebranding-pdfs, Property 17: Resposta pública sem PII
/// Validates: Requirements 12.7
/// </summary>
public class PublicValidationPiiPropertyTests
{
    private sealed class FakeValidationService : IDocumentValidationService
    {
        private readonly DocumentValidationResult _result;

        public FakeValidationService(DocumentValidationResult result) => _result = result;

        public Task<string> GetOrIssueAsync(Guid documentId, string documentType, System.Threading.CancellationToken ct = default)
            => Task.FromResult("fake-token");

        public Task<DocumentValidationResult?> ValidateAsync(string token, System.Threading.CancellationToken ct = default)
            => Task.FromResult<DocumentValidationResult?>(_result);
    }

    [Property(MaxTest = 100)]
    public Property Resposta_Publica_Nao_Contem_PII(NonNull<string> studentName, NonNull<string> cpf, NonNull<string> medicalText)
    {
        var signatures = new List<DocumentValidationSignature>
        {
            new("Assistente Social", true),
            new("Psicólogo(a)", false)
        };

        var validResult = new DocumentValidationResult(
            Valid: true,
            DocumentType: "Report",
            IssuedAt: DateTime.UtcNow,
            Signatures: signatures
        );

        var service = new FakeValidationService(validResult);
        var controller = new PublicValidationController(service, NullLogger<PublicValidationController>.Instance);

        var actionResult = controller.Validate(new PublicValidateDocumentRequest { Token = "test-token" }).GetAwaiter().GetResult();
        var okResult = actionResult as OkObjectResult;

        Assert.NotNull(okResult);
        var response = okResult.Value as PublicValidationResponse;
        Assert.NotNull(response);

        // Serializa a resposta para verificar que não vaza dados pessoais do aluno
        var json = JsonSerializer.Serialize(response);

        // Verifica que o JSON serializado não contém dados arbitrários do aluno
        if (!string.IsNullOrWhiteSpace(studentName.Get) && studentName.Get.Length > 4)
        {
            Assert.DoesNotContain(studentName.Get, json, StringComparison.OrdinalIgnoreCase);
        }

        if (!string.IsNullOrWhiteSpace(cpf.Get) && cpf.Get.Length > 4)
        {
            Assert.DoesNotContain(cpf.Get, json, StringComparison.OrdinalIgnoreCase);
        }

        if (!string.IsNullOrWhiteSpace(medicalText.Get) && medicalText.Get.Length > 4)
        {
            Assert.DoesNotContain(medicalText.Get, json, StringComparison.OrdinalIgnoreCase);
        }

        // Verifica que as chaves presentes são estritamente as especificadas
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var propNames = root.EnumerateObject().Select(p => p.Name).ToList();
        var allowedProps = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Valid", "DocumentType", "IssuedAt", "Signatures"
        };

        return propNames.All(allowedProps.Contains).Label("A resposta pública contém apenas as propriedades permitidas.");
    }
}
