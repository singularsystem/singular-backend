namespace SingularSystem.DTOs
{
    /// <summary>
    /// DTO neutro de retorno de <c>ISignatureService.GetDocumentDetails</c>, desacoplado de
    /// qualquer provedor de assinatura. Os consumidores não conhecem nomes específicos de
    /// ZapSign nem de NeckSign; cada implementação mapeia sua resposta para este tipo.
    /// </summary>
    public sealed class SignatureDocumentDetails
    {
        /// <summary>Identificador de correlação do documento (GUID da entidade no Singular System).</summary>
        public string ExternalId { get; init; } = default!;

        /// <summary>URL do PDF assinado final; preenchida apenas quando o status for <see cref="SignatureDocumentStatus.Signed"/>.</summary>
        public string? SignedFileUrl { get; init; }

        /// <summary>Status neutro do documento, independente de provedor.</summary>
        public SignatureDocumentStatus Status { get; init; }

        /// <summary>Signatários do documento.</summary>
        public IReadOnlyList<SignatureSigner> Signers { get; init; } = [];
    }

    /// <summary>
    /// Signatário neutro de um documento de assinatura, desacoplado de qualquer provedor.
    /// </summary>
    public sealed class SignatureSigner
    {
        /// <summary>Identificador de correlação do signatário (por papel ou usuário).</summary>
        public string ExternalId { get; init; } = default!;

        /// <summary>Nome do signatário.</summary>
        public string? Name { get; init; }

        /// <summary>E-mail do signatário.</summary>
        public string? Email { get; init; }

        /// <summary>Indica se o signatário já assinou.</summary>
        public bool Signed { get; init; }

        /// <summary>Data e hora da assinatura, quando houver.</summary>
        public DateTimeOffset? SignedAt { get; init; }
    }

    /// <summary>
    /// Status neutro de um documento de assinatura. Representado em termos independentes de
    /// provedor, sem nomes específicos de ZapSign ou NeckSign.
    /// </summary>
    public enum SignatureDocumentStatus
    {
        Pending,
        Signing,
        Signed,
        Declined,
        Expired,
        Voided
    }
}
