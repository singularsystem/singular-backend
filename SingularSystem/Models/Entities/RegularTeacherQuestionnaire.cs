using SingularSystem.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SingularSystem.Models.Entities
{
    public class RegularTeacherQuestionnaire : BaseDocument
    {
        public RegularTeacherEvaluationDocument? RegularTeacherEvaluationDocument { get; set; }

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
        public ICollection<RegularTeacherQuestionnaire_BehaviourTrait> BehaviorTraits { get; set; } = new List<RegularTeacherQuestionnaire_BehaviourTrait>();
        public string BehaviorObservations { get; set; }
        public StudentDevelopmentComparison DevelopmentLevel { get; set; } = StudentDevelopmentComparison.None;
        public string DevelopmentObservations { get; set; }
        public string? AdditionalComments { get; set; }
    }
}