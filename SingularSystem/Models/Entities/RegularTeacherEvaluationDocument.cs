using SingularSystem.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace SingularSystem.Models.Entities
{
    public class RegularTeacherEvaluationDocument : BaseDocument
    {
        [ForeignKey(nameof(RegularTeacherScreening))]
        public Guid RegularTeacherScreeningID { get; set; }
        public RegularTeacherScreening RegularTeacherScreening { get; set; }

        [ForeignKey(nameof(RegularTeacherQuestionnaire))]
        public Guid RegularTeacherQuestionnaireID { get; set; }
        public RegularTeacherQuestionnaire RegularTeacherQuestionnaire { get; set; }

        public Status EvaluationStatus { get; set; } = Status.None;
        public string? DocumentToken { get; set; }
        public string? SignedFile { get; set; }
        public Status DocumentStatus { get; set; } = Status.None;
        public SignatureProvider SignatureProvider { get; set; } = SignatureProvider.NeckSign;
        public bool TeacherSign { get; set; } = false;
        public string? SignUrl { get; set; }
        public DateTime? SignedAt { get; set; }
    }
}