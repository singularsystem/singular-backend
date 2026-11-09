using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SingularSystem.Models.Entities
{
    public class PsychologistReport : BaseDocument
    {
        [ForeignKey(nameof(Report))]
        public Guid? ReportID { get; set; }
        public Report? Report { get; set; }

        public AdaptiveBehaviorScaleDI AdaptiveBehaviorScaleDI { get; set; }
        public SnapIVQuestionnaire SnapIVQuestionnaire { get; set; }
        public PsychologistQuestionnaire PsychologistQuestionnaire { get; set; }
        public string? Conclusion { get; set; }

    }
}