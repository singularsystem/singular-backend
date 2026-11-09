using SingularSystem.DTOs.PsychopedagogueReports;
using SingularSystem.DTOs.SocialWorkerReports;
using SingularSystem.DTOs.Users;
using SingularSystem.Extensions;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;

namespace SingularSystem.Mappers
{
    public static class PsychopedagogueReportMapper
    {
        public static PsychopedagogueReportResponse ToResponse(
            PsychopedagogueReport entity, UserSummaryResponse? createdBy, Student? reportStudent = null
        )
        {
            var mchat = entity.MCHATQuestionnaire;
            var cars = entity.CARSQuestionnaire;

            var unifiedAt = entity.UnifiedAt;
            var studentToUse = entity.Student ?? reportStudent;
            
            if (unifiedAt == null)
            {
                bool hasCognitive = entity.CognitiveAssessment != null;
                if (studentToUse != null)
                {
                    int age = DateTime.UtcNow.Year - studentToUse.DateOfBirth.Year;
                    if (studentToUse.DateOfBirth.Date > DateTime.UtcNow.Date.AddYears(-age)) age--;
                    if (age < 6) hasCognitive = true;
                }
                
                if (hasCognitive && entity.PsychopedagogueQuestionnaire != null && (mchat != null || cars != null))
                {
                    unifiedAt = DateTime.UtcNow;
                }
            }

            return new PsychopedagogueReportResponse()
            {
                ID = entity.ID,
                Professional = createdBy,
                ReportID = entity.ReportID,
                Student = studentToUse != null ? StudentMapper.ToSummaryResponse(studentToUse) : null,
                MCHATQuestionnaire = mchat != null ? MCHATQuestionnaireMapper.ToResponse(mchat, null) : null,
                CARSQuestionnaire = cars != null ? CARSQuestionnaireMapper.ToResponse(cars, null) : null,
                CognitiveAssessment = CognitiveAssessmentMapper.ToResponse(entity.CognitiveAssessment, null),
                PsychopedagogueQuestionnaire = PsychopedagogueQuestionnaireMapper.ToResponse(entity.PsychopedagogueQuestionnaire, null),
                Conclusion = entity.Conclusion,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                UnifiedAt = unifiedAt,
            };
        }

        public static PsychopedagogueReportSummaryResponse ToSummaryResponse(
            PsychopedagogueReport entity, UserSummaryResponse? createdBy, Student? reportStudent = null
        )
        {
            var unifiedAt = entity.UnifiedAt;
            var studentToUse = entity.Student ?? reportStudent;
            
            if (unifiedAt == null)
            {
                bool hasCognitive = entity.CognitiveAssessment != null;
                if (studentToUse != null)
                {
                    int age = DateTime.UtcNow.Year - studentToUse.DateOfBirth.Year;
                    if (studentToUse.DateOfBirth.Date > DateTime.UtcNow.Date.AddYears(-age)) age--;
                    if (age < 6) hasCognitive = true;
                }
                
                if (hasCognitive && entity.PsychopedagogueQuestionnaire != null && (entity.MCHATQuestionnaire != null || entity.CARSQuestionnaire != null))
                {
                    unifiedAt = DateTime.UtcNow;
                }
            }

            return new PsychopedagogueReportSummaryResponse()
            {
                ID = entity.ID,
                Professional = createdBy,
                Student = studentToUse != null ? StudentMapper.ToSummaryResponse(studentToUse) : null,
                ReportID = entity.ReportID,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                UnifiedAt = unifiedAt,
            };
        }
    }
}
