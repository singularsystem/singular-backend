using SingularSystem.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SingularSystem.Models.Entities
{
    public class PsychopedagogueQuestionnaire_Recommendation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid ID { get; set; }

        [ForeignKey(nameof(PsychopedagogueQuestionnaire))]
        public Guid PsychopedagogueQuestionnaireID { get; set; }
        public PsychopedagogueQuestionnaire PsychopedagogueQuestionnaire { get; set; }

        public PsychopedagogueRecommendations Recommendation { get; set; } = PsychopedagogueRecommendations.None;
    }
}
