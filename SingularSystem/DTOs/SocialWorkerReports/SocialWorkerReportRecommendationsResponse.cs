namespace SingularSystem.DTOs.SocialWorkerReports
{
    public class SocialWorkerReportRecommendationsResponse
    {
        public Guid ID { get; set; }
        public Guid SocialWorkerReportID { get; set; }
        public string Recommendation { get; set; }
    }
}
