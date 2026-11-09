using SingularSystem.DTOs.Students;
using SingularSystem.DTOs.Users;
using SingularSystem.Extensions;
using SingularSystem.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace SingularSystem.DTOs.PsychopedagogueReports.CognitiveAssessments
{
    public class CognitiveAssessmentResponse
    {
        public Guid ID { get; set; }
        public UserSummaryResponse? Professional { get; set; }
        public StudentSummaryResponse? Student { get; set; }
        public Guid? PsychopedagogueReportID { get; set; }
        
        public string IdentifiedBasicColors { get; set; }
        public string IdentifiedBasicShapes { get; set; }
        public string CompletesAgeAppropriatePuzzles { get; set; }
        public string GroupsObjectsBySimilarCharacteristics { get; set; }
        public string IdentifiesVisualSequencePatterns { get; set; }
        public string UnderstandsSimpleVerbalInstructions { get; set; }
        public string NamesCommonEverydayObjects { get; set; }
        public string FormsAgeAppropriateSentences { get; set; }
        public string AnswersQuestionsAboutFamilyInformation { get; set; }
        public string UnderstandsBasicAbstractConcepts { get; set; }
        public string RemembersTwoToThreeStepInstructions { get; set; }
        public string MaintainsAttentionForAppropriateDuration { get; set; }
        public string RecognizesFamiliarPeopleAndPlaces { get; set; }
        public string RecallsRecentEvents { get; set; }
        public string RepeatsSimpleSequences { get; set; }
        public string SeeksSolutionsToSimpleEverydayProblems { get; set; }
        public string DemonstratesCreativityInPlay { get; set; }
        public string LearnsFromExperience { get; set; }
        public string UnderstandsCauseAndEffectRelationships { get; set; }
        public string AdaptsStrategiesWhenSomethingDoesNotWork { get; set; }
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
        public string Result { get; set; } 

        [Display(Name = "Data de Criação")]
        public DateTime CreatedAt { get; set; }
        public string LocalCreatedAt => CreatedAt.ToBrazilianTime().ToString("dd/MM/yyyy HH:mm", new CultureInfo("pt-BR"));

        [Display(Name = "Última atualização")]
        public DateTime? UpdatedAt { get; set; }
        public string LocalUpdatedAt => UpdatedAt?.ToBrazilianTime().ToString("dd/MM/yyyy HH:mm", new CultureInfo("pt-BR")) ?? string.Empty;
    }
}
