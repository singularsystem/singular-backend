using SingularSystem.Interfaces;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SingularSystem.Models.Entities
{
    public class Student : ISoftDelete
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid ID { get; set; }

        [Required]
        public string FullName { get; set; } = string.Empty;

        public Sex Sex { get; set; } = Enums.Sex.None;

        [Required]
        public DateTime DateOfBirth { get; set; }

        [Required]
        public string Address { get; set; } = string.Empty;

        [Required]
        public string State { get; set; }

        [Required]
        public string City { get; set; }

        public Serie Serie { get; set; } = Enums.Serie.None;

        [Required]
        public string NameOfPersonResponsible { get; set; } = string.Empty;

        public MaritalStatus MaritalStatusParents { get; set; } = MaritalStatus.None;

        public SkinColor SkinColor { get; set; } = Enums.SkinColor.None;

        public string? SUSCard { get; set; }

        public string? RG { get; set; }

        public string? CPF { get; set; }

        [ForeignKey(nameof(School))]
        public Guid? SchoolID { get; set; }
        public School? School { get; set; }

        public string? RegistrationType { get; set; } = "Geral";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public Student()
        {
            BaseDocuments = new HashSet<BaseDocument>();
        }

        public ICollection<BaseDocument> BaseDocuments { get; set; }
        
        public bool IsActive { get; set; } = true;
        public DateTime? DeactivatedAt { get; set; }
    }
}
