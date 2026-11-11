using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using SingularSystem.Interfaces;
using SingularSystem.Models.Entities;

namespace SingularSystem.Services;

public class DocumentValidationService : IDocumentValidationService
{
    private readonly AppDbContext _db;
    private readonly IDataProtector _protector;

    public DocumentValidationService(AppDbContext db, IDataProtectionProvider provider)
    {
        _db = db;
        _protector = provider.CreateProtector("SingularSystem.DocumentValidation.Token.v1");
    }

    public async Task<string> GetOrIssueAsync(Guid documentId, string documentType, CancellationToken ct = default)
    {
        var existing = await _db.DocumentValidations
            .FirstOrDefaultAsync(x => x.DocumentId == documentId, ct);

        if (existing != null)
        {
            try
            {
                return _protector.Unprotect(existing.TokenProtected);
            }
            catch (CryptographicException)
            {
                // Se a chave mudou ou falhou unprotect, regera token preservando o hash anterior se possível ou atualizando
            }
        }

        // Token opaco com 32 bytes de RandomNumberGenerator em base64url (43 caracteres)
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        var token = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(bytes);

        var tokenHash = ComputeSha256(token);
        var tokenProtected = _protector.Protect(token);

        if (existing != null)
        {
            existing.TokenHash = tokenHash;
            existing.TokenProtected = tokenProtected;
            existing.DocumentType = documentType;
            existing.IssuedAtUtc = DateTime.UtcNow;
        }
        else
        {
            var record = new DocumentValidation
            {
                Id = Guid.NewGuid(),
                DocumentId = documentId,
                DocumentType = documentType,
                TokenHash = tokenHash,
                TokenProtected = tokenProtected,
                IssuedAtUtc = DateTime.UtcNow
            };
            _db.DocumentValidations.Add(record);
        }

        await _db.SaveChangesAsync(ct);
        return token;
    }

    public async Task<DocumentValidationResult?> ValidateAsync(string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var tokenHash = ComputeSha256(token.Trim());

        var validation = await _db.DocumentValidations
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, ct);

        if (validation == null)
            return null;

        var signatures = new List<DocumentValidationSignature>();

        // Busca dados de assinatura com base no tipo de documento
        if (string.Equals(validation.DocumentType, "Report", StringComparison.OrdinalIgnoreCase))
        {
            var report = await _db.Reports
                .AsNoTracking()
                .Include(r => r.SocialWorkerReport).ThenInclude(sw => sw.User)
                .Include(r => r.PsychologistReport).ThenInclude(p => p.User)
                .Include(r => r.PsychopedagogueReport).ThenInclude(pp => pp.User)
                .FirstOrDefaultAsync(r => r.ID == validation.DocumentId, ct);

            if (report != null)
            {
                signatures.Add(new DocumentValidationSignature("Assistente Social", report.SocialWorkerSign));
                signatures.Add(new DocumentValidationSignature("Psicólogo(a)", report.PsychologistSign));
                signatures.Add(new DocumentValidationSignature("Psicopedagogo(a)", report.PsychopedagogueSign));
            }
        }

        return new DocumentValidationResult(
            Valid: true,
            DocumentType: validation.DocumentType,
            IssuedAt: validation.IssuedAtUtc,
            Signatures: signatures
        );
    }

    private static byte[] ComputeSha256(string input)
    {
        return SHA256.HashData(Encoding.UTF8.GetBytes(input));
    }
}
