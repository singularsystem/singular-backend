using SingularSystem.DTOs.Students;

namespace SingularSystem.DTOs.ReportAEE
{
    public class ReportAEEResponse
    {
        public StudentSummaryResponse Student { get; set; }
        public ICollection<string> Disabilities { get; set; } = new List<string>();
        public ICollection<string> Disorders { get; set; } = new List<string>();
    }
}
