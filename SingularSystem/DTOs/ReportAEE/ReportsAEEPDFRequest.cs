namespace SingularSystem.DTOs.ReportAEE
{
    public class ReportsAEEPDFRequest
    {
        public string? State { get; set; }
        public string? City { get; set; }
        public Guid? SchoolID { get; set; }
        public string Disabilities { get; set; }
        public string Disorders { get; set; }
    }
}
