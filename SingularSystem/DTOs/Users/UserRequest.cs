using System.ComponentModel.DataAnnotations;

namespace SingularSystem.DTOs.Users
{
    public class UserRequest
    {
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

        [Required(ErrorMessage = "O tipo de usuário é obrigatório")]
        [Display(Name = "Tipo de usuário")]
        public string Type { get; set; }

        [Required(ErrorMessage = "A senha é obrigatória")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "A senha deve ter no mínimo 6 caracteres")]
        [DataType(DataType.Password)]
        [Display(Name = "Senha")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "A confirmação de senha é obrigatória")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar Senha")]
        [Compare("Password", ErrorMessage = "As senhas não coincidem")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Display(Name = "Estado")]
        public string? State { get; set; } = string.Empty;

        [Required(ErrorMessage = "O município é obrigatório")]
        [Display(Name = "Município")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "O sexo é obrigatório")]
        [Display(Name = "Sexo")]
        public string Sex { get; set; }

        [Required(ErrorMessage = "A data de nascimento é obrigatória")]
        [Display(Name = "Data de Nascimento")]
        [DataType(DataType.Date)]
        [AgeValidation(MinimumAge = 18, ErrorMessage = "Você deve ter pelo menos 18 anos")]
        public DateTime DateOfBirth { get; set; }

        [Display(Name = "Endereço")]
        public string Address { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "O nome da escola deve ter no máximo 100 caracteres")]
        [Display(Name = "Nome da Escola")]
        public string? SchoolID { get; set; }
    }

    public class AgeValidationAttribute : ValidationAttribute
    {
        public int MinimumAge { get; set; }

        public override bool IsValid(object? value)
        {
            if (value is null)
            {
                return false;
            }

            if (value is DateTime date)
            {
                return date.AddYears(MinimumAge) <= DateTime.Today;
            }
            return false;
        }
    }
}
