using SingularSystem.DTOs.Students;
using SingularSystem.DTOs.Users;
using SingularSystem.Extensions;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace SingularSystem.DTOs.SocialWorkerReports
{
    public class SocialWorkerReportResponse
    {
        public Guid ID { get; set; }
        public UserSummaryResponse? Professional { get; set; }
        public StudentSummaryResponse? Student { get; set; }
        public Guid? ReportID { get; set; }

        public ICollection<SocialWorkerReportProceduresResponse> ProceduresUsed { get; set; } = new List<SocialWorkerReportProceduresResponse>();
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
        public ICollection<SocialWorkerReportFamilyIncomeSourcesResponse> FamilyIncomeSource { get; set; } = new List<SocialWorkerReportFamilyIncomeSourcesResponse>();
        public string? OtherFamilyIncomeSource { get; set; }
        public ICollection<SocialWorkerReportGovernmentBenefitsResponse> ReceivedGovernmentBenefits { get; set; } = new List<SocialWorkerReportGovernmentBenefitsResponse>();
        public string? OtherReceivedGovernmentBenefits { get; set; }
        public ICollection<SocialWorkerReportHealthConditionsResponse> HealthConditions { get; set; } = new List<SocialWorkerReportHealthConditionsResponse>();
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
        public ICollection<SocialWorkerReportMainSchoolDifficultiesResponse> MainSchoolDifficulties { get; set; } = new List<SocialWorkerReportMainSchoolDifficultiesResponse>();
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
        public ICollection<SocialWorkerReportRecommendationsResponse> Recommendations { get; set; } = new List<SocialWorkerReportRecommendationsResponse>();
        public string? OtherRecommendations { get; set; }
        public string? Conclusion { get; set; }
        public string? SocioeconomicContext { get; set; }

        [Display(Name = "Data de Criação")]
        public DateTime CreatedAt { get; set; }
        public string LocalCreatedAt => CreatedAt.ToBrazilianTime().ToString("dd/MM/yyyy HH:mm", new CultureInfo("pt-BR"));

        [Display(Name = "Última atualização")]
        public DateTime? UpdatedAt { get; set; }
        public string LocalUpdatedAt => UpdatedAt?.ToBrazilianTime().ToString("dd/MM/yyyy HH:mm", new CultureInfo("pt-BR")) ?? string.Empty;
    }
}
