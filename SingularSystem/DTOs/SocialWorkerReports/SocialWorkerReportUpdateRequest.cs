namespace SingularSystem.DTOs.SocialWorkerReports
{
    public class SocialWorkerReportUpdateRequest
    {
        public Guid ID { get; set; }
        public Guid StudentID { get; set; }
        public Guid ReportID { get; set; }
        public Guid UserID { get; set; }
        public ICollection<SocialWorkerReportProceduresRequest> ProceduresUsed { get; set; } = new List<SocialWorkerReportProceduresRequest>();
        public string? OtherProceduresUsed { get; set; }
        public string FamilyComposition { get; set; }
        public string? OtherFamilyComposition { get; set; }
        public string NumberOfPeopleInTheHousehold { get; set; }
        public string? OtherNumberOfPeopleInTheHousehold { get; set; }
        public string? AdditionalObservationHouseholdMembers { get; set; }
        public string TypeOfHousing { get; set; }
        public string? OtherTypeOfHousing { get; set; }
        public string HousingConditions { get; set; }
        public string? OtherHousingConditions { get; set; }
        public string NumberOfRooms { get; set; }
        public string? OtherNumberOfRooms { get; set; }
        public string MonthlyHouseholdIncome { get; set; }
        public string? OtherMonthlyHouseholdIncome { get; set; }
        public ICollection<SocialWorkerReportFamilyIncomeSourcesRequest> FamilyIncomeSource { get; set; } = new List<SocialWorkerReportFamilyIncomeSourcesRequest>();
        public string? OtherFamilyIncomeSource { get; set; }
        public ICollection<SocialWorkerReportGovernmentBenefitsRequest> ReceivedGovernmentBenefits { get; set; } = new List<SocialWorkerReportGovernmentBenefitsRequest>();
        public string? OtherReceivedGovernmentBenefits { get; set; }
        public ICollection<SocialWorkerReportHealthConditionsRequest> HealthConditions { get; set; } = new List<SocialWorkerReportHealthConditionsRequest>();
        public string? OtherDetailsHealthConditions { get; set; }
        public string ContinuousMedication { get; set; }
        public string? DetailsContinuousMedication { get; set; }
        public string? OtherContinuousMedication { get; set; }
        public string HealthServiceAccess { get; set; }
        public string? OtherHealthServiceAccess { get; set; }
        public string SchoolAttendance { get; set; }
        public string? OtherSchoolAttendance { get; set; }
        public string AcademicPerformance { get; set; }
        public string? OtherAcademicPerformance { get; set; }
        public ICollection<SocialWorkerReportMainSchoolDifficultiesRequest> MainSchoolDifficulties { get; set; } = new List<SocialWorkerReportMainSchoolDifficultiesRequest>();
        public string? OtherMainSchoolDifficulties { get; set; }
        public string DailyMeals { get; set; }
        public string? OtherDailyMeals { get; set; }
        public string FoodSecurity { get; set; }
        public string? OtherFoodSecurity { get; set; }
        public string FamilyRelationship { get; set; }
        public string? OtherFamilyRelationship { get; set; }
        public string SocialSupportNetwork { get; set; }
        public string? OtherSocialSupportNetwork { get; set; }
        public string CommunityActivityParticipation { get; set; }
        public string? OtherCommunityActivityParticipation { get; set; }
        public ICollection<SocialWorkerReportRecommendationsRequest> Recommendations { get; set; } = new List<SocialWorkerReportRecommendationsRequest>();
        public string? OtherRecommendations { get; set; }
        public string? Conclusion { get; set; }
        public string? SocioeconomicContext { get; set; }
    }
}
