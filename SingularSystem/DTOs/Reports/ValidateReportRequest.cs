namespace SingularSystem.DTOs.Reports
{
    public class ValidateReportRequest
    {
        public Guid ID { get; set; }
        public Guid UserID { get; set; }
        public bool Confirm { get; set; }
    }
}
