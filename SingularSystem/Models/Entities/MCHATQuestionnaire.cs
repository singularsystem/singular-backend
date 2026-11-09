using SingularSystem.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace SingularSystem.Models.Entities
{
    public class MCHATQuestionnaire : BaseDocument
    {
        [ForeignKey(nameof(PsychopedagogueReport))]
        public Guid? PsychopedagogueReportID { get; set; }
        public PsychopedagogueReport? PsychopedagogueReport { get; set; }

        public Evaluation FollowsPointingGesture { get; set; }
        public Evaluation SuspectedDeafness { get; set; }
        public Evaluation PretendPlay { get; set; }
        public Evaluation ClimbsObjects { get; set; }
        public Evaluation UnusualFingerMovementsNearEyes { get; set; }
        public Evaluation PointsToRequest { get; set; }
        public Evaluation PointsToShowInterest { get; set; }
        public Evaluation InterestedInOtherChildren { get; set; }
        public Evaluation SharesObjectsForAttention { get; set; }
        public Evaluation RespondsToName { get; set; }
        public Evaluation SmilesBack { get; set; }
        public Evaluation SensitiveToNoise { get; set; }
        public Evaluation CanWalk { get; set; }
        public Evaluation MaintainsEyeContact { get; set; }
        public Evaluation ImitatesActions { get; set; }
        public Evaluation FollowsGaze { get; set; }
        public Evaluation SeeksAttention { get; set; }
        public Evaluation UnderstandsVerbalInstructions { get; set; }
        public Evaluation ChecksFacialReactionToEvents { get; set; }
        public Evaluation EnjoysMovementActivities { get; set; }
        public int Score { get; set; }
        public MCHATResult Result { get; set; }

        public MCHATQuestionnaire()
        {
            var evaluationProperties = this.GetType()
                                             .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                             .Where(p => p.PropertyType == typeof(Evaluation));

            foreach (var prop in evaluationProperties)
            {
                var currentValue = prop.GetValue(this) ?? default(Evaluation);
                if (currentValue == default)
                    prop.SetValue(this, Evaluation.None);
            }

            Result = MCHATResult.None;
        }
    }
}