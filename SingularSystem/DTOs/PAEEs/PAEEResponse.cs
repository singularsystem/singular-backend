using SingularSystem.DTOs.Portage;
using SingularSystem.DTOs.Students;
using SingularSystem.DTOs.Users;
using SingularSystem.Extensions;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace SingularSystem.DTOs.Responses
{
    public class PAEEResponse
    {
        public Guid ID { get; set; }
        public UserSummaryResponse? Professional { get; set; }
        public StudentSummaryResponse? Student { get; set; }
        public PortageResponse Portage { get; set; }
        
        public string TypeOfDisability { get; set; }
        public string AcademicRecord { get; set; }
        public string MedicalReport { get; set; }
        public string Medications { get; set; }
        public string FollowUp { get; set; }
        public List<PAEETypeAEEResponse> OrganizationOfCare { get; set; } = new List<PAEETypeAEEResponse>();
        public string? OtherOrganization { get; set; }
        public string WeeklyFrequency { get; set; }
        public string? OtherFrequency { get; set; }
        public string AssistingTimePerSession { get; set; }
        public string? OtherServiceTime { get; set; }
        public string CompositionOfAssisting { get; set; }
        public List<PAEEProfessionalsAEEResponse> OtherProfessionals { get; set; } = new List<PAEEProfessionalsAEEResponse>();
        public string? MedicalSpecialty { get; set; }
        public string? OtherProfessionalsDescription { get; set; }
        public List<PAEEActivitiesAEEResponse> DifferentiatedActivitiesInAEE { get; set; } = new List<PAEEActivitiesAEEResponse>();
        public List<PAEEWorkMethodologyAEEResponse> WorkMethodology { get; set; } = new List<PAEEWorkMethodologyAEEResponse>();
        public List<PAEEMaterialResourcesAEEResponse> MaterialResourcesAndEquipment { get; set; } = new List<PAEEMaterialResourcesAEEResponse>();
        public List<PAEEEvaluationCriterionAEEResponse> CriteriaInEvaluation { get; set; } = new List<PAEEEvaluationCriterionAEEResponse>();
        public List<PAEEResourcesAndStrategiesResponse> ResourcesAndStrategies { get; set; } = new List<PAEEResourcesAndStrategiesResponse>();
        public string AdditionalObservations { get; set; }
        public string? DocumentToken { get; set; }
        public string? SignedFile { get; set; }
        public string DocumentStatus { get; set; }
        public bool TeacherSign { get; set; }
        public string SignUrl { get; set; }

        [Display(Name = "Data de Criação")]
        public DateTime CreatedAt { get; set; }
        public string LocalCreatedAt => CreatedAt.ToBrazilianTime().ToString("dd/MM/yyyy HH:mm", new CultureInfo("pt-BR"));

        [Display(Name = "Última atualização")]
        public DateTime? UpdatedAt { get; set; }
        public string LocalUpdatedAt => UpdatedAt?.ToBrazilianTime().ToString("dd/MM/yyyy HH:mm", new CultureInfo("pt-BR")) ?? string.Empty;
        public DateTime? SignedAt { get; set; }
        public string LocalSignedAt => SignedAt?.ToBrazilianTime().ToString("dd/MM/yyyy HH:mm", new CultureInfo("pt-BR")) ?? string.Empty;

    }
}
