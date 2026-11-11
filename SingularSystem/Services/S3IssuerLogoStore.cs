using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SingularSystem.Services;

public class S3IssuerLogoStore : IIssuerLogoStore
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<S3IssuerLogoStore> _logger;
    private readonly string _bucketName;

    public S3IssuerLogoStore(IConfiguration configuration, ILogger<S3IssuerLogoStore> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _bucketName = configuration["S3BucketName"] ?? "singular-system";
    }

    public async Task<string> SaveAsync(Guid issuerProfileId, ValidatedLogo logo, CancellationToken ct = default)
    {
        var ext = logo.ContentType.Equals("image/png", StringComparison.OrdinalIgnoreCase) ? "png" : "jpg";
        var objectKey = $"issuer-logos/{issuerProfileId}/{Guid.NewGuid():N}.{ext}";

        using var client = S3ClientFactory.CreateClient(_configuration);
        using var stream = new MemoryStream(logo.Data);

        var putRequest = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = objectKey,
            InputStream = stream,
            ContentType = logo.ContentType
        };

        await client.PutObjectAsync(putRequest, ct);
        return objectKey;
    }

    public async Task<byte[]?> TryReadAsync(string objectKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
        {
            return null;
        }

        try
        {
            using var client = S3ClientFactory.CreateClient(_configuration);
            var getRequest = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = objectKey
            };

            using var response = await client.GetObjectAsync(getRequest, ct);
            using var ms = new MemoryStream();
            await response.ResponseStream.CopyToAsync(ms, ct);
            return ms.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Falha ao ler logotipo no S3 com chave {ObjectKey}: {Message}", objectKey, ex.Message);
            return null;
        }
    }

    public string GetPreviewUrl(string objectKey, TimeSpan ttl)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
        {
            return string.Empty;
        }

        using var client = S3ClientFactory.CreateClient(_configuration);
        var clampedMinutes = PreSignedUrlDurationHelper.Clamp(ttl.TotalMinutes);

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = objectKey,
            Expires = DateTime.UtcNow.AddMinutes(clampedMinutes)
        };

        return client.GetPreSignedURL(request);
    }
}
