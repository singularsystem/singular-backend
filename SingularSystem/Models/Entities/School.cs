using SingularSystem.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SingularSystem.Models.Entities
{
    public class School : ISoftDelete
    {
        public School()
        {
            Students = new HashSet<Student>();
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid ID { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string State { get; set; } = string.Empty;

        [Required]
        public string City { get; set; } = string.Empty;

        [ForeignKey(nameof(Principal))]
        public string? PrincipalID { get; set; }
        public ApplicationUser? Principal { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public ICollection<Student> Students { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime? DeactivatedAt { get; set; }
    }
}