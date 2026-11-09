using SingularSystem.DTOs.Students;
using SingularSystem.DTOs.Users;
using SingularSystem.Extensions;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace SingularSystem.DTOs.PsychologistReports.AdaptiveBehaviorScaleDI
{
    public class AdaptiveBehaviorScaleDIResponse
    {
        public Guid ID { get; set; }
        public UserSummaryResponse? Professional { get; set; }
        public StudentSummaryResponse? Student { get; set; }
        public Guid? PsychologistReportID { get; set; }

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
        public string Result { get; set; }
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

        [Display(Name = "Data de Criação")]
        public DateTime CreatedAt { get; set; }
        public string LocalCreatedAt => CreatedAt.ToBrazilianTime().ToString("dd/MM/yyyy HH:mm", new CultureInfo("pt-BR"));

        [Display(Name = "Última atualização")]
        public DateTime? UpdatedAt { get; set; }
        public string LocalUpdatedAt => UpdatedAt?.ToBrazilianTime().ToString("dd/MM/yyyy HH:mm", new CultureInfo("pt-BR")) ?? string.Empty;
    }
}
