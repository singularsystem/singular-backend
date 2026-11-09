using SingularSystem.DTOs;

namespace SingularSystem.Interfaces
{
    /// <summary>
    /// Abstração do provedor de assinatura digital do iPAAGS. Espelha o contrato de
    /// <see cref="IZapSignService"/> (mesmos nomes, quantidade, ordem e tipos de parâmetro),
    /// diferindo apenas no tipo de retorno de <see cref="GetDocumentDetails"/>, que passa a ser
    /// o DTO neutro <see cref="SignatureDocumentDetails"/> em vez de um tipo acoplado ao ZapSign.
    /// Permite trocar ZapSign por NeckSign sem reescrever os controllers chamadores.
    /// </summary>
    public interface ISignatureService
    {
        Task<bool> CreateDoc(Guid idDoc, string type);
        Task<bool> DeleteDoc(Guid idDoc, string type);
        Task<SignatureDocumentDetails?> GetDocumentDetails(string documentToken, bool bypassCooldown = false);
        Task<bool> SyncDocument(string documentToken, bool bypassCooldown = false);
        Task SyncPendingDocuments();
    }
}
