using System.Threading;
using System.Threading.Tasks;
using SingularSystem.Models;
using SingularSystem.Models.Entities;

namespace SingularSystem.Services;

public interface IIssuerResolver
{
    Task<IssuerHeaderModel> ResolveAsync(BaseDocument document, PdfProfile profile, CancellationToken ct = default);
}
