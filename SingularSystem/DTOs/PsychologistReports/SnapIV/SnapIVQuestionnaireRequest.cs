namespace SingularSystem.DTOs.PsychologistReports.SnapIV
{
    public class SnapIVQuestionnaireRequest
    {
        public Guid StudentID { get; set; }
        public Guid UserID { get; set; }
        public Guid PsychologistReportID { get; set; }

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
    }
}
