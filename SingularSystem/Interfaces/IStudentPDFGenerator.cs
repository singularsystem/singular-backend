using SingularSystem.DTOs.Students;

namespace SingularSystem.Interfaces
{
    public interface IStudentPDFGenerator
    {
        Task<byte[]> GenerateStudentsPDFAsync(List<StudentsPDFResponse> rows, CancellationToken ct = default);
    }
}
