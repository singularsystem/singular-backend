using SingularSystem.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SingularSystem.Models.Entities
{
    public class SocialWorkerReport_MainSchoolDifficulty
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid ID { get; set; }

        [ForeignKey(nameof(SocialWorkerReport))]
        public Guid SocialWorkerReportID { get; set; }
        public SocialWorkerReport SocialWorkerReport { get; set; }

        public MainSchoolDifficulties Difficulty { get; set; } = MainSchoolDifficulties.None;
    }
}
