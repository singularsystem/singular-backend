using SingularSystem.DTOs.RegularTeachers.NeuropsychopedagogicalQuestionnaires;
using SingularSystem.DTOs.Users;
using SingularSystem.Extensions;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;

namespace SingularSystem.Mappers
{
    public static class RegularTeacherQuestionnaireMapper
    {
        public static RegularTeacherQuestionnaire ToEntity(
            RegularTeacherQuestionnaireRequest model,
            string userId
        )
        {
            return new RegularTeacherQuestionnaire()
            {
                StudentID = model.StudentID,
                UserID = model.UserID.ToString(),
                ClassroomDevelopment = model.ClassroomDevelopment,
                ClassroomBehavior = model.ClassroomBehavior,
                MainDifficulties = model.MainDifficulties,
                LearningCharacteristics = model.LearningCharacteristics,
                ReadingAndWritingDescription = model.ReadingAndWritingDescription,
                LogicalMathematicalReasoningDescription = model.LogicalMathematicalReasoningDescription,
                CompletesSchoolActivities = model.CompletesSchoolActivities,
                CompletesHomework = model.CompletesHomework,
                ReactionToFrustration = model.ReactionToFrustration,
                InappropriateBehaviorDescription = model.InappropriateBehaviorDescription,
                DifficultyWorkingInGroup = model.DifficultyWorkingInGroup,
                DifficultyOrganizingTasks = model.DifficultyOrganizingTasks,
                PeerAcceptance = model.PeerAcceptance,
                BehaviorTraits = model.BehaviorTraits.Select(temp => new RegularTeacherQuestionnaire_BehaviourTrait()
                {
                    BehaviorTrait = Helpers.GetEnumValueFromDisplayName<StudentBehaviorTrait>(temp.BehaviorTrait)
                }).ToList(),
                BehaviorObservations = model.BehaviorObservations,
                DevelopmentLevel = Helpers.GetEnumValueFromDisplayName<StudentDevelopmentComparison>(model.DevelopmentLevel),
                DevelopmentObservations = model.DevelopmentObservations,
                AdditionalComments = model.AdditionalComments,
            };
        }

        public static RegularTeacherQuestionnaireResponse ToResponse(
            RegularTeacherQuestionnaire entity, UserSummaryResponse? createdBy
        )
        {
            return new RegularTeacherQuestionnaireResponse()
            {
                ID = entity.ID,
                Professional = createdBy,
                Student = entity.Student != null ? StudentMapper.ToSummaryResponse(entity.Student) : null,
                ClassroomDevelopment = entity.ClassroomDevelopment,
                ClassroomBehavior = entity.ClassroomBehavior,
                MainDifficulties = entity.MainDifficulties,
                LearningCharacteristics = entity.LearningCharacteristics,
                ReadingAndWritingDescription = entity.ReadingAndWritingDescription,
                LogicalMathematicalReasoningDescription = entity.LogicalMathematicalReasoningDescription,
                CompletesSchoolActivities = entity.CompletesSchoolActivities,
                CompletesHomework = entity.CompletesHomework,
                ReactionToFrustration = entity.ReactionToFrustration,
                InappropriateBehaviorDescription = entity.InappropriateBehaviorDescription,
                DifficultyWorkingInGroup = entity.DifficultyWorkingInGroup,
                DifficultyOrganizingTasks = entity.DifficultyOrganizingTasks,
                PeerAcceptance = entity.PeerAcceptance,
                BehaviorTraits = entity.BehaviorTraits.Select(temp => new BehaviorTraitResponse()
                {
                    ID = temp.ID,
                    NeuropsychopedagogicalQuestionnaireID = temp.RegularTeacherQuestionnaireID,
                    BehaviorTrait = temp.BehaviorTrait.GetDisplayName() ?? ""
                }).ToList(),
                BehaviorObservations = entity.BehaviorObservations,
                DevelopmentLevel = entity.DevelopmentLevel.GetDisplayName() ?? "",
                DevelopmentObservations = entity.DevelopmentObservations,
                AdditionalComments = entity.AdditionalComments,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
            };
        }

        public static void UpdateEntity(
            RegularTeacherQuestionnaire entity,
            RegularTeacherQuestionnaireUpdateRequest model
        )
        {
            entity.ClassroomDevelopment = model.ClassroomDevelopment;
            entity.ClassroomBehavior = model.ClassroomBehavior;
            entity.MainDifficulties = model.MainDifficulties;
            entity.LearningCharacteristics = model.LearningCharacteristics;
            entity.ReadingAndWritingDescription = model.ReadingAndWritingDescription;
            entity.LogicalMathematicalReasoningDescription = model.LogicalMathematicalReasoningDescription;
            entity.CompletesSchoolActivities = model.CompletesSchoolActivities;
            entity.CompletesHomework = model.CompletesHomework;
            entity.ReactionToFrustration = model.ReactionToFrustration;
            entity.InappropriateBehaviorDescription = model.InappropriateBehaviorDescription;
            entity.DifficultyWorkingInGroup = model.DifficultyWorkingInGroup;
            entity.DifficultyOrganizingTasks = model.DifficultyOrganizingTasks;
            entity.PeerAcceptance = model.PeerAcceptance;
            entity.BehaviorTraits = model.BehaviorTraits.Select(temp => new RegularTeacherQuestionnaire_BehaviourTrait()
            {
                BehaviorTrait = Helpers.GetEnumValueFromDisplayName<StudentBehaviorTrait>(temp.BehaviorTrait)
            }).ToList();
            entity.BehaviorObservations = model.BehaviorObservations;
            entity.DevelopmentLevel = Helpers.GetEnumValueFromDisplayName<StudentDevelopmentComparison>(model.DevelopmentLevel);
            entity.DevelopmentObservations = model.DevelopmentObservations;
            entity.AdditionalComments = model.AdditionalComments;
            entity.UpdatedAt = DateTime.UtcNow;
        }
    }
}
