using SingularSystem.Extensions;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace SingularSystem.DTOs.Students
{
    public class StudentResponse
    {
        public Guid ID { get; set; }
        public string FullName { get; set; } = string.Empty;

        public string Sex { get; set; }

        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;

        public DateTime DateOfBirth { get; set; }
        public string FormattedDateOfBirth => DateOfBirth.ToString("dd/MM/yyyy");
        public int Age => DateTime.Now.Year - DateOfBirth.Year -
                         (DateTime.Now.DayOfYear < DateOfBirth.DayOfYear ? 1 : 0);

        public string Address { get; set; } = string.Empty;

        public string SchoolName { get; set; } = string.Empty;

        public string SchoolCity { get; set; } = string.Empty;
        public string SchoolState { get; set; } = string.Empty;

        public string Serie { get; set; }

        public string NameOfPersonResponsible { get; set; } = string.Empty;
        public string MaritalStatusParents { get; set; } = string.Empty;
        public string SkinColor { get; set; } = string.Empty;

        public string SUSCard { get; set; } = string.Empty;

        public string RG { get; set; } = string.Empty;

        public string CPF { get; set; } = string.Empty;

        public string ReportStatus { get; set; } = string.Empty;
        public string TypeOfDisability { get; set; } = string.Empty;
        //public Guid? ReportID { get; set; }
        //public Guid? PsychologistReportID { get; set; }
        //public Guid? PsychopedagogueReportID { get; set; }
        //public Guid? SocialWorkerReportID { get; set; }
        //public Guid? SnapIVQuestionnaireID { get; set; }
        //public Guid? CarsChildhoodQuestionnaireID { get; set; }
        //public Guid? NeuropsychopedagogicalQuestionnaireID { get; set; }
        public Guid? MCHATRQuestionnaireID { get; set; }
        public int MCHATRQuestionnaireScore { get; set; } = 0;
        //public Guid? MCHATRFQuestionnaireID { get; set; }

        public DateTime CreatedAt { get; set; }
        public string LocalCreatedAt => CreatedAt.ToBrazilianTime().ToString("dd/MM/yyyy HH:mm", new CultureInfo("pt-BR"));

        [Display(Name = "�ltima atualiza��o")]
        public DateTime? UpdatedAt { get; set; }
        public string? LocalUpdatedAt => UpdatedAt?.ToBrazilianTime().ToString("dd/MM/yyyy HH:mm", new CultureInfo("pt-BR"));

        public DateTime? ReportCreationDate { get; set; }
        public string? LocalReportCreationDate => ReportCreationDate?.ToBrazilianTime().ToString("dd/MM/yyyy HH:mm", new CultureInfo("pt-BR"));
    }
}