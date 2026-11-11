using System;
using System.Threading;
using System.Threading.Tasks;

namespace SingularSystem.Services;

public interface IIssuerLogoStore
{
    Task<string> SaveAsync(Guid issuerProfileId, ValidatedLogo logo, CancellationToken ct = default);
    Task<byte[]?> TryReadAsync(string objectKey, CancellationToken ct = default);
    string GetPreviewUrl(string objectKey, TimeSpan ttl);
}
