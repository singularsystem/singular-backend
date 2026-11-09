namespace SingularSystem.DTOs.Reports
{
    public class ReportRequest
    {
        public Guid StudentID { get; set; }
    }

    public class UpdateReportConclusionsRequest
    {
        public string? SocialWorkerConclusion { get; set; }
        public string? PsychologistConclusion { get; set; }
        public string? PsychopedagogueConclusion { get; set; }
    }
}
