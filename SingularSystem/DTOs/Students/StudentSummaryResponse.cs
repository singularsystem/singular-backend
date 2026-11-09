namespace SingularSystem.DTOs.Students
{
    public class StudentSummaryResponse
    {
        public Guid? ID { get; set; }
        public string? FullName { get; set; }
        public string? Sex { get; set; }
        public string City { get; set; } 
        public string State { get; set; }
        public string Address { get; set; } 
        public string? SchoolName { get; set; }
        public string? SchoolCity { get; set; }
        public string? SchoolState { get; set; }
        public string? Serie { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string NameOfPersonResponsible { get; set; }
        public string FormattedDateOfBirth =>
            DateOfBirth?.ToString("dd/MM/yyyy") ?? "";
        public int Age =>
            DateOfBirth.HasValue
                ? DateTime.Now.Year - DateOfBirth.Value.Year -
                  (DateTime.Now.DayOfYear < DateOfBirth.Value.DayOfYear ? 1 : 0)
                : 0;
    }
}
