using System.ComponentModel.DataAnnotations;

namespace SingularSystem.DTOs.Schools
{
    public class SchoolUpdateRequest
    {
        [Required(ErrorMessage = "O ID da escola é obrigatório")]
        public Guid ID { get; set; }

        [Required(ErrorMessage = "O nome da escola é obrigatório")]
        [Display(Name = "Nome da Escola")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "O nome da cidade é obrigatório")]
        [Display(Name = "Cidade")]
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string PrincipalID { get; set; }
    }
}
