using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SingularSystem.Models.Entities
{
    public class PsychopedagogueReport : BaseDocument
    {
        [ForeignKey(nameof(Report))]
        public Guid? ReportID { get; set; }
        public Report? Report { get; set; }

        public MCHATQuestionnaire? MCHATQuestionnaire { get; set; }
        public CARSQuestionnaire? CARSQuestionnaire { get; set; }
        public CognitiveAssessment CognitiveAssessment { get; set; }
        public PsychopedagogueQuestionnaire PsychopedagogueQuestionnaire { get; set; }
        public string? Conclusion { get; set; }

    }
}