using SingularSystem.DTOs.ReportAEE;

namespace SingularSystem.Interfaces
{
    public interface IReportAEEPDFGenerator
    {
        Task<byte[]> GenerateReportsAEEPDFAsync(List<ReportAEEPDFResponse> rows, CancellationToken ct = default);
    }
}
