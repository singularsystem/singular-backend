using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.Extensions.Configuration;
using SingularSystem.Configuration;
using Xunit;

namespace SingularSystem.Tests
{
    public sealed class ConfigValidatorPropertyTests
    {
        private static readonly string[] RequiredKeys = new[]
        {
            "ConnectionStrings:DefaultConnection",
            "JWTSecret",
            "S3BucketAccessKey",
            "S3BucketSecretKey",
            "S3BucketName",
            "S3SignedDocumentsBucketName",
            "NeckSign:BaseUrl",
            "NeckSign:ApiKey",
            "NeckSign:WebhookSecret",
            "NeckSign:WebhookUrl",
            "Smtp:Host",
            "Smtp:Port",
            "Smtp:Username",
            "Smtp:Password",
            "Smtp:FromAddress"
        };

        private static Dictionary<string, string?> CreateValidConfigDict()
        {
            return new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=singular_db;Username=postgres;Password=pass",
                ["JWTSecret"] = "this_is_a_very_secure_jwt_secret_key_32bytes_long",
                ["S3BucketAccessKey"] = "AKIAEXAMPLEKEY12345",
                ["S3BucketSecretKey"] = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
                ["S3BucketName"] = "singular-app-bucket",
                ["S3SignedDocumentsBucketName"] = "singular-signed-bucket",
                ["S3ServiceUrl"] = "https://s3.us-east-2.amazonaws.com",
                ["NeckSign:BaseUrl"] = "https://api.necksign.com",
                ["NeckSign:ApiKey"] = "necksign_api_key_1234567890",
                ["NeckSign:WebhookSecret"] = "whsec_abcdef123456",
                ["NeckSign:WebhookUrl"] = "https://app.singularsystem.com.br/api/webhooks/necksign",
                ["Smtp:Host"] = "smtp.sendgrid.net",
                ["Smtp:Port"] = "587",
                ["Smtp:Username"] = "smtp-user",
                ["Smtp:Password"] = "smtp-secret-pass",
                ["Smtp:FromAddress"] = "noreply@singularsystem.com.br"
            };
        }

        public enum CorruptionKind
        {
            None,
            OmitKey,
            EmptyString,
            WhitespaceOnly,
            MalformedValue
        }

        public sealed record ConfigTestCase(
            CorruptionKind Kind,
            string TargetKey,
            string? CorruptedValue,
            Dictionary<string, string?> Settings);

        public static class ConfigTestCaseGen
        {
            public static Arbitrary<ConfigTestCase> Arbitrary()
            {
                var keysGen = Gen.Elements(RequiredKeys);
                var corruptionKindGen = Gen.Elements(
                    CorruptionKind.None,
                    CorruptionKind.OmitKey,
                    CorruptionKind.EmptyString,
                    CorruptionKind.WhitespaceOnly,
                    CorruptionKind.MalformedValue
                );

                var whitespaceGen = Gen.Elements(" ", "   ", "\t", "\n", " \t \r\n ");

                var malformedPortGen = Gen.Elements("0", "-1", "-500", "65536", "70000", "abc", "12.5");
                var malformedUrlGen = Gen.Elements("not_an_url", "relative/path", "://bad", "http://", "ftp://");

                var gen = from kind in corruptionKindGen
                          from key in keysGen
                          from whitespace in whitespaceGen
                          from malformedPort in malformedPortGen
                          from malformedUrl in malformedUrlGen
                          select BuildTestCase(kind, key, whitespace, malformedPort, malformedUrl);

                return gen.ToArbitrary();
            }

            private static ConfigTestCase BuildTestCase(
                CorruptionKind kind,
                string key,
                string whitespace,
                string malformedPort,
                string malformedUrl)
            {
                var dict = CreateValidConfigDict();
                string? corruptedVal = null;

                if (kind == CorruptionKind.None)
                {
                    return new ConfigTestCase(CorruptionKind.None, "", null, dict);
                }

                if (kind == CorruptionKind.OmitKey)
                {
                    dict.Remove(key);
                }
                else if (kind == CorruptionKind.EmptyString)
                {
                    dict[key] = "";
                    corruptedVal = "";
                }
                else if (kind == CorruptionKind.WhitespaceOnly)
                {
                    dict[key] = whitespace;
                    corruptedVal = whitespace;
                }
                else if (kind == CorruptionKind.MalformedValue)
                {
                    if (key == "Smtp:Port")
                    {
                        dict[key] = malformedPort;
                        corruptedVal = malformedPort;
                    }
                    else if (key == "NeckSign:BaseUrl" || key == "NeckSign:WebhookUrl")
                    {
                        dict[key] = malformedUrl;
                        corruptedVal = malformedUrl;
                    }
                    else
                    {
                        // For generic string fields, corruption via empty/whitespace is the invalid form
                        dict[key] = "";
                        corruptedVal = "";
                    }
                }

                return new ConfigTestCase(kind, key, corruptedVal, dict);
            }
        }

        // Feature: modulo-1-infraestrutura, Property 5: Validação de configuração fail-fast
        //
        // Para toda configuração gerada, o Config_Validator deve considerá-la válida se e somente se
        // todas as variáveis obrigatórias estiverem presentes e no formato esperado; se qualquer obrigatória
        // estiver ausente, vazia, composta apenas de espaços ou malformada, o resultado deve ser inválido
        // e deve identificar pelo nome ao menos uma variável afetada.
        //
        // Validates: Requirements 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7
        [Property(MaxTest = 100, Arbitrary = new[] { typeof(ConfigTestCaseGen) })]
        public Property Config_validator_fail_fast_property(ConfigTestCase testCase)
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(testCase.Settings!)
                .Build();

            var result = ConfigValidator.Validate(config);

            if (testCase.Kind == CorruptionKind.None)
            {
                var isValid = result.IsValid;
                var noErrors = result.MissingOrInvalidKeys.Count == 0;
                return (isValid && noErrors).ToProperty()
                    .Label("Valid configuration must succeed with zero errors.");
            }
            else
            {
                var isInvalid = !result.IsValid;
                var containsTargetKey = result.MissingOrInvalidKeys.Contains(testCase.TargetKey);
                var hasErrorMessage = result.ErrorMessages.Any(m => m.Contains(testCase.TargetKey));

                return (isInvalid && containsTargetKey && hasErrorMessage).ToProperty()
                    .Label($"Invalid config ({testCase.Kind}) for key '{testCase.TargetKey}' must report failure identifying the affected key.");
            }
        }
    }
}
