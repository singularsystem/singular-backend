using SingularSystem.DTOs.Students;
using SingularSystem.DTOs.Users;
using SingularSystem.Extensions;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace SingularSystem.DTOs.PEIs
{
    public class PEIResponse
    {
        public Guid ID { get; set; }
        public UserSummaryResponse? Professional { get; set; }
        public StudentSummaryResponse? Student { get; set; }
        public string SchoolYear { get; set; }
        public string Serie { get; set; }
        public string AcademicRecord { get; set; } = "";
        public string MedicalReport { get; set; } = "";
        public string Medications { get; set; } = "";
        public string FollowUp { get; set; } = "";
        public string AdditionalObservations { get; set; } = "";
        public ICollection<PEISubjectResponse> Subjects { get; set; } = new List<PEISubjectResponse>();
        public string? DocumentToken { get; set; }
        public string? SignedFile { get; set; }
        public string DocumentStatus { get; set; }
        public bool TeacherSign { get; set; }
        public string SignUrl { get; set; }

        [Display(Name = "Data de Criação")]
        public DateTime CreatedAt { get; set; }
        public string LocalCreatedAt => CreatedAt.ToBrazilianTime().ToString("dd/MM/yyyy HH:mm", new CultureInfo("pt-BR"));

        [Display(Name = "Última atualização")]
        public DateTime? UpdatedAt { get; set; }
        public string LocalUpdatedAt => UpdatedAt?.ToBrazilianTime().ToString("dd/MM/yyyy HH:mm", new CultureInfo("pt-BR")) ?? string.Empty;
        public DateTime? SignedAt { get; set; }
        public string LocalSignedAt => SignedAt?.ToBrazilianTime().ToString("dd/MM/yyyy HH:mm", new CultureInfo("pt-BR")) ?? string.Empty;

    }
}
