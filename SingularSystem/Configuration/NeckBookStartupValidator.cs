using Microsoft.Extensions.Configuration;

namespace SingularSystem.Configuration
{
    /// <summary>
    /// Validação de inicialização (<i>fail-fast</i>) da configuração do NeckBook (Req 2.1–2.4).
    /// Deve ser invocada no <c>Program.cs</c> logo após a construção do
    /// <c>WebApplicationBuilder</c>, antes de o servidor HTTP começar a escutar, para garantir que
    /// uma configuração inválida encerre o processo com código de saída diferente de zero
    /// (<c>Environment.Exit(1)</c>).
    /// </summary>
    public static class NeckBookStartupValidator
    {
        /// <summary>
        /// Código de saída usado quando a validação de inicialização falha.
        /// </summary>
        public const int FailureExitCode = 1;

        /// <summary>
        /// Avalia se <c>NeckBook:BaseUrl</c> está presente, não vazia e é uma URI absoluta válida
        /// (com esquema HTTP ou HTTPS), sem encerrar o processo (método isolável para testes unitários).
        /// </summary>
        public static bool TryValidate(IConfiguration configuration, out string? errorMessage)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            var baseUrlKey = $"{NeckBookOptions.SectionName}:{nameof(NeckBookOptions.BaseUrl)}";
            var baseUrl = configuration[baseUrlKey];

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                errorMessage =
                    $"[FATAL] A configuração '{baseUrlKey}' está ausente ou vazia. " +
                    "Encerrando a inicialização.";
                return false;
            }

            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) ||
                (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
                 !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
            {
                errorMessage =
                    $"[FATAL] A configuração '{baseUrlKey}' não é uma URL absoluta válida (valor: \"{baseUrl}\"). " +
                    "Encerrando a inicialização.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        /// <summary>
        /// Valida a configuração do NeckBook e escreve no <paramref name="errorWriter"/> (ou
        /// <see cref="Console.Error"/>) caso inválida, retornando <c>false</c>.
        /// </summary>
        public static bool Validate(IConfiguration configuration, TextWriter? errorWriter = null)
        {
            if (TryValidate(configuration, out var errorMessage))
            {
                return true;
            }

            (errorWriter ?? Console.Error).WriteLine(errorMessage);
            return false;
        }

        /// <summary>
        /// Valida a configuração do NeckBook e encerra o processo
        /// (<see cref="Environment.Exit(int)"/> com <see cref="FailureExitCode"/>) caso ela seja
        /// inválida. Em configuração válida, retorna normalmente e a inicialização prossegue.
        /// </summary>
        public static void ValidateOrExit(IConfiguration configuration)
        {
            if (!Validate(configuration, Console.Error))
            {
                Environment.Exit(FailureExitCode);
            }
        }
    }
}
