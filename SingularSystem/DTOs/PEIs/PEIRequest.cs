using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;

namespace SingularSystem.DTOs.PEIs
{
    public class PEIRequest
    {
        public Guid StudentID { get; set; }
        public Guid UserID { get; set; }
        public string Serie { get; set; }
        public string SchoolYear { get; set; }
        public string AcademicRecord { get; set; } = "";
        public string MedicalReport { get; set; } = "";
        public string Medications { get; set; } = "";
        public string FollowUp { get; set; } = "";
        public string AdditionalObservations { get; set; } = "";
        public ICollection<PEISubjectRequest> Subjects { get; set; } = new List<PEISubjectRequest>();
    }
}
