using SingularSystem.DTOs.PsychopedagogueReports.CARS;
using SingularSystem.DTOs.Users;
using SingularSystem.Extensions;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;

namespace SingularSystem.Mappers
{
    public static class CARSQuestionnaireMapper
    {

        public static CARSQuestionnaire ToEntity(
            CARSQuestionnaireRequest model,
            string userId
        )
        {
            CARSResult result;

            if (model.TotalScore == "Até 30")
            {
                result = CARSResult.NoAutism;
            } else if (model.TotalScore == "30 até 36.5")
            {
                result = CARSResult.MildToModerateAutism;
            } else
            {
                result = CARSResult.SevereAutism;
            }

            var entity = new CARSQuestionnaire()
            {
                UserID = userId,
                StudentID = model.StudentID,
                PsychopedagogueReportID = model.PsychopedagogueReportID,
                TotalScore = model.TotalScore,
                Result = result,
            };

            return entity;
        }

        public static CARSQuestionnaireResponse? ToResponse(
            CARSQuestionnaire entity, UserSummaryResponse? createdBy
        )
        {
            if (entity  == null) return null;

            return new CARSQuestionnaireResponse()
            {
                ID = entity.ID,
                Professional = createdBy,
                Student = entity.Student != null ? StudentMapper.ToSummaryResponse(entity.Student) : null,
                PsychopedagogueReportID = entity.PsychopedagogueReportID,
                TotalScore = entity.TotalScore,
                Result = entity.Result.GetDisplayName(),
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
            };
        }

        public static void UpdateEntity(
            CARSQuestionnaire entity,
            CARSQuestionnaireUpdateRequest model
        )
        {
            CARSResult result;

            if (model.TotalScore == "Até 30")
            {
                result = CARSResult.NoAutism;
            }
            else if (model.TotalScore == "30 até 36.5")
            {
                result = CARSResult.MildToModerateAutism;
            }
            else
            {
                result = CARSResult.SevereAutism;
            }

            entity.TotalScore = model.TotalScore;
            entity.Result = result;
            entity.UpdatedAt = DateTime.UtcNow;
        }
    }
}
