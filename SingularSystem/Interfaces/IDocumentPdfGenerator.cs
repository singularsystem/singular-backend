using System.Threading;
using System.Threading.Tasks;
using SingularSystem.Models;
using SingularSystem.Models.Entities;

namespace SingularSystem.Interfaces
{
    public interface IDocumentPdfGenerator
    {
        string Discriminator { get; } 

        Task<byte[]> GenerateAsync(BaseDocument document, PdfRenderContext context, CancellationToken ct = default)
            => GenerateAsync(document, ct);

        Task<byte[]> GenerateAsync(BaseDocument document, CancellationToken ct = default)
            => GenerateAsync(document, PdfRenderContext.Default(), ct);
    }
}
