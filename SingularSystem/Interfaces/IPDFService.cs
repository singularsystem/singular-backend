using System;
using System.Threading.Tasks;
using SingularSystem.DTOs.Students;
using SingularSystem.Models;

namespace SingularSystem.Interfaces
{
    public interface IPDFService
    {
        Task<byte[]> GeneratePdf(Guid documentId, PdfProfile profile);
        Task<byte[]> GeneratePdf(Guid documentId) => GeneratePdf(documentId, PdfProfile.View);
        Task<string?> GetDocumentType(Guid documentId);
        Task<byte[]> GenerateStudentsPdf(string state, string city, string schoolID, string serie = "");
        Task<byte[]> GenerateReportsAEEPdf(string state, string city, string schoolID, string disabilities, string disorders);
        Task<byte[]> GenerateCaseStudiesTrackingPdf(string state, string city, string schoolID, string searchTerm, string status, int? year, int? month);
        Task<byte[]> GenerateBiopsychosocialTrackingPdf(string state, string city, string schoolID, string searchTerm, string status, string pendingProfessional, int? year, int? month);
    }
}
