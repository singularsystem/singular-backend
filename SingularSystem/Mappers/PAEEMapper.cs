using SingularSystem.DTOs.PsychologistReports.PsychologistQuestionnaire;
using SingularSystem.DTOs.RegularTeachers.RegularTeacherScreenings;
using SingularSystem.DTOs.Reports;
using SingularSystem.DTOs.Requests;
using SingularSystem.DTOs.Responses;
using SingularSystem.DTOs.Users;
using SingularSystem.Extensions;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;

namespace SingularSystem.Mappers
{
    public static class PAEEMapper
    {
        public static PAEE ToEntity(
            PAEERequest model,
            string userId
        )
        {
            return new PAEE()
            {
                StudentID = model.StudentID,
                UserID = userId,
                PortageID = model.PortageID,
                AcademicRecord = model.AcademicRecord,
                MedicalReport = model.MedicalReport,
                Medications = model.Medications,
                FollowUp = model.FollowUp,
                OrganizationOfCare = model.OrganizationOfCare.Select(r => new PAEETypeAEE()
                {
                    Type = Helpers.GetEnumValueFromDisplayName<TypeAEE>(r.Type)
                }).ToList(),
                OtherOrganization = model.OtherOrganization,
                WeeklyFrequency = Helpers.GetEnumValueFromDisplayName<FrequencyAEE>(model.WeeklyFrequency),
                OtherFrequency = model.OtherFrequency,
                AssistingTimePerSession = Helpers.GetEnumValueFromDisplayName<ServiceTimeAEE>(model.AssistingTimePerSession),
                OtherServiceTime = model.OtherServiceTime,
                CompositionOfAssisting = model.CompositionOfAssisting,
                OtherProfessionals = model.OtherProfessionals.Select(p => new PAEEProfessionalsAEE()
                {
                    Professional = Helpers.GetEnumValueFromDisplayName<ProfessionalsAEE>(p.Professional)
                }).ToList(),
                MedicalSpecialty = model.MedicalSpecialty,
                OtherProfessionalsDescription = model.OtherProfessionalsDescription,
                DifferentiatedActivitiesInAEE = model.DifferentiatedActivitiesInAEE.Select(a => new PAEEActivitiesAEE()
                {
                    Activity = a.Activity,
                }).ToList(),
                WorkMethodology = model.WorkMethodology.Select(w => new PAEEWorkMethodologyAEE()
                {
                    Methodology = w.Methodology,
                }).ToList(),
                MaterialResourcesAndEquipment = model.MaterialResourcesAndEquipment.Select(m => new PAEEMaterialResourcesAEE()
                {
                    Resource = m.Resource,
                }).ToList(),
                CriteriaInEvaluation = model.CriteriaInEvaluation.Select(c => new PAEEEvaluationCriterionAEE()
                {
                    Criterion = c.Criterion,
                }).ToList(),
                ResourcesAndStrategies = model.ResourcesAndStrategies.Select(r => new PAEEResourcesAndStrategiesAEE()
                {
                    Strategy = r.Strategy
                }).ToList(),
                AdditionalObservations = model.AdditionalObservations
            };
        }

        public static PAEEResponse ToResponse(
            PAEE entity, UserSummaryResponse? createdBy
        )
        {
            return new PAEEResponse()
            {
                ID = entity.ID,
                Professional = createdBy,
                Student = entity.Student != null ? StudentMapper.ToSummaryResponse(entity.Student) : null,
                Portage = PortageGuideMapper.ToResponse(entity.Portage, null),
                AcademicRecord = entity.AcademicRecord,
                MedicalReport = entity.MedicalReport,
                Medications = entity.Medications,
                FollowUp = entity.FollowUp,
                TypeOfDisability = "",
                OrganizationOfCare = entity.OrganizationOfCare.Select(o => new PAEETypeAEEResponse()
                {
                    ID = o.ID,
                    PAEEID = o.PAEEID,
                    Type = o.Type.GetDisplayName(),
                }).ToList(),
                OtherOrganization = entity.OtherOrganization,
                WeeklyFrequency = entity.WeeklyFrequency.GetDisplayName(),
                OtherFrequency = entity.OtherFrequency,
                AssistingTimePerSession = entity.AssistingTimePerSession.GetDisplayName(),
                OtherServiceTime = entity.OtherServiceTime,
                CompositionOfAssisting = entity.CompositionOfAssisting,
                OtherProfessionals = entity.OtherProfessionals.Select(o => new PAEEProfessionalsAEEResponse()
                {
                    ID = o.ID,
                    PAEEID = o.PAEEID,
                    Professional = o.Professional.GetDisplayName() ?? ""
                }).ToList(),
                MedicalSpecialty = entity.MedicalSpecialty,
                OtherProfessionalsDescription = entity.OtherProfessionalsDescription,
                DifferentiatedActivitiesInAEE = entity.DifferentiatedActivitiesInAEE.Select(o => new PAEEActivitiesAEEResponse()
                {
                    ID = o.ID,
                    PAEEID = o.PAEEID,
                    Activity = o.Activity
                }).ToList(),
                WorkMethodology = entity.WorkMethodology.Select(o => new PAEEWorkMethodologyAEEResponse()
                {
                    ID = o.ID,
                    PAEEID = o.PAEEID,
                    Methodology = o.Methodology
                }).ToList(),
                MaterialResourcesAndEquipment = entity.MaterialResourcesAndEquipment.Select(o => new PAEEMaterialResourcesAEEResponse()
                {
                    ID = o.ID,
                    PAEEID = o.PAEEID,
                    Resource = o.Resource
                }).ToList(),
                CriteriaInEvaluation = entity.CriteriaInEvaluation.Select(o => new PAEEEvaluationCriterionAEEResponse()
                {
                    ID = o.ID,
                    PAEEID = o.PAEEID,
                    Criterion = o.Criterion
                }).ToList(),
                ResourcesAndStrategies = entity.ResourcesAndStrategies.Select(r => new PAEEResourcesAndStrategiesResponse()
                {
                    ID = r.ID,
                    PAEEID = r.PAEEID,
                    Strategy = r.Strategy
                }).ToList(),
                AdditionalObservations = entity.AdditionalObservations,
                DocumentToken = entity.DocumentToken,
                DocumentStatus = entity.DocumentStatus.GetDisplayName() ?? "",
                SignedFile = entity.SignedFile,
                TeacherSign = entity.TeacherSign,
                SignUrl = entity.SignUrl ?? "",
                SignedAt = entity.SignedAt,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
            };
        }

        public static PAEESummaryResponse ToSummaryResponse(
            PAEE entity, UserSummaryResponse? createdBy
        )
        {
            return new PAEESummaryResponse()
            {
                ID = entity.ID,
                Professional = createdBy,
                Student = entity.Student != null ? StudentMapper.ToSummaryResponse(entity.Student) : null,
                Portage = PortageGuideMapper.ToResponse(entity.Portage, createdBy),
                DocumentToken = entity.DocumentToken,
                DocumentStatus = entity.DocumentStatus.GetDisplayName() ?? "",
                SignedFile = entity.SignedFile,
                TeacherSign = entity.TeacherSign,
                SignUrl = entity.SignUrl ?? "",
                SignedAt = entity.SignedAt,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
            };
        }

        public static void UpdateEntity(
            PAEE entity,
            PAEEUpdateRequest model
        )
        {
            entity.AcademicRecord = model.AcademicRecord;
            entity.MedicalReport = model.MedicalReport;
            entity.Medications = model.Medications;
            entity.FollowUp = model.FollowUp;
            entity.OrganizationOfCare = model.OrganizationOfCare.Select(r => new PAEETypeAEE()
            {
                Type = Helpers.GetEnumValueFromDisplayName<TypeAEE>(r.Type)
            }).ToList();
            entity.OtherOrganization = model.OtherOrganization;
            entity.WeeklyFrequency = Helpers.GetEnumValueFromDisplayName<FrequencyAEE>(model.WeeklyFrequency);
            entity.OtherFrequency = model.OtherFrequency;
            entity.AssistingTimePerSession = Helpers.GetEnumValueFromDisplayName<ServiceTimeAEE>(model.AssistingTimePerSession);
            entity.OtherServiceTime = model.OtherServiceTime;
            entity.CompositionOfAssisting = model.CompositionOfAssisting;
            entity.OtherProfessionals = model.OtherProfessionals.Select(p => new PAEEProfessionalsAEE()
            {
                Professional = Helpers.GetEnumValueFromDisplayName<ProfessionalsAEE>(p.Professional)
            }).ToList();
            entity.MedicalSpecialty = model.MedicalSpecialty;
            entity.OtherProfessionalsDescription = model.OtherProfessionalsDescription;
            entity.DifferentiatedActivitiesInAEE = model.DifferentiatedActivitiesInAEE.Select(a => new PAEEActivitiesAEE()
            {
                Activity = a.Activity,
            }).ToList();
            entity.WorkMethodology = model.WorkMethodology.Select(w => new PAEEWorkMethodologyAEE()
            {
                Methodology = w.Methodology,
            }).ToList();
            entity.MaterialResourcesAndEquipment = model.MaterialResourcesAndEquipment.Select(m => new PAEEMaterialResourcesAEE()
            {
                Resource = m.Resource,
            }).ToList();
            entity.CriteriaInEvaluation = model.CriteriaInEvaluation.Select(c => new PAEEEvaluationCriterionAEE()
            {
                Criterion = c.Criterion,
            }).ToList();
            entity.ResourcesAndStrategies = model.ResourcesAndStrategies.Select(r => new PAEEResourcesAndStrategiesAEE()
            {
                Strategy = r.Strategy
            }).ToList();
            entity.AdditionalObservations = model.AdditionalObservations;
            entity.UpdatedAt = DateTime.UtcNow;
        }
    }
}
