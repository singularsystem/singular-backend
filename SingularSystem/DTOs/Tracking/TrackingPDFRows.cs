namespace SingularSystem.DTOs.Tracking
{
    public class CaseStudiesTrackingPDFRow
    {
        public string StudentName { get; set; } = string.Empty;
        public string SchoolName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string LastUpdate { get; set; } = string.Empty;
    }

    public class BiopsychosocialTrackingPDFRow
    {
        public string StudentName { get; set; } = string.Empty;
        public string SchoolName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string PendingProfessionals { get; set; } = string.Empty;
    }
}
