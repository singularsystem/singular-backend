using SingularSystem.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SingularSystem.Models.Entities
{
    public class RegularTeacherQuestionnaire_BehaviourTrait
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid ID { get; set; }

        [ForeignKey(nameof(RegularTeacherQuestionnaire))]
        public Guid RegularTeacherQuestionnaireID { get; set; }
        public RegularTeacherQuestionnaire RegularTeacherQuestionnaire { get; set; }
        public StudentBehaviorTrait BehaviorTrait { get; set; } = StudentBehaviorTrait.None;
    }
}