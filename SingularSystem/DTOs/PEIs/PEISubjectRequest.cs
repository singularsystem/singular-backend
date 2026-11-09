using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;

namespace SingularSystem.DTOs.PEIs
{
    public class PEISubjectRequest
    {
        public string Subject { get; set; }
        public ICollection<PEIResponseRequest> Responses { get; set; } = new List<PEIResponseRequest>();
    }
}
