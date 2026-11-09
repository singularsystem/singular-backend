using SingularSystem.DTOs.Students;
using SingularSystem.DTOs.Users;
using SingularSystem.Extensions;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace SingularSystem.DTOs.RegularTeachers.NeuropsychopedagogicalQuestionnaires
{
    public class RegularTeacherQuestionnaireResponse
    {
        public Guid ID { get; set; }
        public UserSummaryResponse? Professional { get; set; }
        public StudentSummaryResponse? Student { get; set; }

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
        public List<BehaviorTraitResponse> BehaviorTraits { get; set; } = new();
        public string BehaviorObservations { get; set; }
        public string DevelopmentLevel { get; set; }
        public string DevelopmentObservations { get; set; }
        public string? AdditionalComments { get; set; }

        [Display(Name = "Data de Criação")]
        public DateTime CreatedAt { get; set; }
        public string LocalCreatedAt => CreatedAt.ToBrazilianTime().ToString("dd/MM/yyyy HH:mm", new CultureInfo("pt-BR"));

        [Display(Name = "Última atualização")]
        public DateTime? UpdatedAt { get; set; }
        public string LocalUpdatedAt => UpdatedAt?.ToBrazilianTime().ToString("dd/MM/yyyy HH:mm", new CultureInfo("pt-BR")) ?? string.Empty;
    }
}
