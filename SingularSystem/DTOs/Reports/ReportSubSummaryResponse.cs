using SingularSystem.DTOs.PsychologistReports;
using SingularSystem.DTOs.PsychopedagogueReports;
using SingularSystem.DTOs.SocialWorkerReports;
using SingularSystem.DTOs.Students;
using SingularSystem.Extensions;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace SingularSystem.DTOs.Reports
{
    public class ReportSubSummaryResponse
    {
        public Guid ID { get; set; }
        public StudentSummaryResponse? Student { get; set; }

        public string Status { get; set; }
        public bool HasSocialWorkerReport { get; set; }
        public PsychologistReportSubSummaryResponse? PsychologistReport { get; set; }
        public PsychopedagogueReportSubSummaryResponse? PsychopedagogueReport { get; set; }

        public string? DocumentToken { get; set; }
        public string? SignedFile { get; set; }
        public string DocumentStatus { get; set; }
        public bool SocialWorkerSign { get; set; }
        public bool PsychologistSign { get; set; }
        public bool PsychopedagogueSign { get; set; }
        public string SocialWorkerSignUrl { get; set; }
        public string PsychologistSignUrl { get; set; }
        public string PsychopedagogueSignUrl { get; set; }

        public DateTime? SocialWorkerSignedAt { get; set; }
        public string LocalSocialWorkerSignedAt => SocialWorkerSignedAt?.ToBrazilianTime().ToString("dd/MM/yyyy HH:mm", new CultureInfo("pt-BR")) ?? string.Empty;

        public DateTime? PsychologistSignedAt { get; set; }
        public string LocalPsychologistSignedAt => PsychologistSignedAt?.ToBrazilianTime().ToString("dd/MM/yyyy HH:mm", new CultureInfo("pt-BR")) ?? string.Empty;

        public DateTime? PsychopedagogueSignedAt { get; set; }
        public string LocalPsychopedagogueSignedAt => PsychopedagogueSignedAt?.ToBrazilianTime().ToString("dd/MM/yyyy HH:mm", new CultureInfo("pt-BR")) ?? string.Empty;

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