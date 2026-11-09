namespace SingularSystem.DTOs.PsychopedagogueReports.PsychopedagogueQuestionnaires
{
    public class PsychopedagogueQuestionnaireUpdateRequest
    {
        public Guid ID { get; set; }
        public Guid StudentID { get; set; }
        public Guid UserID { get; set; }
        public Guid PsychopedagogueReportID { get; set; }
        //public string Context { get; set; }
        //public string Methodology { get; set; }
        public string ComportamentalObservations { get; set; }
        public string NotebookObservations { get; set; }
        public string OtherObservations { get; set; }
        public ICollection<PsychopedagogueQuestionnaireTypeOfDisabilityRequest> TypeOfDisability { get; set; } = new List<PsychopedagogueQuestionnaireTypeOfDisabilityRequest>();
        public string OtherTypesOfDisability { get; set; }
        public ICollection<PsychopedagogueQuestionnaireTypeOfDisorderRequest> TypeOfDisorder { get; set; } = new List<PsychopedagogueQuestionnaireTypeOfDisorderRequest>();
        public string JustificationForClassification { get; set; }
        public ICollection<PsychopedagogueQuestionnaireRecommendationRequest> Recommendations { get; set; } = new List<PsychopedagogueQuestionnaireRecommendationRequest>();
        public string OtherRecommendations { get; set; }
    }
}
