using SingularSystem.Models.Enums;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace SingularSystem.Models.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public string? State { get; set; }
        public string City { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public Sex Sex { get; set; } = Sex.None;
        public string Address { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        public bool IsActive { get; set; } = true;
        public bool MustChangePassword { get; set; } = false;

        [ForeignKey(nameof(School))]
        public Guid? SchoolID { get; set; }
        public School? School { get; set; }

        public School? SchoolAsPrincipal { get; set; }

    }
}
