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
    public class PsychologistReportSubSummaryResponse
    {
        public Guid ID { get; set; }
        public UserSummaryResponse? Professional { get; set; }
        public StudentSummaryResponse? Student { get; set; }
        public bool HasSnapIV { get; set; }
        public bool HasAdaptativeScale { get; set; }
        public bool HasPsychologistQuestionnaire { get; set; }

        [Display(Name = "Data de Criação")]
        public DateTime CreatedAt { get; set; }
        public string LocalCreatedAt => CreatedAt.ToBrazilianTime().ToString("dd/MM/yyyy HH:mm", new CultureInfo("pt-BR"));

        [Display(Name = "Última atualização")]
        public DateTime? UpdatedAt { get; set; }
        public string LocalUpdatedAt => UpdatedAt?.ToBrazilianTime().ToString("dd/MM/yyyy HH:mm", new CultureInfo("pt-BR")) ?? string.Empty;
    }
}
