using SingularSystem.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SingularSystem.Models.Entities
{
    public class Notification
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid ID { get; set; }

        [ForeignKey(nameof(User))]
        public string? UserID { get; set; }
        public ApplicationUser? User { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(50)]
        public TypeNotification Type { get; set; } = TypeNotification.None;

        [Required]
        [MaxLength(255)]
        public string Title { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Message { get; set; }

        public bool Read { get; set; } = false;

        public DateTime? ReadAt { get; set; }

        [MaxLength(255)]
        public string ActionURL { get; set; }
    }
}
