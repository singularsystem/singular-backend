using SingularSystem.DTOs.Students;
using SingularSystem.Models.Entities;

namespace SingularSystem.Interfaces
{
    public interface IDocumentPdfGenerator
    {
        string Discriminator { get; } 
        Task<byte[]> GenerateAsync(BaseDocument document, CancellationToken ct = default);
    }
}
