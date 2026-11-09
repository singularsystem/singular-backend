namespace SingularSystem.DTOs.PsychopedagogueReports.CognitiveAssessments
{
    public class CognitiveAssessmentUpdateRequest
    {
        public Guid ID { get; set; }
        public Guid StudentID { get; set; }
        public Guid UserID { get; set; }
        public Guid PsychopedagogueReportID { get; set; }

        public string IdentifiedBasicColors { get; set; }
        public string IdentifiedBasicShapes { get; set; }
        public string CompletesAgeAppropriatePuzzles { get; set; }
        public string GroupsObjectsBySimilarCharacteristics { get; set; }
        public string IdentifiesVisualSequencePatterns { get; set; }
        public string UnderstandsSimpleVerbalInstructions { get; set; }
        public string NamesCommonEverydayObjects { get; set; }
        public string FormsAgeAppropriateSentences { get; set; }
        public string AnswersQuestionsAboutFamilyInformation { get; set; }
        public string UnderstandsBasicAbstractConcepts { get; set; }
        public string RemembersTwoToThreeStepInstructions { get; set; }
        public string MaintainsAttentionForAppropriateDuration { get; set; }
        public string RecognizesFamiliarPeopleAndPlaces { get; set; }
        public string RecallsRecentEvents { get; set; }
        public string RepeatsSimpleSequences { get; set; }
        public string SeeksSolutionsToSimpleEverydayProblems { get; set; }
        public string DemonstratesCreativityInPlay { get; set; }
        public string LearnsFromExperience { get; set; }
        public string UnderstandsCauseAndEffectRelationships { get; set; }
        public string AdaptsStrategiesWhenSomethingDoesNotWork { get; set; }
    }
}
