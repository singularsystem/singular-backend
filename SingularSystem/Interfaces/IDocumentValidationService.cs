using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SingularSystem.Interfaces;

public record DocumentValidationSignature(string Role, bool Signed);

public record DocumentValidationResult(
    bool Valid,
    string DocumentType,
    DateTime IssuedAt,
    IReadOnlyList<DocumentValidationSignature> Signatures
);

public interface IDocumentValidationService
{
    Task<string> GetOrIssueAsync(Guid documentId, string documentType, CancellationToken ct = default);
    Task<DocumentValidationResult?> ValidateAsync(string token, CancellationToken ct = default);
}
