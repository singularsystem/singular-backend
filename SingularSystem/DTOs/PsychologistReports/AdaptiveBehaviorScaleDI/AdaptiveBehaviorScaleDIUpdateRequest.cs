namespace SingularSystem.DTOs.PsychologistReports.AdaptiveBehaviorScaleDI
{
    public class AdaptiveBehaviorScaleDIUpdateRequest
    {
        public Guid ID { get; set; }
        public Guid StudentID { get; set; }
        public Guid UserID { get; set; }
        public Guid PsychologistReportID { get; set; }

        public string AgeAppropriateReadingComprehension { get; set; }
        public string PerformsBasicMathematicalCalculations { get; set; }
        public string UnderstandsTimeConcepts { get; set; }
        public string FollowsMultiStepInstructions { get; set; }
        public string InteractsAppropriatelyWithPeers { get; set; }
        public string RecognizesOwnAndOthersEmotions { get; set; }
        public string FollowsBasicSocialRules { get; set; }
        public string ResolvesConflictsAppropriately { get; set; }
        public string MaintainsPersonalHygieneIndependently { get; set; }
        public string OrganizesSchoolMaterials { get; set; }
        public string DressesAppropriately { get; set; }
        public string ShowsConfidenceInFamiliarEnvironments { get; set; }
    }
}
