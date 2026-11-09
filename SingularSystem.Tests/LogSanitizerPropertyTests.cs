using System;
using System.Linq;
using FsCheck;
using FsCheck.Xunit;
using SingularSystem.Security;
using Xunit;

namespace SingularSystem.Tests
{
    public sealed class LogSanitizerPropertyTests
    {
        // Feature: modulo-1-infraestrutura, Property 7: Não-recuperabilidade de dados sensíveis em logs
        //
        // Para toda entrada de log — em qualquer nível de severidade, seja um valor avulso,
        // um registro de requisição HTTP (cabeçalhos de autenticação e corpo), ou uma exceção com
        // PII/segredos em sua mensagem/dados — a saída sanitizada não deve conter o valor sensível
        // original nem uma substring a partir da qual o original seja reconstruível; no caso de
        // exceção, o tipo e o rastreamento de pilha são preservados sem os valores sensíveis; e quando
        // a classificação de um valor é incerta, ele deve ser omitido em vez de registrado em bruto.
        //
        // Validates: Requirements 10.1, 10.2, 10.3, 10.4
        [Property(MaxTest = 100, Arbitrary = new[] { typeof(LogEntryCaseArb) })]
        public Property Sensitive_data_never_recoverable_in_sanitized_logs(LogEntryCase entry)
        {
            switch (entry.Kind)
            {
                case LogEntryKind.AuthHeader:
                {
                    var headerSanitized = LogSanitizer.SanitizeHeader(entry.Key, entry.SecretValue);
                    var notContainsRaw = string.IsNullOrEmpty(entry.SecretValue) ||
                                         !headerSanitized.Contains(entry.SecretValue, StringComparison.Ordinal);
                    var isRedacted = headerSanitized.Contains("[REDACTED]");
                    return (notContainsRaw && isRedacted).ToProperty()
                        .Label($"Header {entry.Key}: Sanitized={headerSanitized}");
                }

                case LogEntryKind.JsonBodyWithSecret:
                {
                    var rawBody = $"{{\"username\": \"alice\", \"{entry.Key}\": \"{entry.SecretValue}\"}}";
                    var bodySanitized = LogSanitizer.Sanitize(rawBody);
                    var notContainsRaw = string.IsNullOrEmpty(entry.SecretValue) ||
                                         !bodySanitized.Contains(entry.SecretValue, StringComparison.Ordinal);
                    return notContainsRaw.ToProperty()
                        .Label($"Body key={entry.Key}, secret={entry.SecretValue}: Sanitized={bodySanitized}");
                }

                case LogEntryKind.ExceptionWithPII:
                {
                    var rawEx = new InvalidOperationException($"Falha com senha={entry.SecretValue} e token={entry.SecretValue}");
                    var exSanitized = LogSanitizer.SanitizeException(rawEx);
                    var preservesType = exSanitized.Contains(typeof(InvalidOperationException).FullName!);
                    var notContainsRaw = string.IsNullOrEmpty(entry.SecretValue) ||
                                         !exSanitized.Contains(entry.SecretValue, StringComparison.Ordinal);
                    return (preservesType && notContainsRaw).ToProperty()
                        .Label($"Exception sanitization: preservesType={preservesType}, notContainsRaw={notContainsRaw}, sanitized={exSanitized}");
                }

                case LogEntryKind.UncertainClassification:
                default:
                {
                    var safeVal = LogSanitizer.SafeLogValue(entry.Key, entry.SecretValue);
                    var isOmittedOrRedacted = safeVal.Contains("[OMITTED]") || safeVal.Contains("[REDACTED");
                    var notContainsRaw = string.IsNullOrEmpty(entry.SecretValue) ||
                                         !safeVal.Contains(entry.SecretValue, StringComparison.Ordinal);
                    return (isOmittedOrRedacted && notContainsRaw).ToProperty()
                        .Label($"Uncertain key={entry.Key}: SafeVal={safeVal}");
                }
            }
        }

        // --- Geradores ---

        public enum LogEntryKind
        {
            AuthHeader,
            JsonBodyWithSecret,
            ExceptionWithPII,
            UncertainClassification
        }

        public sealed class LogEntryCase
        {
            public LogEntryKind Kind { get; }
            public string Key { get; }
            public string SecretValue { get; }

            public LogEntryCase(LogEntryKind kind, string key, string secretValue)
            {
                Kind = kind;
                Key = key;
                SecretValue = secretValue;
            }
        }

        public static class LogEntryCaseArb
        {
            public static Arbitrary<LogEntryCase> Arbitrary()
            {
                var headerKeys = new[] { "Authorization", "X-Api-Key", "Cookie", "X-NeckSign-Signature", "Proxy-Authorization" };
                var bodyKeys = new[] { "password", "senha", "token", "secret", "apiKey", "cpf", "laudo" };
                var uncertainKeys = new[] { "secret_field", "user_token", "diagnostico_paciente", "dados_confidenciais" };

                var secretGen = from str in Arb.Default.NonEmptyString().Generator
                                let clean = new string(str.Get.Where(c => char.IsLetterOrDigit(c)).Take(16).ToArray())
                                where clean.Length >= 4
                                select clean;

                var authHeaderGen = from h in Gen.Elements(headerKeys)
                                    from s in secretGen
                                    select new LogEntryCase(LogEntryKind.AuthHeader, h, "Bearer " + s);

                var bodyGen = from k in Gen.Elements(bodyKeys)
                              from s in secretGen
                              select new LogEntryCase(LogEntryKind.JsonBodyWithSecret, k, s);

                var exGen = from s in secretGen
                            select new LogEntryCase(LogEntryKind.ExceptionWithPII, "exception", s);

                var uncertainGen = from u in Gen.Elements(uncertainKeys)
                                   from s in secretGen
                                   select new LogEntryCase(LogEntryKind.UncertainClassification, u, s);

                return Gen.OneOf(authHeaderGen, bodyGen, exGen, uncertainGen).ToArbitrary();
            }
        }
    }
}
