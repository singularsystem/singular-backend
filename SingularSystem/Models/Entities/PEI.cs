using SingularSystem.Models.Enums;

namespace SingularSystem.Models.Entities
{
    public class PEI : BaseDocument
    {
        public int SchoolYear { get; set; }
        public Serie Serie { get; set; } = Serie.None;
        public ICollection<PEI_Subject> Subjects { get; set; } = new List<PEI_Subject>();

        public string AcademicRecord { get; set; } = "";
        public string MedicalReport { get; set; } = "";
        public string Medications { get; set; } = "";
        public string FollowUp { get; set; } = "";
        public string AdditionalObservations { get; set; } = "";

        public string? DocumentToken { get; set; }
        public string? SignedFile { get; set; }
        public Status DocumentStatus { get; set; } = Status.None;
        public SignatureProvider SignatureProvider { get; set; } = SignatureProvider.NeckSign;
        public bool TeacherSign { get; set; } = false;
        public string? SignUrl { get; set; }
        public DateTime? SignedAt { get; set; }
    }
}
