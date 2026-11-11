using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using SingularSystem.Models.Entities;
using SingularSystem.Services;
using Xunit;

namespace SingularSystem.Tests;

/// <summary>
/// Feature: modulo-2-rebranding-pdfs, Property 16: Token opaco, emissão idempotente e round-trip de validação
/// Validates: Requirements 12.5, 12.6, 17.6
/// </summary>
public class DocumentValidationPropertyTests
{
    private sealed class EphemeralDataProtectionProvider : IDataProtectionProvider
    {
        private readonly EphemeralProtector _protector = new();
        public IDataProtector CreateProtector(string purpose) => _protector;

        private sealed class EphemeralProtector : IDataProtector
        {
            public IDataProtector CreateProtector(string purpose) => this;
            public byte[] Protect(byte[] plaintext) => plaintext;
            public byte[] Unprotect(byte[] protectedData) => protectedData;
        }
    }

    private static AppDbContext CreateInMemoryDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new AppDbContext(options);
    }

    [Property(MaxTest = 100)]
    public Property Token_Possui_43_Caracteres_Base64Url_E_Sao_Distintos(PositiveInt count)
    {
        int n = Math.Clamp(count.Get % 20 + 2, 2, 20);
        using var db = CreateInMemoryDb(Guid.NewGuid().ToString());
        var service = new DocumentValidationService(db, new EphemeralDataProtectionProvider());

        var tokens = new HashSet<string>();
        for (int i = 0; i < n; i++)
        {
            var docId = Guid.NewGuid();
            var token = service.GetOrIssueAsync(docId, "Report").GetAwaiter().GetResult();

            Assert.Equal(43, token.Length);
            // Verifica que não contém caracteres de base64 padrão '+', '/' ou '='
            Assert.DoesNotContain("+", token);
            Assert.DoesNotContain("/", token);
            Assert.DoesNotContain("=", token);

            // Verifica que não contém o guid do documento no token
            Assert.DoesNotContain(docId.ToString(), token, StringComparison.OrdinalIgnoreCase);

            tokens.Add(token);
        }

        return (tokens.Count == n).Label("Todos os tokens gerados para documentos distintos devem ser únicos.");
    }

    [Fact]
    public async Task Emissao_Eh_Idempotente_Para_Mesmo_Documento()
    {
        using var db = CreateInMemoryDb(Guid.NewGuid().ToString());
        var service = new DocumentValidationService(db, new EphemeralDataProtectionProvider());

        var docId = Guid.NewGuid();
        var token1 = await service.GetOrIssueAsync(docId, "Report");
        var token2 = await service.GetOrIssueAsync(docId, "Report");

        Assert.Equal(token1, token2);
    }

    [Fact]
    public async Task RoundTrip_Valida_Documento_Por_Token()
    {
        using var db = CreateInMemoryDb(Guid.NewGuid().ToString());
        var service = new DocumentValidationService(db, new EphemeralDataProtectionProvider());

        var docId = Guid.NewGuid();
        var issuedToken = await service.GetOrIssueAsync(docId, "PAEE");

        var result = await service.ValidateAsync(issuedToken);

        Assert.NotNull(result);
        Assert.True(result.Valid);
        Assert.Equal("PAEE", result.DocumentType);
    }
}
