using SingularSystem.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SingularSystem.Models.Entities
{
    public class SocialWorkerReport_HealthCondition
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid ID { get; set; }

        [ForeignKey(nameof(SocialWorkerReport))]
        public Guid SocialWorkerReportID { get; set; }
        public SocialWorkerReport SocialWorkerReport { get; set; }

        public HealthConditions Condition { get; set; } = HealthConditions.None;
    }
}
