using System;

namespace SingularSystem.Models.Entities
{
    public class TenantProfessionalConfig
    {
        public Guid Id { get; set; }
        public string TenantId { get; set; } = default!;
        public bool PsychologistEnabled { get; set; } = true;
        public bool PsychopedagogueEnabled { get; set; } = true;
        public bool SocialWorkEnabled { get; set; } = false;
        public DateTime UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
