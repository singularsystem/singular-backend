using SingularSystem.DTOs.SocialWorkerReports;
using SingularSystem.DTOs.Users;
using SingularSystem.Extensions;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;

namespace SingularSystem.Mappers
{
    public static class SocialWorkerReportMapper
    {
        public static SocialWorkerReport ToEntity(
            SocialWorkerReportRequest model,
            string userId
        )
        {
            var entity = new SocialWorkerReport()
            {
                UserID = userId,
                ReportID = model.ReportID,
                StudentID = model.StudentID,
                ProceduresUsed = model.ProceduresUsed.Select(r => new SocialWorkerReport_Procedure()
                {
                    Procedure = Helpers.GetEnumValueFromDisplayName<SocialWorkerProcedures>(r.Procedure)
                }).ToList(),
                OtherProceduresUsed = model.OtherProceduresUsed,
                FamilyComposition = Helpers.GetEnumValueFromDisplayName<FamilyComposition>(model.FamilyComposition),
                OtherFamilyComposition = model.OtherFamilyComposition,
                NumberOfPeopleInTheHousehold = Helpers.GetEnumValueFromDisplayName<NumberOfPeopleInTheHousehold>(model.NumberOfPeopleInTheHousehold),
                OtherNumberOfPeopleInTheHousehold = model.OtherNumberOfPeopleInTheHousehold,
                AdditionalObservationHouseholdMembers = model.AdditionalObservationHouseholdMembers,
                TypeOfHousing = Helpers.GetEnumValueFromDisplayName<HousingType>(model.TypeOfHousing),
                OtherTypeOfHousing = model.OtherTypeOfHousing,
                HousingConditions = Helpers.GetEnumValueFromDisplayName<HousingCondition>(model.HousingConditions),
                OtherHousingConditions = model.OtherHousingConditions,
                NumberOfRooms = Helpers.GetEnumValueFromDisplayName<NumberOfHouseholdRooms>(model.NumberOfRooms),
                OtherNumberOfRooms = model.OtherNumberOfRooms,
                MonthlyHouseholdIncome = Helpers.GetEnumValueFromDisplayName<MonthlyFamilyIncome>(model.MonthlyHouseholdIncome),
                OtherMonthlyHouseholdIncome = model.OtherMonthlyHouseholdIncome,
                FamilyIncomeSource = model.FamilyIncomeSource.Select(r => new SocialWorkerReport_FamilyIncomeSource()
                {
                    Source = Helpers.GetEnumValueFromDisplayName<IncomeSource>(r.Source)
                }).ToList(),
                OtherFamilyIncomeSource = model.OtherFamilyIncomeSource,
                ReceivedGovernmentBenefits = model.ReceivedGovernmentBenefits.Select(r => new SocialWorkerReport_GovernmentBenefit()
                {
                    Benefit = Helpers.GetEnumValueFromDisplayName<GovernmentBenefits>(r.Benefit)
                }).ToList(),
                OtherReceivedGovernmentBenefits = model.OtherReceivedGovernmentBenefits,
                HealthConditions = model.HealthConditions.Select(r => new SocialWorkerReport_HealthCondition()
                {
                    Condition = Helpers.GetEnumValueFromDisplayName<HealthConditions>(r.Condition)
                }).ToList(),
                OtherDetailsHealthConditions = model.OtherDetailsHealthConditions,
                ContinuousMedication = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.ContinuousMedication),
                DetailsContinuousMedication = model.DetailsContinuousMedication,
                OtherContinuousMedication = model.OtherContinuousMedication,
                HealthServiceAccess = Helpers.GetEnumValueFromDisplayName<HealthServiceAccess>(model.HealthServiceAccess),
                OtherHealthServiceAccess = model.OtherHealthServiceAccess,
                SchoolAttendance = Helpers.GetEnumValueFromDisplayName<SchoolAttendance>(model.SchoolAttendance),
                OtherSchoolAttendance = model.OtherSchoolAttendance,
                AcademicPerformance = Helpers.GetEnumValueFromDisplayName<AcademicPerformance>(model.AcademicPerformance),
                OtherAcademicPerformance = model.OtherAcademicPerformance,
                MainSchoolDifficulties = model.MainSchoolDifficulties.Select(r => new SocialWorkerReport_MainSchoolDifficulty()
                {
                    Difficulty = Helpers.GetEnumValueFromDisplayName<MainSchoolDifficulties>(r.Difficulty)
                }).ToList(),
                OtherMainSchoolDifficulties = model.OtherMainSchoolDifficulties,
                DailyMeals = Helpers.GetEnumValueFromDisplayName<DailyMeals>(model.DailyMeals),
                OtherDailyMeals = model.OtherDailyMeals,
                FoodSecurity = Helpers.GetEnumValueFromDisplayName<FoodSecurity>(model.FoodSecurity),
                OtherFoodSecurity = model.OtherFoodSecurity,
                FamilyRelationship = Helpers.GetEnumValueFromDisplayName<FamilyRelationship>(model.FamilyRelationship),
                OtherFamilyRelationship = model.OtherFamilyRelationship,
                SocialSupportNetwork = Helpers.GetEnumValueFromDisplayName<SocialSupportNetwork>(model.SocialSupportNetwork),
                OtherSocialSupportNetwork = model.OtherSocialSupportNetwork,
                CommunityActivityParticipation = Helpers.GetEnumValueFromDisplayName<CommunityActivityParticipation>(model.CommunityActivityParticipation),
                OtherCommunityActivityParticipation = model.OtherCommunityActivityParticipation,
                Recommendations = model.Recommendations.Select(r => new SocialWorkerReport_Recommendation()
                {
                    Recommendation = Helpers.GetEnumValueFromDisplayName<Recommendations>(r.Recommendation)
                }).ToList(),
                OtherRecommendations = model.OtherRecommendations,
                Conclusion = model.Conclusion,
                SocioeconomicContext = model.SocioeconomicContext,
            };

            return entity;
        }

        public static SocialWorkerReportResponse ToResponse(
            SocialWorkerReport entity, UserSummaryResponse? createdBy
        )
        {
            return new SocialWorkerReportResponse()
            {
                ID = entity.ID,
                Professional = createdBy,
                Student = entity.Student != null ? StudentMapper.ToSummaryResponse(entity.Student) : null,
                ReportID = entity.ReportID,
                Conclusion = entity.Conclusion,
                ProceduresUsed = entity.ProceduresUsed.Select(o => new SocialWorkerReportProceduresResponse()
                {
                    ID = o.ID,
                    SocialWorkerReportID = o.SocialWorkerReportID,
                    Procedure = o.Procedure.GetDisplayName(),
                }).ToList(),
                OtherProceduresUsed = entity.OtherProceduresUsed,
                FamilyComposition = entity.FamilyComposition.GetDisplayName(),
                OtherFamilyComposition = entity.OtherFamilyComposition,
                NumberOfPeopleInTheHousehold = entity.NumberOfPeopleInTheHousehold.GetDisplayName(),
                OtherNumberOfPeopleInTheHousehold = entity.OtherNumberOfPeopleInTheHousehold,
                AdditionalObservationHouseholdMembers = entity.AdditionalObservationHouseholdMembers,
                TypeOfHousing = entity.TypeOfHousing.GetDisplayName(),
                OtherTypeOfHousing = entity.OtherTypeOfHousing,
                HousingConditions = entity.HousingConditions.GetDisplayName(),
                OtherHousingConditions = entity.OtherHousingConditions,
                NumberOfRooms = entity.NumberOfRooms.GetDisplayName(),
                OtherNumberOfRooms = entity.OtherNumberOfRooms,
                MonthlyHouseholdIncome = entity.MonthlyHouseholdIncome.GetDisplayName(),
                OtherMonthlyHouseholdIncome = entity.OtherMonthlyHouseholdIncome,
                FamilyIncomeSource = entity.FamilyIncomeSource.Select(o => new SocialWorkerReportFamilyIncomeSourcesResponse()
                {
                    ID = o.ID,
                    SocialWorkerReportID = o.SocialWorkerReportID,
                    Source = o.Source.GetDisplayName(),
                }).ToList(),
                OtherFamilyIncomeSource = entity.OtherFamilyIncomeSource,
                ReceivedGovernmentBenefits = entity.ReceivedGovernmentBenefits.Select(o => new SocialWorkerReportGovernmentBenefitsResponse()
                {
                    ID = o.ID,
                    SocialWorkerReportID = o.SocialWorkerReportID,
                    Benefit = o.Benefit.GetDisplayName(),
                }).ToList(),
                OtherReceivedGovernmentBenefits = entity.OtherReceivedGovernmentBenefits,
                HealthConditions = entity.HealthConditions.Select(o => new SocialWorkerReportHealthConditionsResponse()
                {
                    ID = o.ID,
                    SocialWorkerReportID = o.SocialWorkerReportID,
                    Condition = o.Condition.GetDisplayName(),
                }).ToList(),
                OtherDetailsHealthConditions = entity.OtherDetailsHealthConditions,
                ContinuousMedication = entity.ContinuousMedication.GetDisplayName(),
                DetailsContinuousMedication = entity.DetailsContinuousMedication,
                OtherContinuousMedication = entity.OtherContinuousMedication,
                HealthServiceAccess = entity.HealthServiceAccess.GetDisplayName(),
                OtherHealthServiceAccess = entity.OtherHealthServiceAccess,
                SchoolAttendance = entity.SchoolAttendance.GetDisplayName(),
                OtherSchoolAttendance = entity.OtherSchoolAttendance,
                AcademicPerformance = entity.AcademicPerformance.GetDisplayName(),
                OtherAcademicPerformance = entity.OtherAcademicPerformance,
                MainSchoolDifficulties = entity.MainSchoolDifficulties.Select(o => new SocialWorkerReportMainSchoolDifficultiesResponse()
                {
                    ID = o.ID,
                    SocialWorkerReportID = o.SocialWorkerReportID,
                    Difficulty = o.Difficulty.GetDisplayName(),
                }).ToList(),
                OtherMainSchoolDifficulties = entity.OtherMainSchoolDifficulties,
                DailyMeals = entity.DailyMeals.GetDisplayName(),
                OtherDailyMeals = entity.OtherDailyMeals,
                FoodSecurity = entity.FoodSecurity.GetDisplayName(),
                OtherFoodSecurity = entity.OtherFoodSecurity,
                FamilyRelationship = entity.FamilyRelationship.GetDisplayName(),
                OtherFamilyRelationship = entity.OtherFamilyRelationship,
                SocialSupportNetwork = entity.SocialSupportNetwork.GetDisplayName(),
                OtherSocialSupportNetwork = entity.OtherSocialSupportNetwork,
                CommunityActivityParticipation = entity.CommunityActivityParticipation.GetDisplayName(),
                OtherCommunityActivityParticipation = entity.OtherCommunityActivityParticipation,
                Recommendations = entity.Recommendations.Select(o => new SocialWorkerReportRecommendationsResponse()
                {
                    ID = o.ID,
                    SocialWorkerReportID = o.SocialWorkerReportID,
                    Recommendation = o.Recommendation.GetDisplayName(),
                }).ToList(),
                OtherRecommendations = entity.OtherRecommendations,
                SocioeconomicContext = entity.SocioeconomicContext,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
            };
        }

        public static SocialWorkerReportSummaryResponse ToSummaryResponse(
            SocialWorkerReport entity, UserSummaryResponse? createdBy
        )
        {
            return new SocialWorkerReportSummaryResponse()
            {
                ID = entity.ID,
                Professional = createdBy,
                ReportID = entity.ReportID,
                Student = entity.Student != null ? StudentMapper.ToSummaryResponse(entity.Student) : null,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
            };
        }

        public static void UpdateEntity(
            SocialWorkerReport entity,
            SocialWorkerReportUpdateRequest model
        )
        {
            entity.ProceduresUsed = model.ProceduresUsed.Select(r => new SocialWorkerReport_Procedure()
            {
                Procedure = Helpers.GetEnumValueFromDisplayName<SocialWorkerProcedures>(r.Procedure)
            }).ToList();
            entity.OtherProceduresUsed = model.OtherProceduresUsed;
            entity.FamilyComposition = Helpers.GetEnumValueFromDisplayName<FamilyComposition>(model.FamilyComposition);
            entity.OtherFamilyComposition = model.OtherFamilyComposition;
            entity.NumberOfPeopleInTheHousehold = Helpers.GetEnumValueFromDisplayName<NumberOfPeopleInTheHousehold>(model.NumberOfPeopleInTheHousehold);
            entity.OtherNumberOfPeopleInTheHousehold = model.OtherNumberOfPeopleInTheHousehold;
            entity.AdditionalObservationHouseholdMembers = model.AdditionalObservationHouseholdMembers;
            entity.TypeOfHousing = Helpers.GetEnumValueFromDisplayName<HousingType>(model.TypeOfHousing);
            entity.OtherTypeOfHousing = model.OtherTypeOfHousing;
            entity.HousingConditions = Helpers.GetEnumValueFromDisplayName<HousingCondition>(model.HousingConditions);
            entity.OtherHousingConditions = model.OtherHousingConditions;
            entity.NumberOfRooms = Helpers.GetEnumValueFromDisplayName<NumberOfHouseholdRooms>(model.NumberOfRooms);
            entity.OtherNumberOfRooms = model.OtherNumberOfRooms;
            entity.MonthlyHouseholdIncome = Helpers.GetEnumValueFromDisplayName<MonthlyFamilyIncome>(model.MonthlyHouseholdIncome);
            entity.OtherMonthlyHouseholdIncome = model.OtherMonthlyHouseholdIncome;
            entity.FamilyIncomeSource = model.FamilyIncomeSource.Select(r => new SocialWorkerReport_FamilyIncomeSource()
            {
                Source = Helpers.GetEnumValueFromDisplayName<IncomeSource>(r.Source)
            }).ToList();
            entity.OtherFamilyIncomeSource = model.OtherFamilyIncomeSource;
            entity.ReceivedGovernmentBenefits = model.ReceivedGovernmentBenefits.Select(r => new SocialWorkerReport_GovernmentBenefit()
            {
                Benefit = Helpers.GetEnumValueFromDisplayName<GovernmentBenefits>(r.Benefit)
            }).ToList();
            entity.OtherReceivedGovernmentBenefits = model.OtherReceivedGovernmentBenefits;
            entity.HealthConditions = model.HealthConditions.Select(r => new SocialWorkerReport_HealthCondition()
            {
                Condition = Helpers.GetEnumValueFromDisplayName<HealthConditions>(r.Condition)
            }).ToList();
            entity.OtherDetailsHealthConditions = model.OtherDetailsHealthConditions;
            entity.ContinuousMedication = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.ContinuousMedication);
            entity.DetailsContinuousMedication = model.DetailsContinuousMedication;
            entity.OtherContinuousMedication = model.OtherContinuousMedication;
            entity.HealthServiceAccess = Helpers.GetEnumValueFromDisplayName<HealthServiceAccess>(model.HealthServiceAccess);
            entity.OtherHealthServiceAccess = model.OtherHealthServiceAccess;
            entity.SchoolAttendance = Helpers.GetEnumValueFromDisplayName<SchoolAttendance>(model.SchoolAttendance);
            entity.OtherSchoolAttendance = model.OtherSchoolAttendance;
            entity.AcademicPerformance = Helpers.GetEnumValueFromDisplayName<AcademicPerformance>(model.AcademicPerformance);
            entity.OtherAcademicPerformance = model.OtherAcademicPerformance;
            entity.MainSchoolDifficulties = model.MainSchoolDifficulties.Select(r => new SocialWorkerReport_MainSchoolDifficulty()
            {
                Difficulty = Helpers.GetEnumValueFromDisplayName<MainSchoolDifficulties>(r.Difficulty)
            }).ToList();
            entity.OtherMainSchoolDifficulties = model.OtherMainSchoolDifficulties;
            entity.DailyMeals = Helpers.GetEnumValueFromDisplayName<DailyMeals>(model.DailyMeals);
            entity.OtherDailyMeals = model.OtherDailyMeals;
            entity.FoodSecurity = Helpers.GetEnumValueFromDisplayName<FoodSecurity>(model.FoodSecurity);
            entity.OtherFoodSecurity = model.OtherFoodSecurity;
            entity.FamilyRelationship = Helpers.GetEnumValueFromDisplayName<FamilyRelationship>(model.FamilyRelationship);
            entity.OtherFamilyRelationship = model.OtherFamilyRelationship;
            entity.SocialSupportNetwork = Helpers.GetEnumValueFromDisplayName<SocialSupportNetwork>(model.SocialSupportNetwork);
            entity.OtherSocialSupportNetwork = model.OtherSocialSupportNetwork;
            entity.CommunityActivityParticipation = Helpers.GetEnumValueFromDisplayName<CommunityActivityParticipation>(model.CommunityActivityParticipation);
            entity.OtherCommunityActivityParticipation = model.OtherCommunityActivityParticipation;
            entity.Recommendations = model.Recommendations.Select(r => new SocialWorkerReport_Recommendation()
            {
                Recommendation = Helpers.GetEnumValueFromDisplayName<Recommendations>(r.Recommendation)
            }).ToList();
            entity.OtherRecommendations = model.OtherRecommendations;
            entity.Conclusion = model.Conclusion;
            entity.SocioeconomicContext = model.SocioeconomicContext;
            entity.UpdatedAt = DateTime.UtcNow;

            if (entity.Report?.UnifiedAt != null)
            {
                if (entity.Report.IsReadyForUnification())
                {
                    entity.Report.UnifyReport();
                    entity.Report.UpdatedAt = DateTime.UtcNow;
                }
            }
        }
    }
}
