using SingularSystem.DTOs.PEIs;
using SingularSystem.DTOs.PsychopedagogueReports.CognitiveAssessments;
using SingularSystem.DTOs.Users;
using SingularSystem.Extensions;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;

namespace SingularSystem.Mappers
{
    public static class PEIMapper
    {
        public static PEI ToEntity(PEIRequest model, string userId)
        {
            return new PEI
            {
                StudentID = model.StudentID,
                UserID = userId,
                SchoolYear = int.Parse(model.SchoolYear),
                Serie = Helpers.GetEnumValueFromDisplayName<Serie>(model.Serie),
                AcademicRecord = model.AcademicRecord ?? "",
                MedicalReport = model.MedicalReport ?? "",
                Medications = model.Medications ?? "",
                FollowUp = model.FollowUp ?? "",
                AdditionalObservations = model.AdditionalObservations ?? "",
                Subjects = model.Subjects?.Select(subject => new PEI_Subject
                {
                    Subject = Helpers.GetEnumValueFromDisplayName<Subject>(subject.Subject),
                    Responses = subject.Responses?.Select(response => new PEI_Response
                    {
                        Key = response.Key,
                        Category = response.Category,
                        Response = Helpers.GetEnumValueFromDisplayName<PlanResponse>(response.Response)
                    }).ToList() ?? new List<PEI_Response>()
                }).ToList() ?? new List<PEI_Subject>()
            };
        }

        public static PEI_Subject ToSubjectEntity(PEISubjectRequest model, Guid peiId)
        {
            return new PEI_Subject
            {
                PEIID = peiId,
                Subject = Helpers.GetEnumValueFromDisplayName<Subject>(model.Subject),
                Responses = model.Responses.Select(response => new PEI_Response
                {
                    Key = response.Key,
                    Category = response.Category,
                    Response = Helpers.GetEnumValueFromDisplayName<PlanResponse>(response.Response)
                }).ToList() ?? new List<PEI_Response>()
            };
        }


        public static PEIResponse ToResponse(
            PEI entity, UserSummaryResponse? createdBy
        )
        {
            return new PEIResponse()
            {
                ID = entity.ID,
                Student = entity.Student != null ? StudentMapper.ToSummaryResponse(entity.Student) : null,
                Professional = createdBy,
                Serie = entity.Serie.GetDisplayName(),
                SchoolYear = entity.SchoolYear.ToString(),
                AcademicRecord = entity.AcademicRecord ?? "",
                MedicalReport = entity.MedicalReport ?? "",
                Medications = entity.Medications ?? "",
                FollowUp = entity.FollowUp ?? "",
                AdditionalObservations = entity.AdditionalObservations ?? "",
                Subjects = entity.Subjects.Select(temp => new PEISubjectResponse()
                {
                    ID = temp.ID,
                    PEIID = temp.PEIID,
                    Subject = temp.Subject.GetDisplayName(),
                    Responses = temp.Responses.Select(r => new PEIResponseResponse()
                    {
                        ID = r.ID,
                        Key = r.Key,
                        Category = r.Category,
                        Response = r.Response.GetDisplayName(),
                        PEISubjectID = r.PEISubjectID
                    }).ToList()
                }).ToList(),
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
            PEI entity,
            PEIUpdateRequest model
        )
        {
            entity.SchoolYear = int.Parse(model.SchoolYear);
            entity.Serie = Helpers.GetEnumValueFromDisplayName<Serie>(model.Serie);
            entity.AcademicRecord = model.AcademicRecord ?? "";
            entity.MedicalReport = model.MedicalReport ?? "";
            entity.Medications = model.Medications ?? "";
            entity.FollowUp = model.FollowUp ?? "";
            entity.AdditionalObservations = model.AdditionalObservations ?? "";
            entity.Subjects = model.Subjects?.Select(subject => new PEI_Subject
            {
                Subject = Helpers.GetEnumValueFromDisplayName<Subject>(subject.Subject),
                Responses = subject.Responses?.Select(response => new PEI_Response
                {
                    Key = response.Key,
                    Category = response.Category,
                    Response = Helpers.GetEnumValueFromDisplayName<PlanResponse>(response.Response)
                }).ToList() ?? new List<PEI_Response>()
            }).ToList() ?? new List<PEI_Subject>();
            entity.UpdatedAt = DateTime.UtcNow;
        }
    }
}
