using SingularSystem.DTOs.PsychopedagogueReports.CARS;
using SingularSystem.DTOs.PsychopedagogueReports.CognitiveAssessments;
using SingularSystem.DTOs.PsychopedagogueReports.MCHAT;
using SingularSystem.DTOs.PsychopedagogueReports.PsychopedagogueQuestionnaires;
using SingularSystem.DTOs.Students;
using SingularSystem.DTOs.Users;
using SingularSystem.Extensions;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace SingularSystem.DTOs.PsychopedagogueReports
{
    public class PsychopedagogueReportSummaryResponse
    {
        public Guid ID { get; set; }
        public UserSummaryResponse? Professional { get; set; }
        public StudentSummaryResponse? Student { get; set; }
        public Guid? ReportID { get; set; }

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
