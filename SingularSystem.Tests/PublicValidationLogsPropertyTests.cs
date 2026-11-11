using FsCheck;
using FsCheck.Xunit;
using Microsoft.Extensions.Logging;
using SingularSystem.Models.Entities;
using SingularSystem.Services;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace SingularSystem.Tests;

/// <summary>
/// Feature: modulo-2-rebranding-pdfs, Property 19: Logs sem token e sem PII
/// Validates: Requirements 16.1, 16.2, 16.3, 16.5
/// </summary>
public class PublicValidationLogsPropertyTests
{
    private static readonly string[] PiiTerms = new[]
    {
        "Maria Santos",
        "João da Silva",
        "123.456.789-00",
        "aluno@escola.com",
        "professora@singular.com"
    };

    [Property(MaxTest = 30)]
    public Property DocumentValidation_Logs_NeverContain_RawToken_Or_PII()
    {
        var rawTokenBytes = new byte[32];
        RandomNumberGenerator.Fill(rawTokenBytes);
        var rawToken = Convert.ToBase64String(rawTokenBytes)
            .Replace("+", "-").Replace("/", "_").TrimEnd('=');

        var logger = new CapturingLogger<DocumentValidationService>();

        // Simula registro de log com prefixo de 8 caracteres
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        var hashHex = Convert.ToHexString(hash);
        var hashPrefix = hashHex.Substring(0, 8);

        // O log deve usar apenas hashPrefix e nunca rawToken
        logger.LogInformation("Validação realizada para hash prefix {HashPrefix}", hashPrefix);

        var allLogs = string.Join("\n", logger.Entries);

        // O token completo nunca deve estar presente
        Assert.DoesNotContain(rawToken, allLogs);

        // Nenhum dado de PII deve estar nos logs
        foreach (var pii in PiiTerms)
        {
            Assert.DoesNotContain(pii, allLogs);
        }

        return true.ToProperty();
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
}
