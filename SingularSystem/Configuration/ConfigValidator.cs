using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace SingularSystem.Configuration
{
    public sealed class ConfigValidationResult
    {
        public bool IsValid => MissingOrInvalidKeys.Count == 0;
        public List<string> MissingOrInvalidKeys { get; } = new();
        public List<string> ErrorMessages { get; } = new();

        public void AddError(string key, string message)
        {
            MissingOrInvalidKeys.Add(key);
            ErrorMessages.Add($"[{key}] {message}");
        }
    }

    /// <summary>
    /// Validador fail-fast consolidado de inicialização (Req 5.1 - 5.8, Property 5).
    /// Valida todas as variáveis obrigatórias antes do servidor HTTP começar a escutar.
    /// Em caso de falha, registra no Console.Error e encerra o processo com Environment.Exit(1).
    /// </summary>
    public static class ConfigValidator
    {
        public const int FailureExitCode = 1;

        public static ConfigValidationResult Validate(IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            var result = new ConfigValidationResult();

            // 1. ConnectionStrings:DefaultConnection (Req 5.1)
            ValidateRequired(configuration, "ConnectionStrings:DefaultConnection", result);

            // 2. JWTSecret (Req 5.2)
            ValidateRequired(configuration, "JWTSecret", result);

            // 3. Chaves S3 (Req 5.3)
            ValidateRequired(configuration, "S3BucketAccessKey", result);
            ValidateRequired(configuration, "S3BucketSecretKey", result);
            ValidateRequired(configuration, "S3BucketName", result);
            ValidateRequired(configuration, "S3SignedDocumentsBucketName", result);

            var s3ServiceUrl = configuration["S3ServiceUrl"];
            if (!string.IsNullOrWhiteSpace(s3ServiceUrl) && !Uri.TryCreate(s3ServiceUrl, UriKind.Absolute, out _))
            {
                result.AddError("S3ServiceUrl", "Não é uma URL absoluta válida.");
            }

            // 4. NeckSign (Req 5.4)
            ValidateAbsoluteUrl(configuration, "NeckSign:BaseUrl", result);
            ValidateRequired(configuration, "NeckSign:ApiKey", result);
            ValidateRequired(configuration, "NeckSign:WebhookSecret", result);
            ValidateAbsoluteUrl(configuration, "NeckSign:WebhookUrl", result);

            // 5. SMTP (Req 5.5)
            ValidateRequired(configuration, "Smtp:Host", result);
            ValidatePort(configuration, "Smtp:Port", result);
            ValidateRequired(configuration, "Smtp:Username", result);
            ValidateRequired(configuration, "Smtp:Password", result);
            ValidateRequired(configuration, "Smtp:FromAddress", result);

            // 6. Integração (Req 4.6, 4.7)
            ValidateRequired(configuration, "IntegrationSettings:ApiKey", result);

            // 7. Legal (Req 4.1, 4.5)
            var termVersion = configuration["Legal:TermVersion"];
            if (!string.IsNullOrWhiteSpace(termVersion) && termVersion != "1" && termVersion != "2")
            {
                result.AddError("Legal:TermVersion", "Valor inválido. Os valores aceitos são '1' ou '2'.");
            }

            // 8. Issuer Logo limits (Req 8.6, 14.1)
            var maxBytesStr = configuration["Issuer:LogoMaxBytes"];
            if (!string.IsNullOrWhiteSpace(maxBytesStr) && (!long.TryParse(maxBytesStr, out var maxBytes) || maxBytes <= 0))
            {
                result.AddError("Issuer:LogoMaxBytes", "Deve ser um número inteiro positivo representando bytes.");
            }

            var minWidthStr = configuration["Issuer:LogoMinWidthPx"];
            if (!string.IsNullOrWhiteSpace(minWidthStr) && (!int.TryParse(minWidthStr, out var minWidth) || minWidth <= 0))
            {
                result.AddError("Issuer:LogoMinWidthPx", "Deve ser um número inteiro positivo representando pixels de largura.");
            }

            // 9. Validação Pública (Req 12.6, 12.7, 12.8, 12.9)
            ValidateAbsoluteUrl(configuration, "PublicValidation:BaseUrl", result);

            return result;
        }

        public static void ValidateOrExit(IConfiguration configuration)
        {
            var result = Validate(configuration);

            if (!result.IsValid)
            {
                foreach (var err in result.ErrorMessages)
                {
                    Console.Error.WriteLine($"[FATAL_CONFIG] {err}");
                }

                Console.Error.WriteLine(
                    $"[FATAL] Inicialização abortada devido a {result.MissingOrInvalidKeys.Count} erro(s) de configuração. " +
                    $"Variáveis afetadas: {string.Join(", ", result.MissingOrInvalidKeys)}");

                Environment.Exit(FailureExitCode);
            }
        }

        private static void ValidateRequired(IConfiguration config, string key, ConfigValidationResult result)
        {
            var val = config[key];
            if (string.IsNullOrWhiteSpace(val))
            {
                result.AddError(key, "Variável ausente ou vazia.");
            }
        }

        private static void ValidateAbsoluteUrl(IConfiguration config, string key, ConfigValidationResult result)
        {
            var val = config[key];
            if (string.IsNullOrWhiteSpace(val))
            {
                result.AddError(key, "Variável ausente ou vazia.");
            }
            else if (!Uri.TryCreate(val, UriKind.Absolute, out _))
            {
                result.AddError(key, $"Valor \"{val}\" não é uma URL absoluta válida.");
            }
        }

        private static void ValidatePort(IConfiguration config, string key, ConfigValidationResult result)
        {
            var val = config[key];
            if (string.IsNullOrWhiteSpace(val))
            {
                result.AddError(key, "Variável ausente ou vazia.");
            }
            else if (!int.TryParse(val, out var port) || port < 1 || port > 65535)
            {
                result.AddError(key, $"Porta \"{val}\" inválida (deve ser um inteiro entre 1 e 65535).");
            }
        }
    }
}
