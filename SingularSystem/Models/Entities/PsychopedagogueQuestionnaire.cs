using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SingularSystem.Models.Entities
{
    public class PsychopedagogueQuestionnaire : BaseDocument
    {
        [ForeignKey(nameof(PsychopedagogueReport))]
        public Guid? PsychopedagogueReportID { get; set; }
        public PsychopedagogueReport? PsychopedagogueReport { get; set; }

        public string ComportamentalObservations { get; set; }
        public string NotebookObservations { get; set; }
        public string? OtherObservations { get; set; }
        public ICollection<PsychopedagogueQuestionnaire_TypeOfDisability> TypeOfDisability { get; set; } = new List<PsychopedagogueQuestionnaire_TypeOfDisability>();
        public string? OtherTypesOfDisability { get; set; }
        public ICollection<PsychopedagogueQuestionnaire_TypeOfDisorder> TypeOfDisorder { get; set; } = new List<PsychopedagogueQuestionnaire_TypeOfDisorder>();
        public string JustificationForClassification { get; set; }
        public ICollection<PsychopedagogueQuestionnaire_Recommendation> Recommendations { get; set; } = new List<PsychopedagogueQuestionnaire_Recommendation>();
        public string? OtherRecommendations { get; set; }
    }
}