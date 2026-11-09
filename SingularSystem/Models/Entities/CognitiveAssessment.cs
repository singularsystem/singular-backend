using SingularSystem.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace SingularSystem.Models.Entities
{
    public class CognitiveAssessment : BaseDocument
    {
        [ForeignKey(nameof(PsychopedagogueReport))]
        public Guid? PsychopedagogueReportID { get; set; }
        public PsychopedagogueReport? PsychopedagogueReport { get; set; }

        public Evaluation IdentifiedBasicColors { get; set; } = Evaluation.None;
        public Evaluation IdentifiedBasicShapes { get; set; } = Evaluation.None;
        public Evaluation CompletesAgeAppropriatePuzzles { get; set; } = Evaluation.None;
        public Evaluation GroupsObjectsBySimilarCharacteristics { get; set; } = Evaluation.None;
        public Evaluation IdentifiesVisualSequencePatterns { get; set; } = Evaluation.None;
        public Evaluation UnderstandsSimpleVerbalInstructions { get; set; } = Evaluation.None;
        public Evaluation NamesCommonEverydayObjects { get; set; } = Evaluation.None;
        public Evaluation FormsAgeAppropriateSentences { get; set; } = Evaluation.None;
        public Evaluation AnswersQuestionsAboutFamilyInformation { get; set; } = Evaluation.None;
        public Evaluation UnderstandsBasicAbstractConcepts { get; set; } = Evaluation.None;
        public Evaluation RemembersTwoToThreeStepInstructions { get; set; } = Evaluation.None;
        public Evaluation MaintainsAttentionForAppropriateDuration { get; set; } = Evaluation.None;
        public Evaluation RecognizesFamiliarPeopleAndPlaces { get; set; } = Evaluation.None;
        public Evaluation RecallsRecentEvents { get; set; } = Evaluation.None;
        public Evaluation RepeatsSimpleSequences { get; set; } = Evaluation.None;
        public Evaluation SeeksSolutionsToSimpleEverydayProblems { get; set; } = Evaluation.None;
        public Evaluation DemonstratesCreativityInPlay { get; set; } = Evaluation.None;
        public Evaluation LearnsFromExperience { get; set; } = Evaluation.None;
        public Evaluation UnderstandsCauseAndEffectRelationships { get; set; } = Evaluation.None;
        public Evaluation AdaptsStrategiesWhenSomethingDoesNotWork { get; set; } = Evaluation.None;
        public int Score { get; set; }
        public int ScorePerceptualReasoning { get; set; }
        public int ScoreVerbalReasoning { get; set; }
        public int ScoreMemoryAndAttention { get; set; }
        public int ScoreProblemSolving { get; set; }
        public double Percentage { get; set; }
        public double PercentagePerceptualReasoning { get; set; }
        public double PercentageVerbalReasoning { get; set; }
        public double PercentageMemoryAndAttention { get; set; }
        public double PercentageProblemSolving { get; set; }
        public CognitiveResult Result { get; set; } = CognitiveResult.None;

    }
}