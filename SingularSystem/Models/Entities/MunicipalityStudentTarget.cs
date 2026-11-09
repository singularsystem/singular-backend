using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SingularSystem.Models.Entities
{
    public class MunicipalityStudentTarget
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid ID { get; set; }

        [Required]
        [MaxLength(100)]
        public string State { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string City { get; set; } = string.Empty;

        [Required]
        public int TotalStudents { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
