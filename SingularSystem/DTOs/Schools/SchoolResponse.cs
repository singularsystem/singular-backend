using SingularSystem.Extensions;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace SingularSystem.DTOs.Schools
{
    public class SchoolResponse
    {
        public Guid? ID { get; set; }

        [Display(Name = "Nome da Escola")]
        public string? Name { get; set; }

        [Display(Name = "Cidade")]
        public string? City { get; set; }
        public string? State { get; set; } 

        public string? PrincipalID { get; set; }

        public string? PrincipalName { get; set; }

        [Display(Name = "Data de Cria��o")]
        public DateTime? CreatedAt { get; set; }
        public string? LocalCreatedAt => CreatedAt?.ToBrazilianTime().ToString("dd/MM/yyyy HH:mm", new CultureInfo("pt-BR"));

        [Display(Name = "�ltima atualiza��o")]
        public DateTime? UpdatedAt { get; set; }
        public string? LocalUpdatedAt => UpdatedAt?.ToBrazilianTime().ToString("dd/MM/yyyy HH:mm", new CultureInfo("pt-BR"));

        [Display(Name = "Status")]
        public bool? IsActive { get; set; }
        public int? TotalStudents { get; set; }
        public int? TotalStudentsAEE { get; set; }
        public List<DisabilityResponse>? Disabilities { get; set; }
    }
}