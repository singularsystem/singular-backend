using SingularSystem.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace SingularSystem.Models.Entities
{
    public class AdaptiveBehaviorScaleDI : BaseDocument
    {
        [ForeignKey(nameof(PsychologistReport))]
        public Guid? PsychologistReportID { get; set; }
        public PsychologistReport? PsychologistReport { get; set; }

        public PlanResponse AgeAppropriateReadingComprehension { get; set; }
        public PlanResponse PerformsBasicMathematicalCalculations { get; set; }
        public PlanResponse UnderstandsTimeConcepts { get; set; }
        public PlanResponse FollowsMultiStepInstructions { get; set; }
        public PlanResponse InteractsAppropriatelyWithPeers { get; set; }
        public PlanResponse RecognizesOwnAndOthersEmotions { get; set; }
        public PlanResponse FollowsBasicSocialRules { get; set; }
        public PlanResponse ResolvesConflictsAppropriately { get; set; }
        public PlanResponse MaintainsPersonalHygieneIndependently { get; set; }
        public PlanResponse OrganizesSchoolMaterials { get; set; }
        public PlanResponse DressesAppropriately { get; set; }
        public PlanResponse ShowsConfidenceInFamiliarEnvironments { get; set; }

        public int ScoreConceptualSkills { get; set; }
        public double PercentageConceptualSkills { get; set; }
        public int MaxPointsConceptualSkills { get; set; }
        public int ScoreSocialSkills { get; set; }
        public double PercentageSocialSkills { get; set; }
        public int MaxPointsSocialSkills { get; set; }
        public int ScorePracticalSkills { get; set; }
        public double PercentagePracticalSkills { get; set; }
        public int MaxPointsPracticalSkills { get; set; }
        public int Score { get; set; }
        public double Percentage { get; set; }
        public int MaxPoints { get; set; }
        public ScaleDIResult Result { get; set; }

        public AdaptiveBehaviorScaleDI()
        {
            var planResponseProperties = this.GetType()
                                             .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                             .Where(p => p.PropertyType == typeof(PlanResponse));

            foreach (var prop in planResponseProperties)
            {
                var currentValue = prop.GetValue(this) ?? default(PlanResponse);
                if (currentValue == default)
                    prop.SetValue(this, PlanResponse.None);
            }

            Score = 0;
            Result = ScaleDIResult.None;
        }
    }
}