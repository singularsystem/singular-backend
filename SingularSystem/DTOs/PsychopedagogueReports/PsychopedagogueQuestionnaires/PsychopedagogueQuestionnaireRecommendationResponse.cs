namespace SingularSystem.DTOs.PsychopedagogueReports.PsychopedagogueQuestionnaires
{
    public class PsychopedagogueQuestionnaireRecommendationResponse
    {
        public Guid ID { get; set; }
        public Guid PsychopedagogueQuestionnaireID { get; set; }
        public string Recommendation { get; set; }
    }
}
