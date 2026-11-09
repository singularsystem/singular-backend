namespace SingularSystem.DTOs.Students
{
    public class StudentsPDFRequest
    {
        public string? State { get; set; }
        public string? City { get; set; }
        public Guid? SchoolID { get; set; }
    }
}
