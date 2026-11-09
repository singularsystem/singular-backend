using SingularSystem.DTOs;
using SingularSystem.Security;

namespace SingularSystem.Mappers
{
    /// <summary>
    /// Mapeia o signatário neutro <see cref="SignatureSigner"/> (DTO de provedor da spec 012)
    /// para o signatário de resposta <see cref="SignedUrlSigner"/>, aplicando mascaramento
    /// parcial de nome e e-mail (LGPD) e preservando o estado e a data de assinatura.
    /// </summary>
    public static class SignedUrlSignerMapper
    {
        /// <summary>
        /// Converte um <see cref="SignatureSigner"/> em <see cref="SignedUrlSigner"/>, mascarando
        /// nome ("João D. S.") e e-mail ("usu***@dominio.com") via <see cref="SensitiveDataMasker"/>.
        /// </summary>
        /// <param name="signer">Signatário neutro de origem.</param>
        /// <returns>Signatário de resposta com dados pessoais parcialmente mascarados.</returns>
        public static SignedUrlSigner ToResponse(SignatureSigner signer)
        {
            ArgumentNullException.ThrowIfNull(signer);

            return new SignedUrlSigner
            {
                Name = SensitiveDataMasker.MaskName(signer.Name),
                Email = SensitiveDataMasker.MaskEmail(signer.Email),
                Signed = signer.Signed,
                SignedAt = signer.SignedAt
            };
        }

        /// <summary>
        /// Converte uma coleção de <see cref="SignatureSigner"/> em uma lista de
        /// <see cref="SignedUrlSigner"/> mascarados, preservando a ordem.
        /// </summary>
        /// <param name="signers">Signatários neutros de origem (pode ser <c>null</c>).</param>
        /// <returns>Lista de signatários mascarados; vazia quando a entrada é nula ou vazia.</returns>
        public static IReadOnlyList<SignedUrlSigner> ToResponseList(IEnumerable<SignatureSigner>? signers)
        {
            if (signers is null)
                return [];

            return [.. signers.Select(ToResponse)];
        }
    }
}
