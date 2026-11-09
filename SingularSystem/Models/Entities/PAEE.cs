using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SingularSystem.Models.Entities
{
    public class PAEE : BaseDocument
    {
        [ForeignKey(nameof(Portage))]
        public Guid PortageID { get; set; }
        public PortageGuide Portage { get; set; }
        public string AcademicRecord { get; set; }
        public string MedicalReport { get; set; }
        public string Medications { get; set; }
        public string FollowUp { get; set; }
        public ICollection<PAEETypeAEE> OrganizationOfCare { get; set; } = new List<PAEETypeAEE>();
        public string? OtherOrganization { get; set; }
        public FrequencyAEE WeeklyFrequency { get; set; } = FrequencyAEE.None;
        public string? OtherFrequency { get; set; }
        public ServiceTimeAEE AssistingTimePerSession { get; set; } = ServiceTimeAEE.None;
        public string? OtherServiceTime { get; set; }
        public string CompositionOfAssisting { get; set; }
        public ICollection<PAEEProfessionalsAEE> OtherProfessionals { get; set; } = new List<PAEEProfessionalsAEE>();
        public string? MedicalSpecialty { get; set; }
        public string? OtherProfessionalsDescription { get; set; }
        public ICollection<PAEEActivitiesAEE> DifferentiatedActivitiesInAEE { get; set; } = new List<PAEEActivitiesAEE>();
        public ICollection<PAEEWorkMethodologyAEE> WorkMethodology { get; set; } = new List<PAEEWorkMethodologyAEE>();
        public ICollection<PAEEMaterialResourcesAEE> MaterialResourcesAndEquipment { get; set; } = new List<PAEEMaterialResourcesAEE>();
        public ICollection<PAEEEvaluationCriterionAEE> CriteriaInEvaluation { get; set; } = new List<PAEEEvaluationCriterionAEE>();
        public ICollection<PAEEResourcesAndStrategiesAEE> ResourcesAndStrategies { get; set; } = new List<PAEEResourcesAndStrategiesAEE>();
        public string AdditionalObservations { get; set; }
       
        public string? DocumentToken { get; set; }
        public string? SignedFile { get; set; }
        public Status DocumentStatus { get; set; } = Status.None;
        public SignatureProvider SignatureProvider { get; set; } = SignatureProvider.NeckSign;
        public bool TeacherSign { get; set; } = false;
        public string? SignUrl { get; set; }
        public DateTime? SignedAt { get; set; }

    }
}
