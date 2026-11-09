using SingularSystem.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace SingularSystem.Models.Entities
{
    public class PsychologistQuestionnaire : BaseDocument
    {
        [ForeignKey(nameof(PsychologistReport))]
        public Guid? PsychologistReportID { get; set; }
        public PsychologistReport? PsychologistReport { get; set; }

        public Evaluation LimitedVocabulary { get; set; }
        public string? LimitedVocabularyObservation { get; set; }

        public Evaluation ChildlikeSpeech { get; set; }
        public string? ChildlikeSpeechObservation { get; set; }

        public Evaluation DifficultyFollowingCommands { get; set; }
        public string? DifficultyFollowingCommandsObservation { get; set; }

        public Evaluation DelayedResponse { get; set; }
        public string? DelayedResponseObservation { get; set; }

        public Evaluation AvoidsTalking { get; set; }
        public string? AvoidsTalkingObservation { get; set; }

        public Evaluation DifficultyCopying { get; set; }
        public string? DifficultyCopyingObservation { get; set; }

        public Evaluation ExcessiveSpellingErrors { get; set; }
        public string? ExcessiveSpellingErrorsObservation { get; set; }

        public Evaluation WritingIssues { get; set; }
        public string? WritingIssuesObservation { get; set; }

        public Evaluation PhoneticAssociationDifficulty { get; set; }
        public string? PhoneticAssociationDifficultyObservation { get; set; }

        public WritingLevel WritingLevel { get; set; } = Enums.WritingLevel.None;
        public string? WritingLevelObservation { get; set; }

        public Evaluation RecognizesLettersOrWords { get; set; }
        public string? RecognizesLettersOrWordsObservation { get; set; }

        public Evaluation ReadingFluencyIssues { get; set; }
        public string? ReadingFluencyIssuesObservation { get; set; }

        public Evaluation AnswersReadingQuestions { get; set; }
        public string? AnswersReadingQuestionsObservation { get; set; }

        public Evaluation AvoidsReading { get; set; }
        public string? AvoidsReadingObservation { get; set; }

        public Evaluation NumberRecognitionIssues { get; set; }
        public string? NumberRecognitionIssuesObservation { get; set; }

        public Evaluation ComparisonConceptDifficulty { get; set; }
        public string? ComparisonConceptDifficultyObservation { get; set; }

        public Evaluation BasicOperationDifficulty { get; set; }
        public string? BasicOperationDifficultyObservation { get; set; }

        public Evaluation TimeSpaceConceptDifficulty { get; set; }
        public string? TimeSpaceConceptDifficultyObservation { get; set; }

        public Evaluation DifficultyOrganizingMaterials { get; set; }
        public string? DifficultyOrganizingMaterialsObservation { get; set; }

        public Evaluation NeedsHelpWithBasicActivities { get; set; }
        public string? NeedsHelpWithBasicActivitiesObservation { get; set; }

        public Evaluation DifficultyWithRoutines { get; set; }
        public string? DifficultyWithRoutinesObservation { get; set; }

        public Evaluation DoesNotInitiateTasks { get; set; }
        public string? DoesNotInitiateTasksObservation { get; set; }

        public Evaluation ImmatureBehavior { get; set; }
        public string? ImmatureBehaviorObservation { get; set; }

        public Evaluation PrefersYoungerChildren { get; set; }
        public string? PrefersYoungerChildrenObservation { get; set; }

        public Evaluation BelowAgeLevelPerformance { get; set; }
        public string? BelowAgeLevelPerformanceObservation { get; set; }

        public Evaluation EarlyChildhoodSkillLevel { get; set; }
        public string? EarlyChildhoodSkillLevelObservation { get; set; }

        public Evaluation ForgetsContent { get; set; }
        public string? ForgetsContentObservation { get; set; }

        public Evaluation AttentionDifficulty { get; set; }
        public string? AttentionDifficultyObservation { get; set; }

        public Evaluation EasilyDistracted { get; set; }
        public string? EasilyDistractedObservation { get; set; }

        public Evaluation NeedsRepeatedExplanations { get; set; }
        public string? NeedsRepeatedExplanationsObservation { get; set; }

        public Evaluation FriendshipDifficulty { get; set; }
        public string? FriendshipDifficultyObservation { get; set; }

        public Evaluation EasilyFrustrated { get; set; }
        public string? EasilyFrustratedObservation { get; set; }

        public Evaluation LowSelfEsteem { get; set; }
        public string? LowSelfEsteemObservation { get; set; }

        public Evaluation DependsOnAdults { get; set; }
        public string? DependsOnAdultsObservation { get; set; }

        public string? AdditionalComments { get; set; }

        public PsychologistQuestionnaire()
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