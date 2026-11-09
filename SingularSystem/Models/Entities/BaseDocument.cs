using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SingularSystem.Models.Entities
{
    public class BaseDocument
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid ID { get; set; }

        [ForeignKey(nameof(Student))]
        public Guid? StudentID { get; set; }
        public Student? Student { get; set; }

        [ForeignKey(nameof(User))]
        public string? UserID { get; set; }
        public ApplicationUser? User { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? UnifiedAt { get; set; }
    }
}
