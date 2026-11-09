using SingularSystem.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SingularSystem.Models.Entities
{
    public class CARSQuestionnaire : BaseDocument
    {

        [ForeignKey(nameof(PsychopedagogueReport))]
        public Guid? PsychopedagogueReportID { get; set; }
        public PsychopedagogueReport? PsychopedagogueReport { get; set; }

        public string TotalScore { get; set; }
        public CARSResult Result { get; set; } = CARSResult.None;
    }
}