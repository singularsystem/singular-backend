using SingularSystem.DTOs.Students;
using SingularSystem.DTOs.Users;
using SingularSystem.Extensions;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace SingularSystem.DTOs.PsychopedagogueReports.PsychopedagogueQuestionnaires
{
    public class PsychopedagogueQuestionnaireResponse
    {
        public Guid ID { get; set; }
        public UserSummaryResponse? Professional { get; set; }
        public StudentSummaryResponse? Student { get; set; }
        public Guid? PsychopedagogueReportID { get; set; }
        
        public string ComportamentalObservations { get; set; }
        public string NotebookObservations { get; set; }
        public string? OtherObservations { get; set; }
        public ICollection<PsychopedagogueQuestionnaireTypeOfDisabilityResponse> TypeOfDisability { get; set; } = new List<PsychopedagogueQuestionnaireTypeOfDisabilityResponse>();
        public string? OtherTypesOfDisability { get; set; }
        public ICollection<PsychopedagogueQuestionnaireTypeOfDisorderResponse> TypeOfDisorder { get; set; } = new List<PsychopedagogueQuestionnaireTypeOfDisorderResponse>();
        public string JustificationForClassification { get; set; }
        public ICollection<PsychopedagogueQuestionnaireRecommendationResponse> Recommendations { get; set; } = new List<PsychopedagogueQuestionnaireRecommendationResponse>();
        public string? OtherRecommendations { get; set; }

        [Display(Name = "Data de Criação")]
        public DateTime CreatedAt { get; set; }
        public string LocalCreatedAt => CreatedAt.ToBrazilianTime().ToString("dd/MM/yyyy HH:mm", new CultureInfo("pt-BR"));

        [Display(Name = "Última atualização")]
        public DateTime? UpdatedAt { get; set; }
        public string LocalUpdatedAt => UpdatedAt?.ToBrazilianTime().ToString("dd/MM/yyyy HH:mm", new CultureInfo("pt-BR")) ?? string.Empty;
    }
}
