using SingularSystem.DTOs.PsychopedagogueReports.PsychopedagogueQuestionnaires;
using SingularSystem.DTOs.Users;
using SingularSystem.Extensions;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;

namespace SingularSystem.Mappers
{
    public static class PsychopedagogueQuestionnaireMapper
    {
        public static PsychopedagogueQuestionnaire ToEntity(
            PsychopedagogueQuestionnaireRequest model,
            string userId
        )
        {
            var entity = new PsychopedagogueQuestionnaire()
            {
                UserID = userId,
                StudentID = model.StudentID,
                PsychopedagogueReportID = model.PsychopedagogueReportID,
                ComportamentalObservations = model.ComportamentalObservations,
                NotebookObservations = model.NotebookObservations,
                OtherObservations = model.OtherObservations,
                TypeOfDisability = model.TypeOfDisability.Select(td => new PsychopedagogueQuestionnaire_TypeOfDisability
                {
                    Disability = Helpers.GetEnumValueFromDisplayName<TypeOfDisability>(td.Disability)
                }).ToList(),
                OtherTypesOfDisability = model.OtherTypesOfDisability,
                TypeOfDisorder = model.TypeOfDisorder.Select(td => new PsychopedagogueQuestionnaire_TypeOfDisorder
                {
                    Disorder = Helpers.GetEnumValueFromDisplayName<TypeOfDisorder>(td.Disorder)
                }).ToList(),
                JustificationForClassification = model.JustificationForClassification,
                Recommendations = model.Recommendations.Select(r => new PsychopedagogueQuestionnaire_Recommendation
                {
                    Recommendation = Helpers.GetEnumValueFromDisplayName<PsychopedagogueRecommendations>(r.Recommendation)
                }).ToList(),
                OtherRecommendations = model.OtherRecommendations,
            };

            return entity;
        }

        public static PsychopedagogueQuestionnaireResponse? ToResponse(
            PsychopedagogueQuestionnaire entity, UserSummaryResponse? createdBy
        )
        {
            if (entity == null) return null;

            return new PsychopedagogueQuestionnaireResponse()
            {
                ID = entity.ID,
                Professional = createdBy,
                Student = entity.Student != null ? StudentMapper.ToSummaryResponse(entity.Student) : null,
                PsychopedagogueReportID = entity.PsychopedagogueReportID,
                ComportamentalObservations = entity.ComportamentalObservations,
                NotebookObservations = entity.NotebookObservations,
                OtherObservations = entity.OtherObservations,
                TypeOfDisability = entity.TypeOfDisability.Select(td => new PsychopedagogueQuestionnaireTypeOfDisabilityResponse
                {
                    ID = td.ID,
                    PsychopedagogueQuestionnaireID = td.PsychopedagogueQuestionnaireID,
                    Disability = td.Disability.GetDisplayName()
                }).ToList(),
                OtherTypesOfDisability = entity.OtherTypesOfDisability,
                TypeOfDisorder = entity.TypeOfDisorder.Select(td => new PsychopedagogueQuestionnaireTypeOfDisorderResponse
                {
                    ID = td.ID,
                    PsychopedagogueQuestionnaireID = td.PsychopedagogueQuestionnaireID,
                    Disorder = td.Disorder.GetDisplayName()
                }).ToList(),
                JustificationForClassification = entity.JustificationForClassification,
                Recommendations = entity.Recommendations.Select(r => new PsychopedagogueQuestionnaireRecommendationResponse
                {
                    ID = r.ID,
                    PsychopedagogueQuestionnaireID = r.PsychopedagogueQuestionnaireID,
                    Recommendation = r.Recommendation.GetDisplayName()
                }).ToList(),
                OtherRecommendations = entity.OtherRecommendations,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
            };
        }

        public static void UpdateEntity(
            PsychopedagogueQuestionnaire entity,
            PsychopedagogueQuestionnaireUpdateRequest model
        )
        {
            entity.ComportamentalObservations = model.ComportamentalObservations;
            entity.NotebookObservations = model.NotebookObservations;
            entity.OtherObservations = model.OtherObservations;
            entity.TypeOfDisability = model.TypeOfDisability.Select(td => new PsychopedagogueQuestionnaire_TypeOfDisability
            {
                Disability = Helpers.GetEnumValueFromDisplayName<TypeOfDisability>(td.Disability)
            }).ToList();
            entity.OtherTypesOfDisability = model.OtherTypesOfDisability;
            entity.TypeOfDisorder = model.TypeOfDisorder.Select(td => new PsychopedagogueQuestionnaire_TypeOfDisorder
            {
                Disorder = Helpers.GetEnumValueFromDisplayName<TypeOfDisorder>(td.Disorder)
            }).ToList();
            entity.JustificationForClassification = model.JustificationForClassification;
            entity.Recommendations = model.Recommendations.Select(r => new PsychopedagogueQuestionnaire_Recommendation
            {
                Recommendation = Helpers.GetEnumValueFromDisplayName<PsychopedagogueRecommendations>(r.Recommendation)
            }).ToList();
            entity.OtherRecommendations = model.OtherRecommendations;
            entity.UpdatedAt = DateTime.UtcNow;
        }
    }
}
