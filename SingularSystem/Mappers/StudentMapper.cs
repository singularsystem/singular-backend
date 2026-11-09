using SingularSystem.DTOs.PsychopedagogueReports.PsychopedagogueQuestionnaires;
using SingularSystem.DTOs.Students;
using SingularSystem.DTOs.Users;
using SingularSystem.Extensions;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;
using System.Data;

namespace SingularSystem.Mappers
{
    public static class StudentMapper
    {
        public static Student ToEntity(
            StudentRequest model, School school
        )
        {
            return new Student()
            {
                FullName = model.FullName,
                Sex = Helpers.GetEnumValueFromDisplayName<Sex>(model.Sex),
                DateOfBirth = model.DateOfBirth,
                Address = model.Address,
                City = Helpers.NormalizeCity(model.City),
                State = Helpers.NormalizeState(model.State),
                Serie = Helpers.GetEnumValueFromDisplayName<Serie>(model.Serie),
                NameOfPersonResponsible = model.NameOfPersonResponsible,
                MaritalStatusParents = Helpers.GetEnumValueFromDisplayName<MaritalStatus>(model.MaritalStatusParents),
                SkinColor = Helpers.GetEnumValueFromDisplayName<SkinColor>(model.SkinColor),
                SUSCard = model.SUSCard == "" ? null : model.SUSCard,
                RG = model.RG == "" ? null : model.RG,
                CPF = model.CPF == "" ? null : model.CPF,
                School = school,
                SchoolID = school.ID
            };
        }

        public static StudentResponse ToResponse(
            Student entity
        )
        {
            return new StudentResponse()
            {
                ID = entity.ID,
                FullName = entity.FullName,
                Sex = entity.Sex.GetDisplayName(),
                DateOfBirth = entity.DateOfBirth,
                City = Helpers.NormalizeCity(entity.City),
                State = Helpers.NormalizeState(entity.State),
                Address = entity.Address,
                Serie = entity.Serie.GetDisplayName(),
                SchoolName = entity.School?.Name ?? "",
                SchoolCity = Helpers.NormalizeCity(entity.School?.City ?? ""),
                SchoolState = Helpers.NormalizeState(entity.School?.State ?? ""),
                NameOfPersonResponsible = entity.NameOfPersonResponsible,
                MaritalStatusParents = entity.MaritalStatusParents.GetDisplayName() ?? "",
                SkinColor = entity.SkinColor.GetDisplayName(),
                SUSCard = entity.SUSCard ?? "",
                RG = entity.RG ?? "",
                CPF = entity.CPF ?? "",
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,

            };
        }

        public static StudentSummaryResponse ToSummaryResponse(
            Student entity
        )
        {
            return new StudentSummaryResponse
            {
                ID = entity.ID,
                FullName = entity.FullName,
                Sex = entity.Sex.GetDisplayName(),
                DateOfBirth = entity.DateOfBirth,
                Serie = entity.Serie.GetDisplayName(),
                Address = entity.Address,
                State = Helpers.NormalizeState(entity.State),
                City = Helpers.NormalizeCity(entity.City),
                SchoolName = entity.School?.Name ?? "",
                SchoolCity = Helpers.NormalizeCity(entity.School?.City ?? ""),
                SchoolState = Helpers.NormalizeState(entity.School?.State ?? ""),
                NameOfPersonResponsible = entity.NameOfPersonResponsible,
            };
        }

        public static StudentDocumentResponse ToDocumentResponse(
            BaseDocument entity
        )
        {
            return new StudentDocumentResponse
            {
                ID = entity.ID,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                UnifiedAt = entity.UnifiedAt,
                DocumentType =
                            entity is RegularTeacherEvaluationDocument ? "RegularTeacherEvaluationDocument" :
                            entity is RegularTeacherQuestionnaire ? "RegularTeacherQuestionnaire" :
                            entity is RegularTeacherScreening ? "RegularTeacherScreening" :
                            entity is PsychopedagogueReport ? "PsychopedagogueReport" :
                            entity is PsychologistReport ? "PsychologistReport" :
                            entity is SocialWorkerReport ? "SocialWorkerReport" :
                            entity is Report ? "Report" :
                            entity is PEI ? "PEI" :
                            entity is PAEE ? "PAEE" :
                            entity is PortageGuide ? "PortageGuide" :
                            entity is PsychologistQuestionnaire ? "PsychologistQuestionnaire" :
                            entity is PsychopedagogueQuestionnaire ? "PsychopedagogueQuestionnaire" :
                            entity is CARSQuestionnaire ? "CARSQuestionnaire" :
                            entity is MCHATQuestionnaire ? "MCHATQuestionnaire" :
                            entity is CognitiveAssessment ? "CognitiveAssessment" :
                            entity is SnapIVQuestionnaire ? "SnapIVQuestionnaire" :
                            entity is AdaptiveBehaviorScaleDI ? "AdaptiveBehaviorScaleDI" :
                            "BaseDocument",
            };
        }

        public static void UpdateEntity(
            Student entity,
            StudentUpdateRequest model, School school
        )
        {
            entity.FullName = model.FullName;
            entity.Sex = Helpers.GetEnumValueFromDisplayName<Sex>(model.Sex);
            entity.DateOfBirth = model.DateOfBirth;
            entity.Address = model.Address;
            entity.State = Helpers.NormalizeState(model.State);
            entity.City = Helpers.NormalizeCity(model.City);
            entity.School = school;
            entity.SchoolID = school.ID;
            entity.NameOfPersonResponsible = model.NameOfPersonResponsible;
            entity.MaritalStatusParents = Helpers.GetEnumValueFromDisplayName<MaritalStatus>(model.MaritalStatusParents);
            entity.SkinColor = Helpers.GetEnumValueFromDisplayName<SkinColor>(model.SkinColor);
            entity.Serie = Helpers.GetEnumValueFromDisplayName<Serie>(model.Serie);
            entity.SUSCard = model.SUSCard == "" ? null : model.SUSCard;
            entity.RG = model.RG == "" ? null : model.RG;
            entity.CPF = model.CPF == "" ? null : model.CPF;
            entity.UpdatedAt = DateTime.UtcNow;
        }
    }
}
