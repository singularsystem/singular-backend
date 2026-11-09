namespace SingularSystem.DTOs.PsychopedagogueReports.CARS
{
    public class CARSQuestionnaireUpdateRequest
    {
        public Guid ID { get; set; }
        public Guid StudentID { get; set; }
        public Guid UserID { get; set; }
        public Guid PsychopedagogueReportID { get; set; }

        public string TotalScore { get; set; }
    }
}
