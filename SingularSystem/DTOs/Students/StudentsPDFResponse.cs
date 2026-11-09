namespace SingularSystem.DTOs.Students
{
    public class StudentsPDFResponse
    {
        public string FullName { get; set; } = "";
        public DateTime? DateOfBirth { get; set; }
        public string SchoolName { get; set; } = "";
        public string Serie { get; set; } = "";
        public string City { get; set; } = "";
        public string State { get; set; } = "";
    }
}
