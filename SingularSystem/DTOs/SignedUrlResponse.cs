using System.Text.Json.Serialization;

namespace SingularSystem.DTOs
{
    /// <summary>
    /// Contrato de resposta estável e agnóstico ao provedor do endpoint
    /// <c>GET /api/v1/documents/{documentToken}/signed-url</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Serializado em <b>camelCase</b> explícito (via <see cref="JsonPropertyName"/>) para blindar o
    /// frontend contra a convenção PascalCase padrão do controller. O consumidor lê um único campo
    /// estável de URL (<c>signedFileUrl</c>), independentemente de o documento ter sido originado no
    /// ZapSign ou no NeckSign — nenhum campo ou valor revela o provedor de origem.
    /// </para>
    /// <para>
    /// Este DTO é envelopado em <c>ApiResponse&lt;SignedUrlResponse&gt;</c> na resposta final.
    /// </para>
    /// </remarks>
    public sealed class SignedUrlResponse
    {
        /// <summary>
        /// Status neutro do documento, derivado de <see cref="SignatureDocumentStatus"/>
        /// (<c>Pending</c> | <c>Signing</c> | <c>Signed</c> | <c>Declined</c> | <c>Expired</c> | <c>Voided</c>).
        /// Nunca expõe nomes de status específicos do ZapSign nem do NeckSign.
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; init; } = default!;

        /// <summary>
        /// URL do PDF assinado final. Preenchida apenas quando o documento está assinado;
        /// permanece <c>null</c> para documentos não assinados.
        /// </summary>
        [JsonPropertyName("signedFileUrl")]
        public string? SignedFileUrl { get; init; }

        /// <summary>Lista de signatários neutros, com nome/e-mail parcialmente mascarados.</summary>
        [JsonPropertyName("signers")]
        public IReadOnlyList<SignedUrlSigner> Signers { get; init; } = [];
    }

    /// <summary>
    /// Signatário neutro exposto no contrato de resposta <see cref="SignedUrlResponse"/>.
    /// </summary>
    /// <remarks>
    /// O mascaramento parcial de <see cref="Name"/> e <see cref="Email"/> é aplicado na etapa de
    /// mapeamento de <c>SignatureSigner</c> → <see cref="SignedUrlSigner"/> (task 2.2).
    /// </remarks>
    public sealed class SignedUrlSigner
    {
        /// <summary>Nome do signatário (parcialmente mascarado, ex.: "João D. S.").</summary>
        [JsonPropertyName("name")]
        public string? Name { get; init; }

        /// <summary>E-mail do signatário (parcialmente mascarado, ex.: "jo***@dominio.com").</summary>
        [JsonPropertyName("email")]
        public string? Email { get; init; }

        /// <summary>Indica se o signatário já assinou.</summary>
        [JsonPropertyName("signed")]
        public bool Signed { get; init; }

        /// <summary>Data e hora da assinatura, quando houver.</summary>
        [JsonPropertyName("signedAt")]
        public DateTimeOffset? SignedAt { get; init; }
    }

    /// <summary>
    /// Mapeia o enum neutro <see cref="SignatureDocumentStatus"/> para a representação textual
    /// estável usada no campo <c>status</c> da resposta.
    /// </summary>
    /// <remarks>
    /// O mapeamento é explícito (não depende de <c>Enum.ToString()</c>) para que o contrato de
    /// resposta permaneça estável mesmo que o enum interno seja renomeado ou reordenado no futuro.
    /// Os valores produzidos são neutros, sem expor nomes de status do ZapSign nem do NeckSign.
    /// </remarks>
    public static class SignedUrlStatusMapper
    {
        /// <summary>
        /// Converte um <see cref="SignatureDocumentStatus"/> na string neutra correspondente.
        /// </summary>
        /// <param name="status">Status neutro do documento.</param>
        /// <returns>
        /// A representação textual estável: <c>Pending</c>, <c>Signing</c>, <c>Signed</c>,
        /// <c>Declined</c>, <c>Expired</c> ou <c>Voided</c>.
        /// </returns>
        public static string ToNeutralStatus(SignatureDocumentStatus status) => status switch
        {
            SignatureDocumentStatus.Pending => "Pending",
            SignatureDocumentStatus.Signing => "Signing",
            SignatureDocumentStatus.Signed => "Signed",
            SignatureDocumentStatus.Declined => "Declined",
            SignatureDocumentStatus.Expired => "Expired",
            SignatureDocumentStatus.Voided => "Voided",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Status de documento de assinatura não suportado.")
        };
    }
}
