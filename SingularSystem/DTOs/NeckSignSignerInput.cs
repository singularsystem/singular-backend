using System.Text.Json.Serialization;

namespace SingularSystem.DTOs
{
    /// <summary>
    /// Papel de um signatário no contrato <c>CreateEnvelope</c> do NeckSign
    /// (<c>POST /api/v1/envelopes</c>). Mapeia diretamente para o enum
    /// <c>role: 'SIGNER' | 'WITNESS' | 'APPROVER'</c> esperado pela API do NeckSign.
    /// </summary>
    /// <remarks>
    /// Os valores são serializados em maiúsculas (<c>SIGNER</c>, <c>WITNESS</c>, <c>APPROVER</c>)
    /// para corresponder exatamente ao schema Zod do NeckSign. No Singular System todos os signatários
    /// (assistente social, psicólogo, psicopedagogo e responsáveis de PEI/PAEE/estudo de caso)
    /// assinam de fato o documento, portanto usam o papel <see cref="Signer"/>.
    /// </remarks>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum NeckSignSignerRole
    {
        /// <summary>Signatário efetivo do documento (<c>SIGNER</c>).</summary>
        SIGNER,

        /// <summary>Testemunha (<c>WITNESS</c>).</summary>
        WITNESS,

        /// <summary>Aprovador (<c>APPROVER</c>).</summary>
        APPROVER
    }

    /// <summary>
    /// Representa um signatário a ser enviado no corpo da requisição de criação de envelope do
    /// NeckSign (<c>POST /api/v1/envelopes</c>). É a forma mínima e tipada exigida pelo contrato
    /// <c>CreateEnvelope</c> do NeckSign:
    /// <c>{ name, email, role: 'SIGNER' | 'WITNESS' | 'APPROVER', order? }</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// O campo <see cref="ExternalId"/> é um acréscimo do sistema (fora do schema estrito do NeckSign)
    /// usado internamente para correlacionar a <c>signingUrl</c> retornada por signatário com o papel
    /// correspondente do <c>report</c> (assistente social / psicólogo / psicopedagogo), conforme
    /// Req 2.9. Para <c>pei</c>/<c>paee</c>/<c>caseStudy</c> há um único signatário e o
    /// <see cref="ExternalId"/> corresponde ao <c>User.Id</c> do responsável.
    /// </para>
    /// <para>
    /// Esta é a camada de composição/filtragem (task 4.2). A serialização final para o corpo exato
    /// do <c>POST</c> (incluindo o <c>externalId</c> por signatário quando aplicável) é
    /// responsabilidade do fluxo de criação (task 4.3).
    /// </para>
    /// </remarks>
    public sealed class NeckSignSignerInput
    {
        /// <summary>Nome do signatário. Nunca vazio após a filtragem de composição.</summary>
        public string Name { get; init; } = default!;

        /// <summary>E-mail do signatário. Nunca vazio após a filtragem de composição e único (dedup por e-mail, case-insensitive).</summary>
        public string Email { get; init; } = default!;

        /// <summary>Papel do signatário no envelope. No Singular System, sempre <see cref="NeckSignSignerRole.SIGNER"/>.</summary>
        public NeckSignSignerRole Role { get; init; } = NeckSignSignerRole.SIGNER;

        /// <summary>
        /// Ordem de assinatura (1-based), correspondente ao campo opcional <c>order</c> do contrato.
        /// Reflete a posição do signatário na lista composta.
        /// </summary>
        public int Order { get; init; }

        /// <summary>
        /// Identificador de correlação do signatário usado pelo Singular System para associar a
        /// <c>signingUrl</c> por papel (<c>report</c>) ou pelo responsável (<c>pei</c>/<c>paee</c>/<c>caseStudy</c>).
        /// Não faz parte do schema estrito do NeckSign; é consumido internamente na persistência (task 4.3).
        /// </summary>
        public string ExternalId { get; init; } = default!;
    }
}
