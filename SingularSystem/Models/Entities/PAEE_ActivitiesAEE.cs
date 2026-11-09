using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SingularSystem.Models.Entities
{
    public class PAEEActivitiesAEE
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid ID { get; set; }

        [ForeignKey(nameof(PAEE))]
        public Guid PAEEID { get; set; }
        public PAEE PAEE { get; set; }
        public string Activity { get; set; }
    }
}
