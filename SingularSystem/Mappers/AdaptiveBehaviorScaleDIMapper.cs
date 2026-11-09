using SingularSystem.DTOs.PsychologistReports.AdaptiveBehaviorScaleDI;
using SingularSystem.DTOs.Users;
using SingularSystem.Extensions;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;

namespace SingularSystem.Mappers
{
    public static class AdaptiveBehaviorScaleDIMapper
    {
        private static int CountScores(IEnumerable<PlanResponse?> responses)
        {
            int countNotYet = responses.Count(r => r == PlanResponse.NotYet);
            int countWithAssistence = responses.Count(r => r == PlanResponse.WithAssistence);
            int countAlone = responses.Count(r => r == PlanResponse.Alone);

            return (countNotYet * 0) + (countWithAssistence * 1) + (countAlone * 2);
        }

        private static (int scoreConceptualSkills, int scoreSocialSkills, int scorePraticalSkills, int score, int conceptualMax, int socialMax, int praticalMax, int totalMax, double conceptualPercentage, double socialPercentage, double praticalPercentage, double totalPercentage, ScaleDIResult result) CalculateScore(
            IEnumerable<PlanResponse?> conceptualSkills,
            IEnumerable<PlanResponse?> socialSkills,
            IEnumerable<PlanResponse?> praticalSkills
        )
        {
            int scoreConceptualSkills = CountScores(conceptualSkills);
            int scoreSocialSkills = CountScores(socialSkills);
            int scorePraticalSkills = CountScores(praticalSkills);
            int score = scoreConceptualSkills + scoreSocialSkills + scorePraticalSkills;

            int conceptualMax = conceptualSkills.Count() * 2;
            int socialMax = socialSkills.Count() * 2;
            int practicalMax = praticalSkills.Count() * 2;
            int totalMax = conceptualMax + socialMax + practicalMax;

            double conceptualPercentage = (double)scoreConceptualSkills / conceptualMax * 100;
            double socialPercentage = (double)scoreSocialSkills / socialMax * 100;
            double practicalPercentage = (double)scorePraticalSkills / practicalMax * 100;
            double totalPercentage = (double)score / totalMax * 100;

            ScaleDIResult result = score >= 18
                                    ? ScaleDIResult.NoDevelopmentalDelay
                                    : score >= 12
                                        ? ScaleDIResult.MildDevelopmentalDelay
                                        : ScaleDIResult.SevereDevelopmentalDelay;

            return (scoreConceptualSkills, scoreSocialSkills, scorePraticalSkills, score, conceptualMax, socialMax, practicalMax, totalMax, conceptualPercentage, socialPercentage, practicalPercentage, totalPercentage, result);
        }

        private static (IEnumerable<PlanResponse?> conceptualSkills, IEnumerable<PlanResponse?> socialSkills, IEnumerable<PlanResponse?> praticalSkills)
            ExtractResponses(AdaptiveBehaviorScaleDI entity)
        {

            var conceptualSkills = new List<PlanResponse?>
            {
                entity.AgeAppropriateReadingComprehension,
                entity.PerformsBasicMathematicalCalculations,
                entity.UnderstandsTimeConcepts,
                entity.FollowsMultiStepInstructions
            };

            var socialSkills = new List<PlanResponse?>
            {
                entity.InteractsAppropriatelyWithPeers,
                entity.RecognizesOwnAndOthersEmotions,
                entity.FollowsBasicSocialRules,
                entity.ResolvesConflictsAppropriately
            };

            var praticalSkills = new List<PlanResponse?>
            {
                entity.MaintainsPersonalHygieneIndependently,
                entity.OrganizesSchoolMaterials,
                entity.DressesAppropriately,
                entity.ShowsConfidenceInFamiliarEnvironments
            };

            return (conceptualSkills, socialSkills, praticalSkills);
        }

        public static AdaptiveBehaviorScaleDI ToEntity(
            AdaptiveBehaviorScaleDIRequest model,
            string userId
        )
        {
            var entity = new AdaptiveBehaviorScaleDI()
            {
                StudentID = model.StudentID,
                UserID = userId,
                PsychologistReportID = model.PsychologistReportID,
                AgeAppropriateReadingComprehension = Helpers.GetEnumValueFromDisplayName<PlanResponse>(model.AgeAppropriateReadingComprehension),
                PerformsBasicMathematicalCalculations = Helpers.GetEnumValueFromDisplayName<PlanResponse>(model.PerformsBasicMathematicalCalculations),
                UnderstandsTimeConcepts = Helpers.GetEnumValueFromDisplayName<PlanResponse>(model.UnderstandsTimeConcepts),
                FollowsMultiStepInstructions = Helpers.GetEnumValueFromDisplayName<PlanResponse>(model.FollowsMultiStepInstructions),
                InteractsAppropriatelyWithPeers = Helpers.GetEnumValueFromDisplayName<PlanResponse>(model.InteractsAppropriatelyWithPeers),
                RecognizesOwnAndOthersEmotions = Helpers.GetEnumValueFromDisplayName<PlanResponse>(model.RecognizesOwnAndOthersEmotions),
                FollowsBasicSocialRules = Helpers.GetEnumValueFromDisplayName<PlanResponse>(model.FollowsBasicSocialRules),
                ResolvesConflictsAppropriately = Helpers.GetEnumValueFromDisplayName<PlanResponse>(model.ResolvesConflictsAppropriately),
                MaintainsPersonalHygieneIndependently = Helpers.GetEnumValueFromDisplayName<PlanResponse>(model.MaintainsPersonalHygieneIndependently),
                OrganizesSchoolMaterials = Helpers.GetEnumValueFromDisplayName<PlanResponse>(model.OrganizesSchoolMaterials),
                DressesAppropriately = Helpers.GetEnumValueFromDisplayName<PlanResponse>(model.DressesAppropriately),
                ShowsConfidenceInFamiliarEnvironments = Helpers.GetEnumValueFromDisplayName<PlanResponse>(model.ShowsConfidenceInFamiliarEnvironments),
            };

            var (conceptualSkills, socialSkills, praticalSkills) = ExtractResponses(entity);
            var (scoreConceptualSkills, scoreSocialSkills, scorePraticalSkills, score, conceptualMax, socialMax, practicalMax, totalMax, conceptualPercentage, socialPercentage, practicalPercentage, totalPercentage, result) = CalculateScore(conceptualSkills, socialSkills, praticalSkills);

            entity.ScoreConceptualSkills = scoreConceptualSkills;
            entity.PercentageConceptualSkills = conceptualPercentage;
            entity.MaxPointsConceptualSkills = conceptualMax;
            entity.ScoreSocialSkills = scoreSocialSkills;
            entity.PercentageSocialSkills = socialPercentage;
            entity.MaxPointsSocialSkills = socialMax;
            entity.ScorePracticalSkills = scorePraticalSkills;
            entity.PercentagePracticalSkills = practicalPercentage;
            entity.MaxPointsPracticalSkills = practicalMax;
            entity.Score = score;
            entity.Percentage = totalPercentage;
            entity.MaxPoints = totalMax;
            entity.Result = result;

            return entity;
        }

        public static AdaptiveBehaviorScaleDIResponse? ToResponse(
            AdaptiveBehaviorScaleDI entity, UserSummaryResponse? createdBy
        )
        {
            if (entity == null) return null;

            return new AdaptiveBehaviorScaleDIResponse()
            {
                ID = entity.ID,
                Professional = createdBy,
                PsychologistReportID = entity.PsychologistReportID,
                Student = entity.Student != null ? StudentMapper.ToSummaryResponse(entity.Student) : null,
                AgeAppropriateReadingComprehension = entity.AgeAppropriateReadingComprehension.GetDisplayName(),
                PerformsBasicMathematicalCalculations = entity.PerformsBasicMathematicalCalculations.GetDisplayName(),
                UnderstandsTimeConcepts = entity.UnderstandsTimeConcepts.GetDisplayName(),
                FollowsMultiStepInstructions = entity.FollowsMultiStepInstructions.GetDisplayName(),
                InteractsAppropriatelyWithPeers = entity.InteractsAppropriatelyWithPeers.GetDisplayName(),
                RecognizesOwnAndOthersEmotions = entity.RecognizesOwnAndOthersEmotions.GetDisplayName(),
                FollowsBasicSocialRules = entity.FollowsBasicSocialRules.GetDisplayName(),
                ResolvesConflictsAppropriately = entity.ResolvesConflictsAppropriately.GetDisplayName(),
                MaintainsPersonalHygieneIndependently = entity.MaintainsPersonalHygieneIndependently.GetDisplayName(),
                OrganizesSchoolMaterials = entity.OrganizesSchoolMaterials.GetDisplayName(),
                DressesAppropriately = entity.DressesAppropriately.GetDisplayName(),
                ShowsConfidenceInFamiliarEnvironments = entity.ShowsConfidenceInFamiliarEnvironments.GetDisplayName(),
                Score = entity.Score,
                Result = entity.Result.GetDisplayName(),
                ScoreConceptualSkills = entity.ScoreConceptualSkills,
                PercentageConceptualSkills = entity.PercentageConceptualSkills,
                MaxPointsConceptualSkills = entity.MaxPointsConceptualSkills,
                ScoreSocialSkills = entity.ScoreSocialSkills,
                PercentageSocialSkills = entity.PercentageSocialSkills,
                MaxPointsSocialSkills = entity.MaxPointsSocialSkills,
                ScorePracticalSkills = entity.ScorePracticalSkills,
                PercentagePracticalSkills = entity.PercentagePracticalSkills,
                MaxPointsPracticalSkills = entity.MaxPointsPracticalSkills,
                Percentage = entity.Percentage,
                MaxPoints = entity.MaxPoints,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
            };
        }

        public static void UpdateEntity(
            AdaptiveBehaviorScaleDI entity,
            AdaptiveBehaviorScaleDIUpdateRequest model
        )
        {
            entity.AgeAppropriateReadingComprehension = Helpers.GetEnumValueFromDisplayName<PlanResponse>(model.AgeAppropriateReadingComprehension);
            entity.PerformsBasicMathematicalCalculations = Helpers.GetEnumValueFromDisplayName<PlanResponse>(model.PerformsBasicMathematicalCalculations);
            entity.UnderstandsTimeConcepts = Helpers.GetEnumValueFromDisplayName<PlanResponse>(model.UnderstandsTimeConcepts);
            entity.FollowsMultiStepInstructions = Helpers.GetEnumValueFromDisplayName<PlanResponse>(model.FollowsMultiStepInstructions);
            entity.InteractsAppropriatelyWithPeers = Helpers.GetEnumValueFromDisplayName<PlanResponse>(model.InteractsAppropriatelyWithPeers);
            entity.RecognizesOwnAndOthersEmotions = Helpers.GetEnumValueFromDisplayName<PlanResponse>(model.RecognizesOwnAndOthersEmotions);
            entity.FollowsBasicSocialRules = Helpers.GetEnumValueFromDisplayName<PlanResponse>(model.FollowsBasicSocialRules);
            entity.ResolvesConflictsAppropriately = Helpers.GetEnumValueFromDisplayName<PlanResponse>(model.ResolvesConflictsAppropriately);
            entity.MaintainsPersonalHygieneIndependently = Helpers.GetEnumValueFromDisplayName<PlanResponse>(model.MaintainsPersonalHygieneIndependently);
            entity.OrganizesSchoolMaterials = Helpers.GetEnumValueFromDisplayName<PlanResponse>(model.OrganizesSchoolMaterials);
            entity.DressesAppropriately = Helpers.GetEnumValueFromDisplayName<PlanResponse>(model.DressesAppropriately);
            entity.ShowsConfidenceInFamiliarEnvironments = Helpers.GetEnumValueFromDisplayName<PlanResponse>(model.ShowsConfidenceInFamiliarEnvironments);

            var (conceptualSkills, socialSkills, praticalSkills) = ExtractResponses(entity);
            var (scoreConceptualSkills, scoreSocialSkills, scorePraticalSkills, score, conceptualMax, socialMax, practicalMax, totalMax, conceptualPercentage, socialPercentage, practicalPercentage, totalPercentage, result) = CalculateScore(conceptualSkills, socialSkills, praticalSkills);

            entity.ScoreConceptualSkills = scoreConceptualSkills;
            entity.PercentageConceptualSkills = conceptualPercentage;
            entity.MaxPointsConceptualSkills = conceptualMax;
            entity.ScoreSocialSkills = scoreSocialSkills;
            entity.PercentageSocialSkills = socialPercentage;
            entity.MaxPointsSocialSkills = socialMax;
            entity.ScorePracticalSkills = scorePraticalSkills;
            entity.PercentagePracticalSkills = practicalPercentage;
            entity.MaxPointsPracticalSkills = practicalMax;
            entity.Score = score;
            entity.Percentage = totalPercentage;
            entity.MaxPoints = totalMax;
            entity.Result = result;
            entity.UpdatedAt = DateTime.UtcNow;
        }
    }
}
