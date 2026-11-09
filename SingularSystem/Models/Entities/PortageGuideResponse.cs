using SingularSystem.Models.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SingularSystem.Models.Entities
{
    public class PortageGuideResponse
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid ID { get; set; }

        [Required]
        public string Key { get; set; }
        public PortageGuideValue Value { get; set; }

        [ForeignKey(nameof(PortageGuide))]
        public Guid PortageGuideID { get; set; }
        public PortageGuide PortageGuide { get; set; }
    }
}
