using SingularSystem.DTOs.RegularTeachers.NeuropsychopedagogicalQuestionnaires;
using SingularSystem.DTOs.RegularTeachers.RegularTeacherScreenings;
using SingularSystem.DTOs.Students;
using SingularSystem.DTOs.Users;
using SingularSystem.Extensions;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace SingularSystem.DTOs.RegularTeachers.EvaluationDocuments
{
    public class RegularTeacherEvaluationDocumentResponse
    {
        public Guid ID { get; set; }
        public UserSummaryResponse? Professional { get; set; }
        public StudentSummaryResponse? Student { get; set; }

        public RegularTeacherScreeningResponse? Screening { get; set; }
        public RegularTeacherQuestionnaireResponse? Questionnaire { get; set; }

        public string? DocumentToken { get; set; }
        public string? SignedFile { get; set; }
        public string DocumentStatus { get; set; }
        public bool TeacherSign { get; set; }
        public string SignUrl { get; set; }
        public string Status { get; set; } = string.Empty;

        [Display(Name = "Data de Criação")]
        public DateTime CreatedAt { get; set; }
        public string LocalCreatedAt => CreatedAt.ToBrazilianTime().ToString("dd/MM/yyyy HH:mm", new CultureInfo("pt-BR"));

        [Display(Name = "Última atualização")]
        public DateTime? UpdatedAt { get; set; }
        public string LocalUpdatedAt => UpdatedAt?.ToBrazilianTime().ToString("dd/MM/yyyy HH:mm", new CultureInfo("pt-BR")) ?? string.Empty;
        public DateTime? SignedAt { get; set; }
        public string LocalSignedAt => SignedAt?.ToBrazilianTime().ToString("dd/MM/yyyy HH:mm", new CultureInfo("pt-BR")) ?? string.Empty;

        [Display(Name = "Data de unificação")]
        public DateTime? UnifiedAt { get; set; }
        public string LocalUnifiedAt => UnifiedAt?.ToBrazilianTime().ToString("dd/MM/yyyy HH:mm", new CultureInfo("pt-BR")) ?? string.Empty;

    }
}