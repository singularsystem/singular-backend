using Amazon.S3;
using Amazon.S3.Model;
using SingularSystem.Interfaces;
using Microsoft.Extensions.Configuration;

namespace SingularSystem.Services
{
    /// <summary>
    /// Implementação de <see cref="ISignedMediaResolver"/>: concentra a geração de URL pré-assinada
    /// e verificação de referências S3 no bucket dedicado da Singular System (Req 3.1, 3.2, 3.4, 3.5).
    /// </summary>
    public sealed class SignedMediaResolver : ISignedMediaResolver
    {
        private readonly IConfiguration _configuration;

        public const double MaxPreSignedUrlMinutes = PreSignedUrlDurationHelper.MaxPreSignedUrlMinutes;

        public SignedMediaResolver(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        internal static string? ResolveSignedDocumentsBucket(IConfiguration configuration)
        {
            var dedicated = configuration["S3SignedDocumentsBucketName"]?.Trim().Trim('"', '\'', '\r', '\n', ' ');
            if (!string.IsNullOrWhiteSpace(dedicated))
                return dedicated;

            return configuration["S3BucketName"]?.Trim().Trim('"', '\'', '\r', '\n', ' ');
        }

        public bool IsZapSignUrl(string? signedFile) => false;

        public static bool IsZapSignUrlCore(string? signedFile) => false;

        public bool IsOwnS3Reference(string? signedFile)
        {
            if (string.IsNullOrWhiteSpace(signedFile))
                return false;

            var value = signedFile.Trim();

            if (Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                var host = uri.Host;
                var bucket = _configuration["S3BucketName"]?.Trim().Trim('"', '\'', '\r', '\n', ' ');
                var signedBucket = _configuration["S3SignedDocumentsBucketName"]?.Trim().Trim('"', '\'', '\r', '\n', ' ');
                var serviceUrl = _configuration["S3ServiceUrl"]?.Trim().Trim('"', '\'', '\r', '\n', ' ');

                var isAmazonS3Host =
                    host.EndsWith(".amazonaws.com", StringComparison.OrdinalIgnoreCase) &&
                    host.Contains("s3", StringComparison.OrdinalIgnoreCase);

                var isCustomServiceUrl =
                    !string.IsNullOrWhiteSpace(serviceUrl) &&
                    Uri.TryCreate(serviceUrl, UriKind.Absolute, out var sUri) &&
                    string.Equals(host, sUri.Host, StringComparison.OrdinalIgnoreCase);

                var mentionsBucket =
                    (!string.IsNullOrWhiteSpace(bucket) && host.Contains(bucket!, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(signedBucket) && host.Contains(signedBucket!, StringComparison.OrdinalIgnoreCase));

                return isAmazonS3Host || isCustomServiceUrl || mentionsBucket;
            }

            return true;
        }

        public string GeneratePreSignedUrl(string s3ObjectKey, double durationMinutes = 60)
        {
            var bucketName = ResolveSignedDocumentsBucket(_configuration);
            var effectiveMinutes = PreSignedUrlDurationHelper.Clamp(durationMinutes);

            using var s3Client = S3ClientFactory.CreateClient(_configuration);

            var request = new GetPreSignedUrlRequest
            {
                BucketName = bucketName,
                Key = s3ObjectKey,
                Expires = DateTime.UtcNow.AddMinutes(effectiveMinutes)
            };

            return s3Client.GetPreSignedURL(request);
        }
    }
}
