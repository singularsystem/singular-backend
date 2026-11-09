namespace SingularSystem.DTOs.PsychopedagogueReports.MCHAT
{
    public class MCHATQuestionnaireRequest
    {
        public Guid StudentID { get; set; }
        public Guid UserID { get; set; }
        public Guid PsychopedagogueReportID { get; set; }

        public string FollowsPointingGesture { get; set; }
        public string SuspectedDeafness { get; set; }
        public string PretendPlay { get; set; }
        public string ClimbsObjects { get; set; }
        public string UnusualFingerMovementsNearEyes { get; set; }
        public string PointsToRequest { get; set; }
        public string PointsToShowInterest { get; set; }
        public string InterestedInOtherChildren { get; set; }
        public string SharesObjectsForAttention { get; set; }
        public string RespondsToName { get; set; }
        public string SmilesBack { get; set; }
        public string SensitiveToNoise { get; set; }
        public string CanWalk { get; set; }
        public string MaintainsEyeContact { get; set; }
        public string ImitatesActions { get; set; }
        public string FollowsGaze { get; set; }
        public string SeeksAttention { get; set; }
        public string UnderstandsVerbalInstructions { get; set; }
        public string ChecksFacialReactionToEvents { get; set; }
        public string EnjoysMovementActivities { get; set; }
    }
}
