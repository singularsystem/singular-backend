using SingularSystem.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace SingularSystem.Models.Entities
{
    public class RegularTeacherScreening : BaseDocument
    {
        public RegularTeacherEvaluationDocument? RegularTeacherEvaluationDocument { get; set; }

        public Evaluation LimitedVocabulary { get; set; } = Evaluation.None;
        public string? LimitedVocabularyObservation { get; set; }

        public Evaluation ChildlikeSpeech { get; set; } = Evaluation.None;
        public string? ChildlikeSpeechObservation { get; set; }

        public Evaluation DifficultyFollowingCommands { get; set; } = Evaluation.None;
        public string? DifficultyFollowingCommandsObservation { get; set; }

        public Evaluation DelayedResponse { get; set; } = Evaluation.None;
        public string? DelayedResponseObservation { get; set; }

        public Evaluation AvoidsTalking { get; set; } = Evaluation.None;
        public string? AvoidsTalkingObservation { get; set; }

        public Evaluation DifficultyCopying { get; set; } = Evaluation.None;
        public string? DifficultyCopyingObservation { get; set; }

        public Evaluation ExcessiveSpellingErrors { get; set; } = Evaluation.None;
        public string? ExcessiveSpellingErrorsObservation { get; set; }

        public Evaluation WritingIssues { get; set; } = Evaluation.None;
        public string? WritingIssuesObservation { get; set; }

        public Evaluation PhoneticAssociationDifficulty { get; set; } = Evaluation.None;
        public string? PhoneticAssociationDifficultyObservation { get; set; }

        public WritingLevel WritingLevel { get; set; } = Enums.WritingLevel.None;
        public string? WritingLevelObservation { get; set; }

        public Evaluation RecognizesLettersOrWords { get; set; } = Evaluation.None;
        public string? RecognizesLettersOrWordsObservation { get; set; }

        public Evaluation ReadingFluencyIssues { get; set; } = Evaluation.None;
        public string? ReadingFluencyIssuesObservation { get; set; }

        public Evaluation AnswersReadingQuestions { get; set; } = Evaluation.None;
        public string? AnswersReadingQuestionsObservation { get; set; }

        public Evaluation AvoidsReading { get; set; } = Evaluation.None;
        public string? AvoidsReadingObservation { get; set; }

        public Evaluation NumberRecognitionIssues { get; set; } = Evaluation.None;
        public string? NumberRecognitionIssuesObservation { get; set; }

        public Evaluation ComparisonConceptDifficulty { get; set; } = Evaluation.None;
        public string? ComparisonConceptDifficultyObservation { get; set; }

        public Evaluation BasicOperationDifficulty { get; set; } = Evaluation.None;
        public string? BasicOperationDifficultyObservation { get; set; }

        public Evaluation TimeSpaceConceptDifficulty { get; set; } = Evaluation.None;
        public string? TimeSpaceConceptDifficultyObservation { get; set; }

        public Evaluation DifficultyOrganizingMaterials { get; set; } = Evaluation.None;
        public string? DifficultyOrganizingMaterialsObservation { get; set; }

        public Evaluation NeedsHelpWithBasicActivities { get; set; } = Evaluation.None;
        public string? NeedsHelpWithBasicActivitiesObservation { get; set; }

        public Evaluation DifficultyWithRoutines { get; set; } = Evaluation.None;
        public string? DifficultyWithRoutinesObservation { get; set; }

        public Evaluation DoesNotInitiateTasks { get; set; } = Evaluation.None;
        public string? DoesNotInitiateTasksObservation { get; set; }

        public Evaluation ImmatureBehavior { get; set; } = Evaluation.None;
        public string? ImmatureBehaviorObservation { get; set; }

        public Evaluation PrefersYoungerChildren { get; set; } = Evaluation.None;
        public string? PrefersYoungerChildrenObservation { get; set; }

        public Evaluation BelowAgeLevelPerformance { get; set; } = Evaluation.None;
        public string? BelowAgeLevelPerformanceObservation { get; set; }

        public Evaluation EarlyChildhoodSkillLevel { get; set; } = Evaluation.None;
        public string? EarlyChildhoodSkillLevelObservation { get; set; }

        public Evaluation ForgetsContent { get; set; } = Evaluation.None;
        public string? ForgetsContentObservation { get; set; }

        public Evaluation AttentionDifficulty { get; set; } = Evaluation.None;
        public string? AttentionDifficultyObservation { get; set; }

        public Evaluation EasilyDistracted { get; set; } = Evaluation.None;
        public string? EasilyDistractedObservation { get; set; }

        public Evaluation NeedsRepeatedExplanations { get; set; } = Evaluation.None;
        public string? NeedsRepeatedExplanationsObservation { get; set; }

        public Evaluation FriendshipDifficulty { get; set; } = Evaluation.None;
        public string? FriendshipDifficultyObservation { get; set; }

        public Evaluation EasilyFrustrated { get; set; } = Evaluation.None;
        public string? EasilyFrustratedObservation { get; set; }

        public Evaluation LowSelfEsteem { get; set; } = Evaluation.None;
        public string? LowSelfEsteemObservation { get; set; }

        public Evaluation DependsOnAdults { get; set; } = Evaluation.None;
        public string? DependsOnAdultsObservation { get; set; }

        public Evaluation LowAcademicHistory { get; set; } = Evaluation.None;
        public string? LowAcademicHistoryObservation { get; set; }

        public Evaluation NoProgressWithSupport { get; set; } = Evaluation.None;
        public string? NoProgressWithSupportObservation { get; set; }

        public Evaluation PreviousReportsIndicateImpairment { get; set; } = Evaluation.None;
        public string? PreviousReportsIndicateImpairmentObservation { get; set; }

        public Evaluation CurricularAdaptations { get; set; } = Evaluation.None;
        public string? CurricularAdaptationsObservation { get; set; }

        public string? AdditionalComments { get; set; }

        public RegularTeacherScreening()
        {
            var planResponseProperties = this.GetType()
                                             .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                             .Where(p => p.PropertyType == typeof(Evaluation));

            foreach (var prop in planResponseProperties)
            {
                var currentValue = prop.GetValue(this) ?? default(Evaluation);
                if (currentValue == default)
                    prop.SetValue(this, Evaluation.None);
            }
        }
    }
}