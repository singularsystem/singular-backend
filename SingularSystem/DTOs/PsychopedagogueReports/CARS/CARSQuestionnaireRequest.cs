namespace SingularSystem.DTOs.PsychopedagogueReports.CARS
{
    public class CARSQuestionnaireRequest
    {
        public Guid StudentID { get; set; }
        public Guid UserID { get; set; }
        public Guid PsychopedagogueReportID { get; set; }

        public string TotalScore { get; set; }
    }
}
