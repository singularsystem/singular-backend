namespace SingularSystem.Configuration
{
    /// <summary>
    /// Opções tipadas vinculadas à seção <c>NeckBook</c> de <c>appsettings</c>, contendo os
    /// parâmetros de transporte HTTP para comunicar com a API externa do NeckBook.
    /// </summary>
    /// <remarks>
    /// Não há chave de API global nesta seção: cada município possui sua própria
    /// <c>NeckBook_Api_Key</c> persistida de forma criptografada na entidade
    /// <c>MunicipalityNeckBookConfig</c>.
    /// </remarks>
    public sealed class NeckBookOptions
    {
        /// <summary>Nome da seção de configuração em <c>appsettings</c>.</summary>
        public const string SectionName = "NeckBook";

        /// <summary>
        /// URL base absoluta da API do NeckBook (ex.: <c>https://api.neckbook.com.br</c>).
        /// Usada como <c>BaseAddress</c> do <c>HttpClient</c> nomeado <c>"NeckBook"</c>.
        /// </summary>
        public string BaseUrl { get; set; } = string.Empty;

        /// <summary>
        /// Timeout em segundos para chamadas HTTP ao NeckBook. Padrão: 30 segundos.
        /// </summary>
        public int Timeout { get; set; } = 30;
    }
}
