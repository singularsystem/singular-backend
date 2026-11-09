using SingularSystem.DTOs.Students;

namespace SingularSystem.DTOs.ReportAEE
{
    public class ReportAEEPDFResponse
    {
        public string FullName { get; set; } = "";
        public DateTime? DateOfBirth { get; set; }
        public string SchoolName { get; set; } = "";
        public string Serie { get; set; } = "";
        public string City { get; set; } = "";
        public string State { get; set; } = "";
        public int Age =>
            DateOfBirth.HasValue
                ? DateTime.Now.Year - DateOfBirth.Value.Year -
                  (DateTime.Now.DayOfYear < DateOfBirth.Value.DayOfYear ? 1 : 0)
                : 0;
        public ICollection<string> Disabilities { get; set; } = new List<string>();
        public ICollection<string> Disorders { get; set; } = new List<string>();
    }
}
