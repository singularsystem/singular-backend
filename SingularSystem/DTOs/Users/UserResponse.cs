using SingularSystem.Extensions;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace SingularSystem.DTOs.Users
{
    public class UserResponse
    {
        public string ID { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string? PhoneNumber { get; set; }
        public List<string> Roles { get; set; } = new();
        public string? State { get; set; }
        public string City { get; set; }
        public string Sex { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string FormattedDateOfBirth => DateOfBirth.ToString("dd/MM/yyyy");
        public int Age => DateTime.Now.Year - DateOfBirth.Year -
                         (DateTime.Now.DayOfYear < DateOfBirth.DayOfYear ? 1 : 0);
        public string Address { get; set; }
        public Guid? SchoolID { get; set; }
        public string? SchoolName { get; set; }
        public string Status { get; set; }
        public bool MustChangePassword { get; set; }
        public DateTime? LastLogin { get; set; }
        public string? LocalLastLogin => LastLogin?.ToBrazilianTime().ToString("dd/MM/yyyy HH:mm", new CultureInfo("pt-BR"));
        public DateTime CreatedAt { get; set; }
        public string LocalCreatedAt => CreatedAt.ToBrazilianTime().ToString("dd/MM/yyyy HH:mm", new CultureInfo("pt-BR"));

        [Display(Name = "�ltima atualiza��o")]
        public DateTime? UpdatedAt { get; set; }
        public string? LocalUpdatedAt => UpdatedAt?.ToBrazilianTime().ToString("dd/MM/yyyy HH:mm", new CultureInfo("pt-BR"));
    }
}
