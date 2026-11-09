using SingularSystem.Models.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace SingularSystem.Models.Entities
{
    public class SocialWorkerReport : BaseDocument
    {
        [ForeignKey(nameof(Report))]
        public Guid? ReportID { get; set; }
        public Report? Report { get; set; }

        public ICollection<SocialWorkerReport_Procedure> ProceduresUsed { get; set; } = new List<SocialWorkerReport_Procedure>();
        public string? OtherProceduresUsed { get; set; }
        public FamilyComposition FamilyComposition { get; set; } = Enums.FamilyComposition.None;
        public string? OtherFamilyComposition { get; set; }
        public NumberOfPeopleInTheHousehold NumberOfPeopleInTheHousehold { get; set; } = Enums.NumberOfPeopleInTheHousehold.None;
        public string? OtherNumberOfPeopleInTheHousehold { get; set; }
        public string? AdditionalObservationHouseholdMembers { get; set; }
        public HousingType TypeOfHousing { get; set; } = HousingType.None;
        public string? OtherTypeOfHousing { get; set; }
        public HousingCondition HousingConditions { get; set; } = HousingCondition.None;
        public string? OtherHousingConditions { get; set; }
        public NumberOfHouseholdRooms NumberOfRooms { get; set; } = NumberOfHouseholdRooms.None;
        public string? OtherNumberOfRooms { get; set; }
        public MonthlyFamilyIncome MonthlyHouseholdIncome { get; set; } = MonthlyFamilyIncome.None;
        public string? OtherMonthlyHouseholdIncome { get; set; }
        public ICollection<SocialWorkerReport_FamilyIncomeSource> FamilyIncomeSource { get; set; } = new List<SocialWorkerReport_FamilyIncomeSource>();
        public string? OtherFamilyIncomeSource { get; set; }
        public ICollection<SocialWorkerReport_GovernmentBenefit> ReceivedGovernmentBenefits { get; set; } = new List<SocialWorkerReport_GovernmentBenefit>();
        public string? OtherReceivedGovernmentBenefits { get; set; }
        public ICollection<SocialWorkerReport_HealthCondition> HealthConditions { get; set; } = new List<SocialWorkerReport_HealthCondition>();
        public string? OtherDetailsHealthConditions { get; set; }
        public Evaluation ContinuousMedication { get; set; } = Evaluation.None;
        public string? DetailsContinuousMedication { get; set; }
        public string? OtherContinuousMedication { get; set; }
        public HealthServiceAccess HealthServiceAccess { get; set; } = Enums.HealthServiceAccess.None;
        public string? OtherHealthServiceAccess { get; set; }
        public SchoolAttendance SchoolAttendance { get; set; } = Enums.SchoolAttendance.None;
        public string? OtherSchoolAttendance { get; set; }
        public AcademicPerformance AcademicPerformance { get; set; } = Enums.AcademicPerformance.None;
        public string? OtherAcademicPerformance { get; set; }
        public ICollection<SocialWorkerReport_MainSchoolDifficulty> MainSchoolDifficulties { get; set; } = new List<SocialWorkerReport_MainSchoolDifficulty>();
        public string? OtherMainSchoolDifficulties { get; set; }
        public DailyMeals DailyMeals { get; set; } = Enums.DailyMeals.None;
        public string? OtherDailyMeals { get; set; }
        public FoodSecurity FoodSecurity { get; set; } = Enums.FoodSecurity.None;
        public string? OtherFoodSecurity { get; set; }
        public FamilyRelationship FamilyRelationship { get; set; } = Enums.FamilyRelationship.None;
        public string? OtherFamilyRelationship { get; set; }
        public SocialSupportNetwork SocialSupportNetwork { get; set; } = Enums.SocialSupportNetwork.None;
        public string? OtherSocialSupportNetwork { get; set; }
        public CommunityActivityParticipation CommunityActivityParticipation { get; set; } = Enums.CommunityActivityParticipation.None;
        public string? OtherCommunityActivityParticipation { get; set; }
        public ICollection<SocialWorkerReport_Recommendation> Recommendations { get; set; } = new List<SocialWorkerReport_Recommendation>();
        public string? OtherRecommendations { get; set; }
        public string? Conclusion { get; set; }
        public string? SocioeconomicContext { get; set; }
    }
}