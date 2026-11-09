using System.ComponentModel.DataAnnotations;

namespace SingularSystem.DTOs.Schools
{
    public class SchoolRequest
    {
        [Required(ErrorMessage = "O nome da escola � obrigat�rio")]
        [Display(Name = "Nome da Escola")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "O nome da cidade � obrigat�rio")]
        [Display(Name = "Cidade")]
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;

        public string? PrincipalID { get; set; }
    }
}
