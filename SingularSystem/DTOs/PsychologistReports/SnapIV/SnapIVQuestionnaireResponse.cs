using SingularSystem.DTOs.Students;
using SingularSystem.DTOs.Users;
using SingularSystem.Extensions;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace SingularSystem.DTOs.PsychologistReports.SnapIV
{
    public class SnapIVQuestionnaireResponse
    {
        public Guid ID { get; set; }
        public UserSummaryResponse? Professional { get; set; }
        public StudentSummaryResponse? Student { get; set; }
        public Guid? PsychologistReportID { get; set; }

        public string HasCarelessMistakes { get; set; }
        public string HasDifficultySustainingAttention { get; set; }
        public string AppearsNotToListenWhenSpokenToDirectly { get; set; }
        public string FailsToFollowThroughOnInstructions { get; set; }
        public string HasDifficultyOrganizingTasksAndActivities { get; set; }
        public string AvoidsTasksRequiringSustainedMentalEffort { get; set; }
        public string OftenLosesItemsNecessaryForTasks { get; set; }
        public string EasilyDistractedByExternalStimuli { get; set; }
        public string ForgetfulInDailyActivities { get; set; }
        public string FidgetsWithHandsOrFeet { get; set; }
        public string LeavesSeatInSituationsRequiringSitting { get; set; }
        public string RunsOrClimbsInappropriately { get; set; }
        public string HasDifficultyPlayingQuietly { get; set; }
        public string OftenOnTheGoOrActsAsDrivenByAMotor { get; set; }
        public string TalksExcessively { get; set; }
        public string BlurtsOutAnswers { get; set; }
        public string HasDifficultyWaitingTurn { get; set; }
        public string InterruptsOrIntrudesOnOthers { get; set; }
        public string Score { get; set; }
        public string Result { get; set; }
        public int DesattentionCount { get; set; }
        public int HyperactivityCount { get; set; }

        [Display(Name = "Data de Criação")]
        public DateTime CreatedAt { get; set; }
        public string LocalCreatedAt => CreatedAt.ToBrazilianTime().ToString("dd/MM/yyyy HH:mm", new CultureInfo("pt-BR"));

        [Display(Name = "Última atualização")]
        public DateTime? UpdatedAt { get; set; }
        public string LocalUpdatedAt => UpdatedAt?.ToBrazilianTime().ToString("dd/MM/yyyy HH:mm", new CultureInfo("pt-BR")) ?? string.Empty;
    }
}
