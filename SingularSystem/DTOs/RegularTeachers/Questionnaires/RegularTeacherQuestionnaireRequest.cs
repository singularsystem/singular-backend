namespace SingularSystem.DTOs.RegularTeachers.NeuropsychopedagogicalQuestionnaires
{
    public class RegularTeacherQuestionnaireRequest
    {
        public Guid StudentID { get; set; }
        public Guid UserID { get; set; }
        public string ClassroomDevelopment { get; set; }
        public string ClassroomBehavior { get; set; }
        public string MainDifficulties { get; set; }
        public string LearningCharacteristics { get; set; }
        public string ReadingAndWritingDescription { get; set; }
        public string LogicalMathematicalReasoningDescription { get; set; }
        public string CompletesSchoolActivities { get; set; }
        public string CompletesHomework { get; set; }
        public string ReactionToFrustration { get; set; }
        public string InappropriateBehaviorDescription { get; set; }
        public string DifficultyWorkingInGroup { get; set; }
        public string DifficultyOrganizingTasks { get; set; }
        public string PeerAcceptance { get; set; }
        public List<BehaviorTraitsRequest> BehaviorTraits { get; set; } = new();
        public string BehaviorObservations { get; set; }
        public string DevelopmentLevel { get; set; }
        public string DevelopmentObservations { get; set; }
        public string? AdditionalComments { get; set; }
    }
}
