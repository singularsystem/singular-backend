using SingularSystem.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SingularSystem.Models.Entities
{
    public class PEI_Subject
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid ID { get; set; }

        [ForeignKey(nameof(PEI))]
        public Guid PEIID { get; set; }
        public PEI PEI { get; set; }
        
        public Subject Subject { get; set; } = Subject.None;
        public ICollection<PEI_Response> Responses { get; set; } = new List<PEI_Response>();
    }
}
