using SingularSystem.Models.Enums;
using System;

namespace SingularSystem.DTOs.RegularTeachers.RegularTeacherScreenings
{
    public class RegularTeacherScreeningRequest
    {
        public Guid StudentID { get; set; }
        public Guid UserID { get; set; }

        public string LimitedVocabulary { get; set; }
        public string? LimitedVocabularyObservation { get; set; }

        public string ChildlikeSpeech { get; set; }
        public string? ChildlikeSpeechObservation { get; set; }

        public string DifficultyFollowingCommands { get; set; }
        public string? DifficultyFollowingCommandsObservation { get; set; }

        public string DelayedResponse { get; set; }
        public string? DelayedResponseObservation { get; set; }

        public string AvoidsTalking { get; set; }
        public string? AvoidsTalkingObservation { get; set; }

        public string DifficultyCopying { get; set; }
        public string? DifficultyCopyingObservation { get; set; }

        public string ExcessiveSpellingErrors { get; set; }
        public string? ExcessiveSpellingErrorsObservation { get; set; }

        public string WritingIssues { get; set; }
        public string? WritingIssuesObservation { get; set; }

        public string PhoneticAssociationDifficulty { get; set; }
        public string? PhoneticAssociationDifficultyObservation { get; set; }

        public string WritingLevel { get; set; }
        public string? WritingLevelObservation { get; set; }

        public string RecognizesLettersOrWords { get; set; }
        public string? RecognizesLettersOrWordsObservation { get; set; }

        public string ReadingFluencyIssues { get; set; }
        public string? ReadingFluencyIssuesObservation { get; set; }

        public string AnswersReadingQuestions { get; set; }
        public string? AnswersReadingQuestionsObservation { get; set; }

        public string AvoidsReading { get; set; }
        public string? AvoidsReadingObservation { get; set; }

        public string NumberRecognitionIssues { get; set; }
        public string? NumberRecognitionIssuesObservation { get; set; }

        public string ComparisonConceptDifficulty { get; set; }
        public string? ComparisonConceptDifficultyObservation { get; set; }

        public string BasicOperationDifficulty { get; set; }
        public string? BasicOperationDifficultyObservation { get; set; }

        public string TimeSpaceConceptDifficulty { get; set; }
        public string? TimeSpaceConceptDifficultyObservation { get; set; }

        public string DifficultyOrganizingMaterials { get; set; }
        public string? DifficultyOrganizingMaterialsObservation { get; set; }

        public string NeedsHelpWithBasicActivities { get; set; }
        public string? NeedsHelpWithBasicActivitiesObservation { get; set; }

        public string DifficultyWithRoutines { get; set; }
        public string? DifficultyWithRoutinesObservation { get; set; }

        public string DoesNotInitiateTasks { get; set; }
        public string? DoesNotInitiateTasksObservation { get; set; }

        public string ImmatureBehavior { get; set; }
        public string? ImmatureBehaviorObservation { get; set; }

        public string PrefersYoungerChildren { get; set; }
        public string? PrefersYoungerChildrenObservation { get; set; }

        public string BelowAgeLevelPerformance { get; set; }
        public string? BelowAgeLevelPerformanceObservation { get; set; }

        public string EarlyChildhoodSkillLevel { get; set; }
        public string? EarlyChildhoodSkillLevelObservation { get; set; }

        public string ForgetsContent { get; set; }
        public string? ForgetsContentObservation { get; set; }

        public string AttentionDifficulty { get; set; }
        public string? AttentionDifficultyObservation { get; set; }

        public string EasilyDistracted { get; set; }
        public string? EasilyDistractedObservation { get; set; }

        public string NeedsRepeatedExplanations { get; set; }
        public string? NeedsRepeatedExplanationsObservation { get; set; }

        public string FriendshipDifficulty { get; set; }
        public string? FriendshipDifficultyObservation { get; set; }

        public string EasilyFrustrated { get; set; }
        public string? EasilyFrustratedObservation { get; set; }

        public string LowSelfEsteem { get; set; }
        public string? LowSelfEsteemObservation { get; set; }

        public string DependsOnAdults { get; set; }
        public string? DependsOnAdultsObservation { get; set; }

        public string LowAcademicHistory { get; set; }
        public string? LowAcademicHistoryObservation { get; set; }

        public string NoProgressWithSupport { get; set; }
        public string? NoProgressWithSupportObservation { get; set; }

        public string PreviousReportsIndicateImpairment { get; set; }
        public string? PreviousReportsIndicateImpairmentObservation { get; set; }

        public string CurricularAdaptations { get; set; }
        public string? CurricularAdaptationsObservation { get; set; }

        public string? AdditionalComments { get; set; }
    }
}
