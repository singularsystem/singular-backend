namespace SingularSystem.DTOs.PEIs
{
    public class PEIUpdateRequest
    {
        public Guid ID { get; set; }
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
