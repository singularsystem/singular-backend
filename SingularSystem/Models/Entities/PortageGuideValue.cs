using SingularSystem.Models.Enums;

namespace SingularSystem.Models.Entities
{
    public class PortageGuideValue
    {
        public string Category { get; set; }
        public PlanResponse Response { get; set; } = PlanResponse.None;
    }
}
