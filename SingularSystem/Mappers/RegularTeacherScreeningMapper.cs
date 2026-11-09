using SingularSystem.DTOs.RegularTeachers.RegularTeacherScreenings;
using SingularSystem.DTOs.Users;
using SingularSystem.Extensions;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;

namespace SingularSystem.Mappers
{
    public static class RegularTeacherScreeningMapper
    {
        public static RegularTeacherScreening ToEntity(
            RegularTeacherScreeningRequest model,
            string userId
        )
        {
            return new RegularTeacherScreening
            {
                StudentID = model.StudentID,
                UserID = userId,
                LimitedVocabulary = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.LimitedVocabulary),
                LimitedVocabularyObservation = model.LimitedVocabularyObservation,
                ChildlikeSpeech = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.ChildlikeSpeech),
                ChildlikeSpeechObservation = model.ChildlikeSpeechObservation,
                DifficultyFollowingCommands = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.DifficultyFollowingCommands),
                DifficultyFollowingCommandsObservation = model.DifficultyFollowingCommandsObservation,
                DelayedResponse = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.DelayedResponse),
                DelayedResponseObservation = model.DelayedResponseObservation,
                AvoidsTalking = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.AvoidsTalking),
                AvoidsTalkingObservation = model.AvoidsTalkingObservation,
                DifficultyCopying = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.DifficultyCopying),
                DifficultyCopyingObservation = model.DifficultyCopyingObservation,
                ExcessiveSpellingErrors = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.ExcessiveSpellingErrors),
                ExcessiveSpellingErrorsObservation = model.ExcessiveSpellingErrorsObservation,
                WritingIssues = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.WritingIssues),
                WritingIssuesObservation = model.WritingIssuesObservation,
                PhoneticAssociationDifficulty = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.PhoneticAssociationDifficulty),
                PhoneticAssociationDifficultyObservation = model.PhoneticAssociationDifficultyObservation,
                WritingLevel = Helpers.GetEnumValueFromDisplayName<WritingLevel>(model.WritingLevel),
                WritingLevelObservation = model.WritingLevelObservation,
                RecognizesLettersOrWords = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.RecognizesLettersOrWords),
                RecognizesLettersOrWordsObservation = model.RecognizesLettersOrWordsObservation,
                ReadingFluencyIssues = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.ReadingFluencyIssues),
                ReadingFluencyIssuesObservation = model.ReadingFluencyIssuesObservation,
                AnswersReadingQuestions = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.AnswersReadingQuestions),
                AnswersReadingQuestionsObservation = model.AnswersReadingQuestionsObservation,
                AvoidsReading = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.AvoidsReading),
                AvoidsReadingObservation = model.AvoidsReadingObservation,
                NumberRecognitionIssues = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.NumberRecognitionIssues),
                NumberRecognitionIssuesObservation = model.NumberRecognitionIssuesObservation,
                ComparisonConceptDifficulty = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.ComparisonConceptDifficulty),
                ComparisonConceptDifficultyObservation = model.ComparisonConceptDifficultyObservation,
                BasicOperationDifficulty = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.BasicOperationDifficulty),
                BasicOperationDifficultyObservation = model.BasicOperationDifficultyObservation,
                TimeSpaceConceptDifficulty = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.TimeSpaceConceptDifficulty),
                TimeSpaceConceptDifficultyObservation = model.TimeSpaceConceptDifficultyObservation,
                DifficultyOrganizingMaterials = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.DifficultyOrganizingMaterials),
                DifficultyOrganizingMaterialsObservation = model.DifficultyOrganizingMaterialsObservation,
                NeedsHelpWithBasicActivities = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.NeedsHelpWithBasicActivities),
                NeedsHelpWithBasicActivitiesObservation = model.NeedsHelpWithBasicActivitiesObservation,
                DifficultyWithRoutines = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.DifficultyWithRoutines),
                DifficultyWithRoutinesObservation = model.DifficultyWithRoutinesObservation,
                DoesNotInitiateTasks = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.DoesNotInitiateTasks),
                DoesNotInitiateTasksObservation = model.DoesNotInitiateTasksObservation,
                ImmatureBehavior = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.ImmatureBehavior),
                ImmatureBehaviorObservation = model.ImmatureBehaviorObservation,
                PrefersYoungerChildren = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.PrefersYoungerChildren),
                PrefersYoungerChildrenObservation = model.PrefersYoungerChildrenObservation,
                BelowAgeLevelPerformance = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.BelowAgeLevelPerformance),
                BelowAgeLevelPerformanceObservation = model.BelowAgeLevelPerformanceObservation,
                EarlyChildhoodSkillLevel = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.EarlyChildhoodSkillLevel),
                EarlyChildhoodSkillLevelObservation = model.EarlyChildhoodSkillLevelObservation,
                ForgetsContent = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.ForgetsContent),
                ForgetsContentObservation = model.ForgetsContentObservation,
                AttentionDifficulty = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.AttentionDifficulty),
                AttentionDifficultyObservation = model.AttentionDifficultyObservation,
                EasilyDistracted = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.EasilyDistracted),
                EasilyDistractedObservation = model.EasilyDistractedObservation,
                NeedsRepeatedExplanations = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.NeedsRepeatedExplanations),
                NeedsRepeatedExplanationsObservation = model.NeedsRepeatedExplanationsObservation,
                FriendshipDifficulty = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.FriendshipDifficulty),
                FriendshipDifficultyObservation = model.FriendshipDifficultyObservation,
                EasilyFrustrated = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.EasilyFrustrated),
                EasilyFrustratedObservation = model.EasilyFrustratedObservation,
                LowSelfEsteem = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.LowSelfEsteem),
                LowSelfEsteemObservation = model.LowSelfEsteemObservation,
                DependsOnAdults = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.DependsOnAdults),
                DependsOnAdultsObservation = model.DependsOnAdultsObservation,
                LowAcademicHistory = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.LowAcademicHistory),
                LowAcademicHistoryObservation = model.LowAcademicHistoryObservation,
                NoProgressWithSupport = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.NoProgressWithSupport),
                NoProgressWithSupportObservation = model.NoProgressWithSupportObservation,
                PreviousReportsIndicateImpairment = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.PreviousReportsIndicateImpairment),
                PreviousReportsIndicateImpairmentObservation = model.PreviousReportsIndicateImpairmentObservation,
                CurricularAdaptations = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.CurricularAdaptations),
                CurricularAdaptationsObservation = model.CurricularAdaptationsObservation,
                AdditionalComments = model.AdditionalComments,
            };
        }

        public static RegularTeacherScreeningResponse ToResponse(
            RegularTeacherScreening entity, UserSummaryResponse? createdBy
        )
        {
            return new RegularTeacherScreeningResponse
            {
                ID = entity.ID,
                Professional = createdBy,
                Student = entity.Student != null ? StudentMapper.ToSummaryResponse(entity.Student) : null,
                LimitedVocabulary = entity.LimitedVocabulary.GetDisplayName(),
                LimitedVocabularyObservation = entity.LimitedVocabularyObservation,
                ChildlikeSpeech = entity.ChildlikeSpeech.GetDisplayName(),
                ChildlikeSpeechObservation = entity.ChildlikeSpeechObservation,
                DifficultyFollowingCommands = entity.DifficultyFollowingCommands.GetDisplayName(),
                DifficultyFollowingCommandsObservation = entity.DifficultyFollowingCommandsObservation,
                DelayedResponse = entity.DelayedResponse.GetDisplayName(),
                DelayedResponseObservation = entity.DelayedResponseObservation,
                AvoidsTalking = entity.AvoidsTalking.GetDisplayName(),
                AvoidsTalkingObservation = entity.AvoidsTalkingObservation,
                DifficultyCopying = entity.DifficultyCopying.GetDisplayName(),
                DifficultyCopyingObservation = entity.DifficultyCopyingObservation,
                ExcessiveSpellingErrors = entity.ExcessiveSpellingErrors.GetDisplayName(),
                ExcessiveSpellingErrorsObservation = entity.ExcessiveSpellingErrorsObservation,
                WritingIssues = entity.WritingIssues.GetDisplayName(),
                WritingIssuesObservation = entity.WritingIssuesObservation,
                PhoneticAssociationDifficulty = entity.PhoneticAssociationDifficulty.GetDisplayName(),
                PhoneticAssociationDifficultyObservation = entity.PhoneticAssociationDifficultyObservation,
                WritingLevel = entity.WritingLevel.GetDisplayName(),
                WritingLevelObservation = entity.WritingLevelObservation,
                RecognizesLettersOrWords = entity.RecognizesLettersOrWords.GetDisplayName(),
                RecognizesLettersOrWordsObservation = entity.RecognizesLettersOrWordsObservation,
                ReadingFluencyIssues = entity.ReadingFluencyIssues.GetDisplayName(),
                ReadingFluencyIssuesObservation = entity.ReadingFluencyIssuesObservation,
                AnswersReadingQuestions = entity.AnswersReadingQuestions.GetDisplayName(),
                AnswersReadingQuestionsObservation = entity.AnswersReadingQuestionsObservation,
                AvoidsReading = entity.AvoidsReading.GetDisplayName(),
                AvoidsReadingObservation = entity.AvoidsReadingObservation,
                NumberRecognitionIssues = entity.NumberRecognitionIssues.GetDisplayName(),
                NumberRecognitionIssuesObservation = entity.NumberRecognitionIssuesObservation,
                ComparisonConceptDifficulty = entity.ComparisonConceptDifficulty.GetDisplayName(),
                ComparisonConceptDifficultyObservation = entity.ComparisonConceptDifficultyObservation,
                BasicOperationDifficulty = entity.BasicOperationDifficulty.GetDisplayName(),
                BasicOperationDifficultyObservation = entity.BasicOperationDifficultyObservation,
                TimeSpaceConceptDifficulty = entity.TimeSpaceConceptDifficulty.GetDisplayName(),
                TimeSpaceConceptDifficultyObservation = entity.TimeSpaceConceptDifficultyObservation,
                DifficultyOrganizingMaterials = entity.DifficultyOrganizingMaterials.GetDisplayName(),
                DifficultyOrganizingMaterialsObservation = entity.DifficultyOrganizingMaterialsObservation,
                NeedsHelpWithBasicActivities = entity.NeedsHelpWithBasicActivities.GetDisplayName(),
                NeedsHelpWithBasicActivitiesObservation = entity.NeedsHelpWithBasicActivitiesObservation,
                DifficultyWithRoutines = entity.DifficultyWithRoutines.GetDisplayName(),
                DifficultyWithRoutinesObservation = entity.DifficultyWithRoutinesObservation,
                DoesNotInitiateTasks = entity.DoesNotInitiateTasks.GetDisplayName(),
                DoesNotInitiateTasksObservation = entity.DoesNotInitiateTasksObservation,
                ImmatureBehavior = entity.ImmatureBehavior.GetDisplayName(),
                ImmatureBehaviorObservation = entity.ImmatureBehaviorObservation,
                PrefersYoungerChildren = entity.PrefersYoungerChildren.GetDisplayName(),
                PrefersYoungerChildrenObservation = entity.PrefersYoungerChildrenObservation,
                BelowAgeLevelPerformance = entity.BelowAgeLevelPerformance.GetDisplayName(),
                BelowAgeLevelPerformanceObservation = entity.BelowAgeLevelPerformanceObservation,
                EarlyChildhoodSkillLevel = entity.EarlyChildhoodSkillLevel.GetDisplayName(),
                EarlyChildhoodSkillLevelObservation = entity.EarlyChildhoodSkillLevelObservation,
                ForgetsContent = entity.ForgetsContent.GetDisplayName(),
                ForgetsContentObservation = entity.ForgetsContentObservation,
                AttentionDifficulty = entity.AttentionDifficulty.GetDisplayName(),
                AttentionDifficultyObservation = entity.AttentionDifficultyObservation,
                EasilyDistracted = entity.EasilyDistracted.GetDisplayName(),
                EasilyDistractedObservation = entity.EasilyDistractedObservation,
                NeedsRepeatedExplanations = entity.NeedsRepeatedExplanations.GetDisplayName()    ,
                NeedsRepeatedExplanationsObservation = entity.NeedsRepeatedExplanationsObservation,
                FriendshipDifficulty = entity.FriendshipDifficulty.GetDisplayName(),
                FriendshipDifficultyObservation = entity.FriendshipDifficultyObservation,
                EasilyFrustrated = entity.EasilyFrustrated.GetDisplayName(),
                EasilyFrustratedObservation = entity.EasilyFrustratedObservation,
                LowSelfEsteem = entity.LowSelfEsteem.GetDisplayName(),
                LowSelfEsteemObservation = entity.LowSelfEsteemObservation,
                DependsOnAdults = entity.DependsOnAdults.GetDisplayName(),
                DependsOnAdultsObservation = entity.DependsOnAdultsObservation,
                LowAcademicHistory = entity.LowAcademicHistory.GetDisplayName(),
                LowAcademicHistoryObservation = entity.LowAcademicHistoryObservation,
                NoProgressWithSupport = entity.NoProgressWithSupport.GetDisplayName(),
                NoProgressWithSupportObservation = entity.NoProgressWithSupportObservation,
                PreviousReportsIndicateImpairment = entity.PreviousReportsIndicateImpairment.GetDisplayName(),
                PreviousReportsIndicateImpairmentObservation = entity.PreviousReportsIndicateImpairmentObservation,
                CurricularAdaptations = entity.CurricularAdaptations.GetDisplayName(),
                CurricularAdaptationsObservation = entity.CurricularAdaptationsObservation,
                AdditionalComments = entity.AdditionalComments,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
            };
        }

        public static void UpdateEntity(
            RegularTeacherScreening entity,
            RegularTeacherScreeningUpdateRequest model
        )
        {
            entity.LimitedVocabulary = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.LimitedVocabulary);
            entity.LimitedVocabularyObservation = model.LimitedVocabularyObservation;
            entity.ChildlikeSpeech = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.ChildlikeSpeech);
            entity.ChildlikeSpeechObservation = model.ChildlikeSpeechObservation;
            entity.DifficultyFollowingCommands = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.DifficultyFollowingCommands);
            entity.DifficultyFollowingCommandsObservation = model.DifficultyFollowingCommandsObservation;
            entity.DelayedResponse = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.DelayedResponse);
            entity.DelayedResponseObservation = model.DelayedResponseObservation;
            entity.AvoidsTalking = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.AvoidsTalking);
            entity.AvoidsTalkingObservation = model.AvoidsTalkingObservation;
            entity.DifficultyCopying = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.DifficultyCopying);
            entity.DifficultyCopyingObservation = model.DifficultyCopyingObservation;
            entity.ExcessiveSpellingErrors = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.ExcessiveSpellingErrors);
            entity.ExcessiveSpellingErrorsObservation = model.ExcessiveSpellingErrorsObservation;
            entity.WritingIssues = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.WritingIssues);
            entity.WritingIssuesObservation = model.WritingIssuesObservation;
            entity.PhoneticAssociationDifficulty = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.PhoneticAssociationDifficulty);
            entity.PhoneticAssociationDifficultyObservation = model.PhoneticAssociationDifficultyObservation;
            entity.WritingLevel = Helpers.GetEnumValueFromDisplayName<WritingLevel>(model.WritingLevel);
            entity.WritingLevelObservation = model.WritingLevelObservation;
            entity.RecognizesLettersOrWords = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.RecognizesLettersOrWords);
            entity.RecognizesLettersOrWordsObservation = model.RecognizesLettersOrWordsObservation;
            entity.ReadingFluencyIssues = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.ReadingFluencyIssues);
            entity.ReadingFluencyIssuesObservation = model.ReadingFluencyIssuesObservation;
            entity.AnswersReadingQuestions = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.AnswersReadingQuestions);
            entity.AnswersReadingQuestionsObservation = model.AnswersReadingQuestionsObservation;
            entity.AvoidsReading = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.AvoidsReading);
            entity.AvoidsReadingObservation = model.AvoidsReadingObservation;
            entity.NumberRecognitionIssues = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.NumberRecognitionIssues);
            entity.NumberRecognitionIssuesObservation = model.NumberRecognitionIssuesObservation;
            entity.ComparisonConceptDifficulty = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.ComparisonConceptDifficulty);
            entity.ComparisonConceptDifficultyObservation = model.ComparisonConceptDifficultyObservation;
            entity.BasicOperationDifficulty = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.BasicOperationDifficulty);
            entity.BasicOperationDifficultyObservation = model.BasicOperationDifficultyObservation;
            entity.TimeSpaceConceptDifficulty = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.TimeSpaceConceptDifficulty);
            entity.TimeSpaceConceptDifficultyObservation = model.TimeSpaceConceptDifficultyObservation;
            entity.DifficultyOrganizingMaterials = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.DifficultyOrganizingMaterials);
            entity.DifficultyOrganizingMaterialsObservation = model.DifficultyOrganizingMaterialsObservation;
            entity.NeedsHelpWithBasicActivities = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.NeedsHelpWithBasicActivities);
            entity.NeedsHelpWithBasicActivitiesObservation = model.NeedsHelpWithBasicActivitiesObservation;
            entity.DifficultyWithRoutines = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.DifficultyWithRoutines);
            entity.DifficultyWithRoutinesObservation = model.DifficultyWithRoutinesObservation;
            entity.DoesNotInitiateTasks = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.DoesNotInitiateTasks);
            entity.DoesNotInitiateTasksObservation = model.DoesNotInitiateTasksObservation;
            entity.ImmatureBehavior = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.ImmatureBehavior);
            entity.ImmatureBehaviorObservation = model.ImmatureBehaviorObservation;
            entity.PrefersYoungerChildren = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.PrefersYoungerChildren);
            entity.PrefersYoungerChildrenObservation = model.PrefersYoungerChildrenObservation;
            entity.BelowAgeLevelPerformance = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.BelowAgeLevelPerformance);
            entity.BelowAgeLevelPerformanceObservation = model.BelowAgeLevelPerformanceObservation;
            entity.EarlyChildhoodSkillLevel = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.EarlyChildhoodSkillLevel);
            entity.EarlyChildhoodSkillLevelObservation = model.EarlyChildhoodSkillLevelObservation;
            entity.ForgetsContent = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.ForgetsContent);
            entity.ForgetsContentObservation = model.ForgetsContentObservation;
            entity.AttentionDifficulty = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.AttentionDifficulty);
            entity.AttentionDifficultyObservation = model.AttentionDifficultyObservation;
            entity.EasilyDistracted = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.EasilyDistracted);
            entity.EasilyDistractedObservation = model.EasilyDistractedObservation;
            entity.NeedsRepeatedExplanations = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.NeedsRepeatedExplanations);
            entity.NeedsRepeatedExplanationsObservation = model.NeedsRepeatedExplanationsObservation;
            entity.FriendshipDifficulty = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.FriendshipDifficulty);
            entity.FriendshipDifficultyObservation = model.FriendshipDifficultyObservation;
            entity.EasilyFrustrated = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.EasilyFrustrated);
            entity.EasilyFrustratedObservation = model.EasilyFrustratedObservation;
            entity.LowSelfEsteem = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.LowSelfEsteem);
            entity.LowSelfEsteemObservation = model.LowSelfEsteemObservation;
            entity.DependsOnAdults = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.DependsOnAdults);
            entity.DependsOnAdultsObservation = model.DependsOnAdultsObservation;
            entity.LowAcademicHistory = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.LowAcademicHistory);
            entity.LowAcademicHistoryObservation = model.LowAcademicHistoryObservation;
            entity.NoProgressWithSupport = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.NoProgressWithSupport);
            entity.NoProgressWithSupportObservation = model.NoProgressWithSupportObservation;
            entity.PreviousReportsIndicateImpairment = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.PreviousReportsIndicateImpairment);
            entity.PreviousReportsIndicateImpairmentObservation = model.PreviousReportsIndicateImpairmentObservation;
            entity.CurricularAdaptations = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.CurricularAdaptations);
            entity.CurricularAdaptationsObservation = model.CurricularAdaptationsObservation;
            entity.AdditionalComments = model.AdditionalComments;
            entity.UpdatedAt = DateTime.UtcNow;
        }
    }
}
