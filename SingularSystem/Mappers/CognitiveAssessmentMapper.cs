using SingularSystem.DTOs.PsychopedagogueReports.CognitiveAssessments;
using SingularSystem.DTOs.Users;
using SingularSystem.Extensions;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;

namespace SingularSystem.Mappers
{
    public static class CognitiveAssessmentMapper
    {
        private static int CountScores(IEnumerable<Evaluation?> responses)
        {
            int countYes = responses.Count(r => r == Evaluation.Yes);
            int countNot = responses.Count(r => r == Evaluation.Not);
            int countSometimes = responses.Count(r => r == Evaluation.Sometimes);

            return (countNot * 0) + (countSometimes * 1) + (countYes * 2);
        }

        private static CognitiveResult GetCognitiveResult(double percentage)
        {
            if (percentage >= 85)
                return CognitiveResult.WithinExpected;

            if (percentage >= 70)
                return CognitiveResult.Borderline;

            if (percentage >= 55)
                return CognitiveResult.MildIntellectualDisability;

            return CognitiveResult.ModerateToSevereIntellectualDisability;
        }

        private static (int score, int scorePerceptualReasoning, int scoreVerbalReasoning, int scoreMemoryAndAttention, int scoreProblemSolving, double percentage, double percentagePerceptualReasoning, double percentageVerbalReasoning, double percentageMemoryAndAttention, double percentageProblemSolving, CognitiveResult result) CalculateScore(
            IEnumerable<Evaluation?> perceptualReasoning, IEnumerable<Evaluation?> verbalReasoning, IEnumerable<Evaluation?> memoryAndAttention, IEnumerable<Evaluation?> problemSolving
        )
        {
            int scorePerceptualReasoning = CountScores(perceptualReasoning);
            int scoreVerbalReasoning = CountScores(verbalReasoning);
            int scoreMemoryAndAttention = CountScores(memoryAndAttention);
            int scoreProblemSolving = CountScores(problemSolving);
            int score = scorePerceptualReasoning + scoreVerbalReasoning + scoreMemoryAndAttention + scoreProblemSolving;

            double percentagePerceptualReasoning = 
                (double)scorePerceptualReasoning / perceptualReasoning.Count() * 100;

            double percentageVerbalReasoning =
                (double)scoreVerbalReasoning / verbalReasoning.Count() * 100;

            double percentageMemoryAndAttention =
                (double)scoreMemoryAndAttention / memoryAndAttention.Count() * 100;

            double percentageProblemSolving =
                (double)scoreProblemSolving / problemSolving.Count() * 100;

            double percentage =
                ((double)score /
                (perceptualReasoning.Count()
                + verbalReasoning.Count()
                + memoryAndAttention.Count()
                + problemSolving.Count()) * 100) / 2;

            CognitiveResult result = GetCognitiveResult(percentage);

            return (score, scorePerceptualReasoning, scoreVerbalReasoning, scoreMemoryAndAttention, scoreProblemSolving, percentage, percentagePerceptualReasoning, percentageVerbalReasoning, percentageMemoryAndAttention, percentageProblemSolving, result);
        }

        private static (IEnumerable<Evaluation?> perceptualReasoning, IEnumerable<Evaluation?> verbalReasoning, IEnumerable<Evaluation?> memoryAndAttention, IEnumerable<Evaluation?> problemSolving)
            ExtractResponses(CognitiveAssessment entity)
        {
            var perceptualReasoning = new List<Evaluation?>
            {
                entity.IdentifiedBasicColors,
                entity.IdentifiedBasicShapes,
                entity.CompletesAgeAppropriatePuzzles,
                entity.GroupsObjectsBySimilarCharacteristics,
                entity.IdentifiesVisualSequencePatterns,
            };

            var verbalReasoning = new List<Evaluation?>
            {
                entity.UnderstandsSimpleVerbalInstructions,
                entity.NamesCommonEverydayObjects,
                entity.FormsAgeAppropriateSentences,
                entity.AnswersQuestionsAboutFamilyInformation,
                entity.UnderstandsBasicAbstractConcepts,
            };


            var memoryAndAttention = new List<Evaluation?>
            {
                entity.RemembersTwoToThreeStepInstructions,
                entity.MaintainsAttentionForAppropriateDuration,
                entity.RecognizesFamiliarPeopleAndPlaces,
                entity.RecallsRecentEvents,
                entity.RepeatsSimpleSequences,
            };


            var problemSolving = new List<Evaluation?>
            {
                entity.SeeksSolutionsToSimpleEverydayProblems,
                entity.DemonstratesCreativityInPlay,
                entity.LearnsFromExperience,
                entity.UnderstandsCauseAndEffectRelationships,
                entity.AdaptsStrategiesWhenSomethingDoesNotWork,
            };

            return (perceptualReasoning, verbalReasoning, memoryAndAttention, problemSolving);
        }

        public static CognitiveAssessment ToEntity(
            CognitiveAssessmentRequest model,
            string userId
        )
        {
            var entity = new CognitiveAssessment()
            {
                UserID = userId,
                StudentID = model.StudentID,
                PsychopedagogueReportID = model.PsychopedagogueReportID,
                IdentifiedBasicColors = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.IdentifiedBasicColors),
                IdentifiedBasicShapes = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.IdentifiedBasicShapes),
                CompletesAgeAppropriatePuzzles = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.CompletesAgeAppropriatePuzzles),
                GroupsObjectsBySimilarCharacteristics = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.GroupsObjectsBySimilarCharacteristics),
                IdentifiesVisualSequencePatterns = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.IdentifiesVisualSequencePatterns),
                UnderstandsSimpleVerbalInstructions = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.UnderstandsSimpleVerbalInstructions),
                NamesCommonEverydayObjects = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.NamesCommonEverydayObjects),
                FormsAgeAppropriateSentences = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.FormsAgeAppropriateSentences),
                AnswersQuestionsAboutFamilyInformation = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.AnswersQuestionsAboutFamilyInformation),
                UnderstandsBasicAbstractConcepts = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.UnderstandsBasicAbstractConcepts),
                RemembersTwoToThreeStepInstructions = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.RemembersTwoToThreeStepInstructions),
                MaintainsAttentionForAppropriateDuration = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.MaintainsAttentionForAppropriateDuration),
                RecognizesFamiliarPeopleAndPlaces = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.RecognizesFamiliarPeopleAndPlaces),
                RecallsRecentEvents = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.RecallsRecentEvents),
                RepeatsSimpleSequences = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.RepeatsSimpleSequences),
                SeeksSolutionsToSimpleEverydayProblems = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.SeeksSolutionsToSimpleEverydayProblems),
                DemonstratesCreativityInPlay = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.DemonstratesCreativityInPlay),
                LearnsFromExperience = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.LearnsFromExperience),
                UnderstandsCauseAndEffectRelationships = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.UnderstandsCauseAndEffectRelationships),
                AdaptsStrategiesWhenSomethingDoesNotWork = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.AdaptsStrategiesWhenSomethingDoesNotWork),
            };

            var (perceptualReasoning, verbalReasoning, memoryAndAttention, problemSolving) = ExtractResponses(entity);
            var (score, scorePerceptualReasoning, scoreVerbalReasoning, scoreMemoryAndAttention, scoreProblemSolving, percentage, percentagePerceptualReasoning, percentageVerbalReasoning, percentageMemoryAndAttention, percentageProblemSolving, result) = CalculateScore(perceptualReasoning, verbalReasoning, memoryAndAttention, problemSolving);

            entity.Score = score;
            entity.ScorePerceptualReasoning = scorePerceptualReasoning;
            entity.ScoreVerbalReasoning = scoreVerbalReasoning;
            entity.ScoreMemoryAndAttention = scoreMemoryAndAttention;
            entity.ScoreProblemSolving = scoreProblemSolving;
            entity.Percentage = percentage;
            entity.PercentagePerceptualReasoning = percentagePerceptualReasoning;
            entity.PercentageVerbalReasoning = percentageVerbalReasoning;
            entity.PercentageMemoryAndAttention = percentageMemoryAndAttention;
            entity.PercentageProblemSolving = percentageProblemSolving;
            entity.Result = result;

            return entity;
        }

        public static CognitiveAssessmentResponse? ToResponse(
            CognitiveAssessment entity, UserSummaryResponse? createdBy
        )
        {
            if (entity == null) return null;

            return new CognitiveAssessmentResponse()
            {
                ID = entity.ID,
                Professional = createdBy,
                Student = entity.Student != null ? StudentMapper.ToSummaryResponse(entity.Student) : null,
                PsychopedagogueReportID = entity.PsychopedagogueReportID,
                IdentifiedBasicColors = entity.IdentifiedBasicColors.GetDisplayName(),
                IdentifiedBasicShapes = entity.IdentifiedBasicShapes.GetDisplayName(),
                CompletesAgeAppropriatePuzzles = entity.CompletesAgeAppropriatePuzzles.GetDisplayName(),
                GroupsObjectsBySimilarCharacteristics = entity.GroupsObjectsBySimilarCharacteristics.GetDisplayName(),
                IdentifiesVisualSequencePatterns = entity.IdentifiesVisualSequencePatterns.GetDisplayName(),
                UnderstandsSimpleVerbalInstructions = entity.UnderstandsSimpleVerbalInstructions.GetDisplayName(),
                NamesCommonEverydayObjects = entity.NamesCommonEverydayObjects.GetDisplayName(),
                FormsAgeAppropriateSentences = entity.FormsAgeAppropriateSentences.GetDisplayName(),
                AnswersQuestionsAboutFamilyInformation = entity.AnswersQuestionsAboutFamilyInformation.GetDisplayName(),
                UnderstandsBasicAbstractConcepts = entity.UnderstandsBasicAbstractConcepts.GetDisplayName(),
                RemembersTwoToThreeStepInstructions = entity.RemembersTwoToThreeStepInstructions.GetDisplayName(),
                MaintainsAttentionForAppropriateDuration = entity.MaintainsAttentionForAppropriateDuration.GetDisplayName(),
                RecognizesFamiliarPeopleAndPlaces = entity.RecognizesFamiliarPeopleAndPlaces.GetDisplayName(),
                RecallsRecentEvents = entity.RecallsRecentEvents.GetDisplayName(),
                RepeatsSimpleSequences = entity.RepeatsSimpleSequences.GetDisplayName(),
                SeeksSolutionsToSimpleEverydayProblems = entity.SeeksSolutionsToSimpleEverydayProblems.GetDisplayName(),
                DemonstratesCreativityInPlay = entity.DemonstratesCreativityInPlay.GetDisplayName(),
                LearnsFromExperience = entity.LearnsFromExperience.GetDisplayName(),
                UnderstandsCauseAndEffectRelationships = entity.UnderstandsCauseAndEffectRelationships.GetDisplayName(),
                AdaptsStrategiesWhenSomethingDoesNotWork = entity.AdaptsStrategiesWhenSomethingDoesNotWork.GetDisplayName(),
                Score = entity.Score,
                ScorePerceptualReasoning = entity.ScorePerceptualReasoning,
                ScoreVerbalReasoning = entity.ScoreVerbalReasoning,
                ScoreMemoryAndAttention = entity.ScoreMemoryAndAttention,
                ScoreProblemSolving = entity.ScoreProblemSolving,
                Percentage = entity.Percentage,
                PercentagePerceptualReasoning = entity.PercentagePerceptualReasoning,
                PercentageVerbalReasoning = entity.PercentageVerbalReasoning,
                PercentageMemoryAndAttention = entity.PercentageMemoryAndAttention,
                PercentageProblemSolving = entity.PercentageProblemSolving,
                Result = entity.Result.GetDisplayName(),
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
            };
        }

        public static void UpdateEntity(
            CognitiveAssessment entity,
            CognitiveAssessmentUpdateRequest model
        )
        {
            entity.IdentifiedBasicColors = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.IdentifiedBasicColors);
            entity.IdentifiedBasicShapes = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.IdentifiedBasicShapes);
            entity.CompletesAgeAppropriatePuzzles = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.CompletesAgeAppropriatePuzzles);
            entity.GroupsObjectsBySimilarCharacteristics = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.GroupsObjectsBySimilarCharacteristics);
            entity.IdentifiesVisualSequencePatterns = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.IdentifiesVisualSequencePatterns);
            entity.UnderstandsSimpleVerbalInstructions = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.UnderstandsSimpleVerbalInstructions);
            entity.NamesCommonEverydayObjects = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.NamesCommonEverydayObjects);
            entity.FormsAgeAppropriateSentences = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.FormsAgeAppropriateSentences);
            entity.AnswersQuestionsAboutFamilyInformation = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.AnswersQuestionsAboutFamilyInformation);
            entity.UnderstandsBasicAbstractConcepts = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.UnderstandsBasicAbstractConcepts);
            entity.RemembersTwoToThreeStepInstructions = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.RemembersTwoToThreeStepInstructions);
            entity.MaintainsAttentionForAppropriateDuration = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.MaintainsAttentionForAppropriateDuration);
            entity.RecognizesFamiliarPeopleAndPlaces = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.RecognizesFamiliarPeopleAndPlaces);
            entity.RecallsRecentEvents = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.RecallsRecentEvents);
            entity.RepeatsSimpleSequences = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.RepeatsSimpleSequences);
            entity.SeeksSolutionsToSimpleEverydayProblems = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.SeeksSolutionsToSimpleEverydayProblems);
            entity.DemonstratesCreativityInPlay = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.DemonstratesCreativityInPlay);
            entity.LearnsFromExperience = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.LearnsFromExperience);
            entity.UnderstandsCauseAndEffectRelationships = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.UnderstandsCauseAndEffectRelationships);
            entity.AdaptsStrategiesWhenSomethingDoesNotWork = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.AdaptsStrategiesWhenSomethingDoesNotWork);


            var (perceptualReasoning, verbalReasoning, memoryAndAttention, problemSolving) = ExtractResponses(entity);
            var (score, scorePerceptualReasoning, scoreVerbalReasoning, scoreMemoryAndAttention, scoreProblemSolving, percentage, percentagePerceptualReasoning, percentageVerbalReasoning, percentageMemoryAndAttention, percentageProblemSolving, result) = CalculateScore(perceptualReasoning, verbalReasoning, memoryAndAttention, problemSolving);

            entity.Score = score;
            entity.ScorePerceptualReasoning = scorePerceptualReasoning;
            entity.ScoreVerbalReasoning = scoreVerbalReasoning;
            entity.ScoreMemoryAndAttention = scoreMemoryAndAttention;
            entity.ScoreProblemSolving = scoreProblemSolving;
            entity.Percentage = percentage;
            entity.PercentagePerceptualReasoning = percentagePerceptualReasoning;
            entity.PercentageVerbalReasoning = percentageVerbalReasoning;
            entity.PercentageMemoryAndAttention = percentageMemoryAndAttention;
            entity.PercentageProblemSolving = percentageProblemSolving;
            entity.Result = result; 
            entity.UpdatedAt = DateTime.UtcNow;
        }
    }
}
