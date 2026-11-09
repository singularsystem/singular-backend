using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SingularSystem.Models.Entities
{
    public class PAEEMaterialResourcesAEE
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid ID { get; set; }

        [ForeignKey(nameof(PAEE))]
        public Guid PAEEID { get; set; }
        public PAEE PAEE { get; set; }
        public string Resource { get; set; }
    }
}
