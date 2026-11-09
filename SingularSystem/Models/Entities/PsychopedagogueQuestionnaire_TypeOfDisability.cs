using SingularSystem.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SingularSystem.Models.Entities
{
    public class PsychopedagogueQuestionnaire_TypeOfDisability
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid ID { get; set; }

        [ForeignKey(nameof(PsychopedagogueQuestionnaire))]
        public Guid PsychopedagogueQuestionnaireID { get; set; }
        public PsychopedagogueQuestionnaire PsychopedagogueQuestionnaire { get; set; }

        public TypeOfDisability Disability { get; set; } = TypeOfDisability.None;
    }
}
