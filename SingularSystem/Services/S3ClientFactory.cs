using Amazon;
using Amazon.S3;
using Microsoft.Extensions.Configuration;

namespace SingularSystem.Services
{
    /// <summary>
    /// Fábrica para instanciar clientes AmazonS3Client com suporte opcional a S3ServiceUrl e S3Region,
    /// permitindo compatibilidade com Cloudflare R2 e MinIO sem alteração de código (Req 3.1, 3.4, 3.5).
    /// </summary>
    public static class S3ClientFactory
    {
        public static AmazonS3Client CreateClient(IConfiguration configuration)
        {
            var accessKey = configuration["S3BucketAccessKey"]?.Trim().Trim('"', '\'', '\r', '\n', ' ');
            var secretKey = configuration["S3BucketSecretKey"]?.Trim().Trim('"', '\'', '\r', '\n', ' ');
            var serviceUrl = configuration["S3ServiceUrl"]?.Trim().Trim('"', '\'', '\r', '\n', ' ');
            var regionName = configuration["S3Region"]?.Trim().Trim('"', '\'', '\r', '\n', ' ');

            var clientConfig = new AmazonS3Config();

            if (!string.IsNullOrWhiteSpace(serviceUrl))
            {
                clientConfig.ServiceURL = serviceUrl;
                clientConfig.ForcePathStyle = true;
            }
            else
            {
                var region = !string.IsNullOrWhiteSpace(regionName)
                    ? RegionEndpoint.GetBySystemName(regionName)
                    : RegionEndpoint.USEast2;
                clientConfig.RegionEndpoint = region;
            }

            return new AmazonS3Client(accessKey, secretKey, clientConfig);
        }
    }
}
