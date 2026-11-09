using SingularSystem.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SingularSystem.Models.Entities
{
    public class PAEEProfessionalsAEE
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid ID { get; set; }

        [ForeignKey(nameof(PAEE))]
        public Guid PAEEID { get; set; }
        public PAEE PAEE { get; set; }
        public ProfessionalsAEE Professional { get; set; } = ProfessionalsAEE.None;
    }
}
