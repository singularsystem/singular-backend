using SingularSystem.DTOs.PsychologistReports.AdaptiveBehaviorScaleDI;
using SingularSystem.DTOs.PsychologistReports.PsychologistQuestionnaire;
using SingularSystem.DTOs.PsychologistReports.SnapIV;
using SingularSystem.DTOs.Students;
using SingularSystem.DTOs.Users;
using SingularSystem.Extensions;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;

namespace SingularSystem.DTOs.PsychologistReports
{
    public class PsychologistReportResponse
    {
        public Guid ID { get; set; }
        public UserSummaryResponse? Professional { get; set; }
        public StudentSummaryResponse? Student { get; set; }
        public Guid? ReportID { get; set; }

        public AdaptiveBehaviorScaleDIResponse? AdaptiveBehaviorScaleDI { get; set; }
        public SnapIVQuestionnaireResponse? SnapIVQuestionnaire { get; set; }
        public PsychologistQuestionnaireResponse? PsychologistQuestionnaire { get; set; }
        public string? Conclusion { get; set; }

        [Display(Name = "Data de Criação")]
        public DateTime CreatedAt { get; set; }
        public string LocalCreatedAt => CreatedAt.ToBrazilianTime().ToString("dd/MM/yyyy HH:mm", new CultureInfo("pt-BR"));

        [Display(Name = "Última atualização")]
        public DateTime? UpdatedAt { get; set; }
        public string LocalUpdatedAt => UpdatedAt?.ToBrazilianTime().ToString("dd/MM/yyyy HH:mm", new CultureInfo("pt-BR")) ?? string.Empty;

        [Display(Name = "Data de unificação")]
        public DateTime? UnifiedAt { get; set; }
        public string LocalUnifiedAt => UnifiedAt?.ToBrazilianTime().ToString("dd/MM/yyyy HH:mm", new CultureInfo("pt-BR")) ?? string.Empty;
    }
}
