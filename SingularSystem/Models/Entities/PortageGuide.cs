using SingularSystem.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SingularSystem.Models.Entities
{
    public class PortageGuide : BaseDocument
    {
        public AgeRange PortageAgeRange { get; set; } = AgeRange.None;
        public virtual ICollection<PortageGuideResponse> Responses { get; set; } = new List<PortageGuideResponse>();
        public PAEE? PAEE { get; set; }
    }
}
