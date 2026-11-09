using SingularSystem.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SingularSystem.Models.Entities
{
    public class PEI_Response
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid ID { get; set; }

        [Required]
        public string Key { get; set; }
        public PlanResponse Response { get; set; } = PlanResponse.None;
        public string? Category { get; set; }

        [ForeignKey(nameof(PEISubject))]
        public Guid PEISubjectID { get; set; }
        public PEI_Subject PEISubject { get; set; }
    }

}
