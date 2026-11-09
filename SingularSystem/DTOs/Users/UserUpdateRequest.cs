using System.ComponentModel.DataAnnotations;

namespace SingularSystem.DTOs.Users
{
    public class UserUpdateRequest
    {
        [Required(ErrorMessage = "O ID do usuario é obrigatório")]
        public string ID { get; set; }

        [Required(ErrorMessage = "O nome completo é obrigatório")]
        [StringLength(400, MinimumLength = 3, ErrorMessage = "O nome deve ter entre 3 e 400 caracteres")]
        [Display(Name = "Nome Completo")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "O email é obrigatório")]
        [EmailAddress(ErrorMessage = "Email em formato inválido")]
        [StringLength(100, ErrorMessage = "O email deve ter no máximo 100 caracteres")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Número de Telefone")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Estado")]
        public string? State { get; set; } = string.Empty;

        [Required(ErrorMessage = "O município é obrigatório")]
        [Display(Name = "Município")]
        public string City { get; set; } = string.Empty;

        [Display(Name = "Endereço")]
        public string Address { get; set; } = string.Empty;
        public string? SchoolID { get; set; }
        public string? CurrentPassword { get; set; }
        public string? NewPassword { get; set; }
        [Compare("NewPassword", ErrorMessage = "As senhas não coincidem")]
        public string? ConfirmNewPassword { get; set; }
    }
}
