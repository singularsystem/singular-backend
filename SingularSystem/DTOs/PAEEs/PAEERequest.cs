namespace SingularSystem.DTOs.Requests
{
    public class PAEERequest
    {
        public Guid StudentID { get; set; }
        public Guid UserID { get; set; }
        public Guid PortageID { get; set; }
        public string AcademicRecord { get; set; }
        public string MedicalReport { get; set; }
        public string Medications { get; set; }
        public string FollowUp { get; set; }
        public List<PAEETypeAEERequest> OrganizationOfCare { get; set; } = new List<PAEETypeAEERequest>();
        public string? OtherOrganization { get; set; }
        public string WeeklyFrequency { get; set; }
        public string? OtherFrequency { get; set; }
        public string AssistingTimePerSession { get; set; }
        public string? OtherServiceTime { get; set; }
        public string CompositionOfAssisting { get; set; }
        public List<PAEEProfessionalsAEERequest> OtherProfessionals { get; set; } = new List<PAEEProfessionalsAEERequest>();
        public string? MedicalSpecialty { get; set; }
        public string? OtherProfessionalsDescription { get; set; }
        public List<PAEEActivitiesAEERequest> DifferentiatedActivitiesInAEE { get; set; } = new List<PAEEActivitiesAEERequest>();
        public List<PAEEWorkMethodologyAEERequest> WorkMethodology { get; set; } = new List<PAEEWorkMethodologyAEERequest>();
        public List<PAEEMaterialResourcesAEERequest> MaterialResourcesAndEquipment { get; set; } = new List<PAEEMaterialResourcesAEERequest>();
        public List<PAEEEvaluationCriterionAEERequest> CriteriaInEvaluation { get; set; } = new List<PAEEEvaluationCriterionAEERequest>();
        public List<PAEEResourceAndStrategiesRequest> ResourcesAndStrategies { get; set; } = new List<PAEEResourceAndStrategiesRequest>();
        public string AdditionalObservations { get; set; }
    }
}
