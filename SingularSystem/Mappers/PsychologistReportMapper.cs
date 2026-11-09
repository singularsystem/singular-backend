using SingularSystem.DTOs.PsychologistReports;
using SingularSystem.DTOs.PsychopedagogueReports;
using SingularSystem.DTOs.SocialWorkerReports;
using SingularSystem.DTOs.Users;
using SingularSystem.Extensions;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;

namespace SingularSystem.Mappers
{
    public static class PsychologistReportMapper
    {
        public static PsychologistReportResponse ToResponse(
            PsychologistReport entity, UserSummaryResponse? createdBy, Student? reportStudent = null
        )
        {
            var unifiedAt = entity.UnifiedAt;
            var studentToUse = entity.Student ?? reportStudent;
            
            if (unifiedAt == null)
            {
                bool hasAdaptiveScale = entity.AdaptiveBehaviorScaleDI != null;
                if (studentToUse != null)
                {
                    int age = DateTime.UtcNow.Year - studentToUse.DateOfBirth.Year;
                    if (studentToUse.DateOfBirth.Date > DateTime.UtcNow.Date.AddYears(-age)) age--;
                    if (age < 6) hasAdaptiveScale = true;
                }
                
                if (hasAdaptiveScale && entity.PsychologistQuestionnaire != null && entity.SnapIVQuestionnaire != null)
                {
                    unifiedAt = DateTime.UtcNow;
                }
            }

            return new PsychologistReportResponse()
            {
                ID = entity.ID,
                Professional = createdBy,
                ReportID = entity.ReportID,
                Student = studentToUse != null ? StudentMapper.ToSummaryResponse(studentToUse) : null,
                AdaptiveBehaviorScaleDI = AdaptiveBehaviorScaleDIMapper.ToResponse(entity.AdaptiveBehaviorScaleDI, null),
                PsychologistQuestionnaire = PsychologistQuestionnaireMapper.ToResponse(entity.PsychologistQuestionnaire, null),
                SnapIVQuestionnaire = SnapIVQuestionnaireMapper.ToResponse(entity.SnapIVQuestionnaire, null),
                Conclusion = entity.Conclusion,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                UnifiedAt = unifiedAt,
            };
        }

        public static PsychologistReportSummaryResponse ToSummaryResponse(
            PsychologistReport entity, UserSummaryResponse? createdBy, Student? reportStudent = null
        )
        {
            var unifiedAt = entity.UnifiedAt;
            var studentToUse = entity.Student ?? reportStudent;
            
            if (unifiedAt == null)
            {
                bool hasAdaptiveScale = entity.AdaptiveBehaviorScaleDI != null;
                if (studentToUse != null)
                {
                    int age = DateTime.UtcNow.Year - studentToUse.DateOfBirth.Year;
                    if (studentToUse.DateOfBirth.Date > DateTime.UtcNow.Date.AddYears(-age)) age--;
                    if (age < 6) hasAdaptiveScale = true;
                }
                
                if (hasAdaptiveScale && entity.PsychologistQuestionnaire != null && entity.SnapIVQuestionnaire != null)
                {
                    unifiedAt = DateTime.UtcNow;
                }
            }

            return new PsychologistReportSummaryResponse()
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
