using System.ComponentModel.DataAnnotations;

public class LoginRequest
{
    [Required(ErrorMessage = "Campo obrigatório")]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Campo obrigatório")]
    [Display(Name = "Senha")]
    [StringLength(100, ErrorMessage = "A senha deve ter no mínimo 6 caracteres.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Lembrar-me")]
    public bool RememberMe { get; set; }
}