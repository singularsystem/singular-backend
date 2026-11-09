using System.ComponentModel.DataAnnotations;

namespace SingularSystem.DTOs.Students
{
    public class StudentRequest
    {
        [Required(ErrorMessage = "O nome completo é obrigatório")]
        [StringLength(400, MinimumLength = 3, ErrorMessage = "O nome deve ter entre 3 e 400 caracteres")]
        [Display(Name = "Nome Completo")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "O sexo é obrigatório")]
        [Display(Name = "Sexo")]
        public string Sex { get; set; }

        [Required(ErrorMessage = "A data de nascimento é obrigatória")]
        [Display(Name = "Data de Nascimento")]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "O endereço é obrigatório")]
        [Display(Name = "Endereço")]
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;

        [Required(ErrorMessage = "O nome da escola é obrigatório")]
        [Display(Name = "Nome da Escola")]
        public string SchoolName { get; set; } = string.Empty;

        [Required(ErrorMessage = "O nome da cidade é obrigatório")]
        [Display(Name = "Nome da cidade")]
        public string SchoolCity { get; set; } = string.Empty;
        public string SchoolState { get; set; } = string.Empty;

        [Required(ErrorMessage = "A série é obrigatória")]
        [Display(Name = "Série Escolar")]
        public string Serie { get; set; }

        [Required(ErrorMessage = "O nome do(a) responsável é obrigatório")]
        [Display(Name = "Nome do(a) Responsável")]
        public string NameOfPersonResponsible { get; set; } = string.Empty;

        [Required(ErrorMessage = "O estado civil dos pais é obrigatório")]
        [Display(Name = "Estado civil dos Pais")]
        public string MaritalStatusParents { get; set; } = string.Empty;

        [Required(ErrorMessage = "A cor da pele do aluno é obrigatório")]
        [Display(Name = "Cor da Pele")]
        public string SkinColor { get; set; } = string.Empty;

        //[Required(ErrorMessage = "O número do cartão do SUS é obrigatório")]
        [Display(Name = "Número do Cartão do SUS")]
        public string SUSCard { get; set; } = string.Empty;

        //[Required(ErrorMessage = "O RG é obrigatório")]
        [Display(Name = "Número do RG")]
        public string RG { get; set; } = string.Empty;

        //[Required(ErrorMessage = "O CPF é obrigatório")]
        [Display(Name = "Número do CPF")]
        public string CPF { get; set; } = string.Empty;
    }
}
