using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SingularSystem.Models.Entities
{
    public class PAEEWorkMethodologyAEE
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid ID { get; set; }

        [ForeignKey(nameof(PAEE))]
        public Guid PAEEID { get; set; }
        public PAEE PAEE { get; set; }
        public string Methodology { get; set; }
    }
}
