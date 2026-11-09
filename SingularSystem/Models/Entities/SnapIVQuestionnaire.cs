using SingularSystem.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace SingularSystem.Models.Entities
{
    public class SnapIVQuestionnaire : BaseDocument
    {
        [ForeignKey(nameof(PsychologistReport))]
        public Guid? PsychologistReportID { get; set; }
        public PsychologistReport? PsychologistReport { get; set; }

        public SnapIV HasCarelessMistakes { get; set; }
        public SnapIV HasDifficultySustainingAttention { get; set; }
        public SnapIV AppearsNotToListenWhenSpokenToDirectly { get; set; }
        public SnapIV FailsToFollowThroughOnInstructions { get; set; }
        public SnapIV HasDifficultyOrganizingTasksAndActivities { get; set; }
        public SnapIV AvoidsTasksRequiringSustainedMentalEffort { get; set; }
        public SnapIV OftenLosesItemsNecessaryForTasks { get; set; }
        public SnapIV EasilyDistractedByExternalStimuli { get; set; }
        public SnapIV ForgetfulInDailyActivities { get; set; }
        public SnapIV FidgetsWithHandsOrFeet { get; set; }
        public SnapIV LeavesSeatInSituationsRequiringSitting { get; set; }
        public SnapIV RunsOrClimbsInappropriately { get; set; }
        public SnapIV HasDifficultyPlayingQuietly { get; set; }
        public SnapIV OftenOnTheGoOrActsAsDrivenByAMotor { get; set; }
        public SnapIV TalksExcessively { get; set; }
        public SnapIV BlurtsOutAnswers { get; set; }
        public SnapIV HasDifficultyWaitingTurn { get; set; }
        public SnapIV InterruptsOrIntrudesOnOthers { get; set; }
        public bool Score { get; set; }
        public SnapIVResult Result { get; set; }
        public int DesattentionCount { get; set; }
        public int HyperactivityCount { get; set; }

        public SnapIVQuestionnaire()
        {
            var snapIVProperties = this.GetType()
                                             .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                             .Where(p => p.PropertyType == typeof(SnapIV));

            foreach (var prop in snapIVProperties)
            {
                var currentValue = prop.GetValue(this) ?? default(SnapIV);
                if (currentValue == default)
                    prop.SetValue(this, SnapIV.None);
            }

            Score = false;
            Result = SnapIVResult.None;
        }
    }
}