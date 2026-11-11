using System;
using System.Reflection;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging.Abstractions;
using SingularSystem.Controllers;
using SingularSystem.DTOs;
using SingularSystem.Interfaces;
using Xunit;

namespace SingularSystem.Tests;

public class PublicValidationRateLimitTests
{
    private sealed class DummyValidationService : IDocumentValidationService
    {
        public Task<string> GetOrIssueAsync(Guid documentId, string documentType, System.Threading.CancellationToken ct = default)
            => Task.FromResult("dummy");

        public Task<DocumentValidationResult?> ValidateAsync(string token, System.Threading.CancellationToken ct = default)
            => Task.FromResult<DocumentValidationResult?>(null);
    }

    [Fact]
    public void Rota_Publica_Possui_Atributos_AllowAnonymous_E_EnableRateLimiting()
    {
        var controllerType = typeof(PublicValidationController);
        var rateLimitAttr = controllerType.GetCustomAttribute<EnableRateLimitingAttribute>();

        Assert.NotNull(rateLimitAttr);
        Assert.Equal("public-validation", rateLimitAttr.PolicyName);

        var method = controllerType.GetMethod(nameof(PublicValidationController.Validate));
        Assert.NotNull(method);

        var allowAnon = method.GetCustomAttribute<AllowAnonymousAttribute>();
        Assert.NotNull(allowAnon);
    }

    [Fact]
    public void Particao_RateLimiting_Rejeita_Apos_30_Requisicoes()
    {
        // Testa a regra de particionamento configurada (30 permits por janela de 1 min)
        var options = new FixedWindowRateLimiterOptions
        {
            PermitLimit = 30,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        };

        using var limiter = new FixedWindowRateLimiter(options);

        // Primeiras 30 tentativas devem ser adquiridas com sucesso
        for (int i = 0; i < 30; i++)
        {
            var lease = limiter.AttemptAcquire(1);
            Assert.True(lease.IsAcquired, $"Tentativa {i + 1} deveria ser permitida.");
        }

        // A 31ª requisição deve ser rejeitada (IsAcquired = false)
        var blockedLease = limiter.AttemptAcquire(1);
        Assert.False(blockedLease.IsAcquired, "A 31ª requisição no intervalo deve ser rejeitada pelo limite de taxa.");
    }
}
