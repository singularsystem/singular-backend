using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SingularSystem.Security
{
    /// <summary>
    /// Filtro e enriquecedor transversal de sanitização de logs (Req 10.1, 10.2, 10.3, 10.4, Property 7).
    /// Garante que PII, segredos, senhas, tokens, laudos e cabeçalhos de auth não sejam recuperáveis
    /// a partir de entradas de log, preservando tipo e stack trace de exceções.
    /// Em caso de incerteza sobre o dado, a regra padrão é OMITIR.
    /// </summary>
    public static class LogSanitizer
    {
        private static readonly HashSet<string> SensitiveHeaderNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "Authorization",
            "Proxy-Authorization",
            "X-Api-Key",
            "Cookie",
            "Set-Cookie",
            "X-NeckSign-Signature",
            "X-Secret",
            "Token"
        };

        private static readonly string[] SensitiveKeySubstrings =
        {
            "password", "senha", "token", "secret", "segredo", "apikey", "api_key",
            "jwt", "bearer", "cpf", "cnpj", "laudo", "diagnostico", "cid",
            "confidencial", "sensivel", "privad", "dado"
        };

        private static readonly Regex TokenRegex = new(
            @"(?i)(bearer\s+[a-zA-Z0-9_\-\.]+)|([a-f0-9]{32,64})|(\b[A-Za-z0-9+/=]{40,}\b)",
            RegexOptions.Compiled);

        private static readonly Regex JsonSensitiveFieldRegex = new(
            @"(?i)""(?<key>[^""]*(?:password|senha|token|secret|segredo|apikey|api_key|jwt|cpf|laudo)[^""]*)""\s*:\s*""(?<val>[^""]*)""",
            RegexOptions.Compiled);

        private static readonly Regex KeyValueSensitiveRegex = new(
            @"(?i)\b(?<key>password|senha|token|secret|segredo|apikey|api_key|jwt|cpf|laudo)\b[\s:=]+""?(?<val>[^\s,""';]+)""?",
            RegexOptions.Compiled);

        private static readonly Regex EmailRegex = new(
            @"[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+",
            RegexOptions.Compiled);

        /// <summary>
        /// Sanitiza uma mensagem de log em texto livre, mascarando PII e segredos (Req 10.1).
        /// </summary>
        public static string Sanitize(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var sanitized = input;

            // Mascara campos JSON sensíveis
            sanitized = JsonSensitiveFieldRegex.Replace(sanitized, m =>
                $"\"{m.Groups["key"].Value}\": \"[REDACTED]\"");

            // Mascara pares chave-valor sensíveis em texto livre (ex.: senha=123, segredo: abc)
            sanitized = KeyValueSensitiveRegex.Replace(sanitized, m =>
                $"{m.Groups["key"].Value}=[REDACTED]");

            // Mascara tokens Bearer / hex hashes longos
            sanitized = TokenRegex.Replace(sanitized, "[REDACTED_SECRET]");

            // Mascara e-mails
            sanitized = EmailRegex.Replace(sanitized, m => SensitiveDataMasker.MaskEmail(m.Value) ?? "[REDACTED_EMAIL]");

            return sanitized;
        }

        /// <summary>
        /// Sanitiza um cabeçalho HTTP. Se o cabeçalho for de autenticação ou sensível, seu valor é omitido/redigido (Req 10.2).
        /// </summary>
        public static string SanitizeHeader(string headerName, string? headerValue)
        {
            if (string.IsNullOrWhiteSpace(headerName))
                return string.Empty;

            if (SensitiveHeaderNames.Contains(headerName))
                return "[REDACTED]";

            foreach (var sensitive in SensitiveKeySubstrings)
            {
                if (headerName.Contains(sensitive, StringComparison.OrdinalIgnoreCase))
                    return "[REDACTED]";
            }

            return Sanitize(headerValue);
        }

        /// <summary>
        /// Sanitiza uma exceção, preservando tipo e stack trace sem vazar valores sensíveis (Req 10.3).
        /// </summary>
        public static string SanitizeException(Exception? ex)
        {
            if (ex == null)
                return string.Empty;

            var exType = ex.GetType().FullName ?? ex.GetType().Name;
            var sanitizedMessage = Sanitize(ex.Message);
            var sanitizedStackTrace = Sanitize(ex.StackTrace);

            var result = $"{exType}: {sanitizedMessage}";
            if (!string.IsNullOrEmpty(sanitizedStackTrace))
            {
                result += Environment.NewLine + sanitizedStackTrace;
            }

            if (ex.InnerException != null)
            {
                result += Environment.NewLine + " ---> " + SanitizeException(ex.InnerException);
            }

            return result;
        }

        /// <summary>
        /// Determina se um valor é de classificação incerta ou potencialmente sensível.
        /// Caso seja incerto, o default é omitir (Req 10.4).
        /// </summary>
        public static bool IsUncertainOrSensitive(string key, object? value)
        {
            if (string.IsNullOrWhiteSpace(key))
                return true;

            foreach (var term in SensitiveKeySubstrings)
            {
                if (key.Contains(term, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            if (value is string strVal && (TokenRegex.IsMatch(strVal) || EmailRegex.IsMatch(strVal)))
                return true;

            return false;
        }

        /// <summary>
        /// Avalia e retorna o valor seguro para log. Se for sensível ou incerto, omite (Req 10.4).
        /// </summary>
        public static string SafeLogValue(string key, string? rawValue)
        {
            if (IsUncertainOrSensitive(key, rawValue))
                return "[OMITTED]";

            return Sanitize(rawValue);
        }
    }
}
