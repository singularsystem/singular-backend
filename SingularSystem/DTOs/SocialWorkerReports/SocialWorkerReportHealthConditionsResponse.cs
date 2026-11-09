namespace SingularSystem.DTOs.SocialWorkerReports
{
    public class SocialWorkerReportHealthConditionsResponse
    {
        public Guid ID { get; set; }
        public Guid SocialWorkerReportID { get; set; }
        public string Condition { get; set; }
    }
}
