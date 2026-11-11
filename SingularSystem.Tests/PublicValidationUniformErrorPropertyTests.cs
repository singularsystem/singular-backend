using System;
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
/// Feature: modulo-2-rebranding-pdfs, Property 18: Token inválido tem resposta uniforme
/// Validates: Requirements 12.8
/// </summary>
public class PublicValidationUniformErrorPropertyTests
{
    private sealed class NotFoundValidationService : IDocumentValidationService
    {
        public Task<string> GetOrIssueAsync(Guid documentId, string documentType, System.Threading.CancellationToken ct = default)
            => Task.FromResult("fake-token");

        public Task<DocumentValidationResult?> ValidateAsync(string token, System.Threading.CancellationToken ct = default)
            => Task.FromResult<DocumentValidationResult?>(null);
    }

    [Property(MaxTest = 100)]
    public Property Qualquer_Token_Invalido_Retorna_Status_404_E_Corpo_Identico(string? invalidToken)
    {
        var service = new NotFoundValidationService();
        var controller = new PublicValidationController(service, NullLogger<PublicValidationController>.Instance);

        var actionResult = controller.Validate(new PublicValidateDocumentRequest { Token = invalidToken ?? "" }).GetAwaiter().GetResult();
        var notFoundResult = actionResult as NotFoundObjectResult;

        Assert.NotNull(notFoundResult);
        Assert.Equal(404, notFoundResult.StatusCode);

        var body = notFoundResult.Value as PublicValidationResponse;
        Assert.NotNull(body);
        Assert.False(body.Valid);
        Assert.Null(body.DocumentType);
        Assert.Null(body.IssuedAt);
        Assert.Empty(body.Signatures);

        var json = JsonSerializer.Serialize(body);
        return (json == "{\"Valid\":false,\"DocumentType\":null,\"IssuedAt\":null,\"Signatures\":[]}")
            .Label("O corpo de erro retornado deve ser estritamente idêntico para qualquer token inválido.");
    }
}
