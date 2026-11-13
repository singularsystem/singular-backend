using System.ComponentModel.DataAnnotations;

namespace SingularSystem.DTOs.Users
{
    public class SchoolUserRequest
    {
        [Required(ErrorMessage = "O nome completo é obrigatório")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "O email é obrigatório")]
        [EmailAddress(ErrorMessage = "Email em formato inválido")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "O tipo de usuário é obrigatório")]
        public string Type { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }
        public string? Sex { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Address { get; set; }
    }
}
