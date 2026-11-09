using System.ComponentModel.DataAnnotations;

namespace SingularSystem.Models.Entities
{
    public class UserTermAcceptance
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid TermId { get; set; }
        public Term Term { get; set; } = null!;

        [Required]
        public DateTime AcceptedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(64)]
        public string? IpAddress { get; set; }

        [MaxLength(300)]
        public string? UserAgent { get; set; }

        [Required, MaxLength(64)]
        public string AcceptanceHash { get; set; } = string.Empty;

        public string? RenderedContentSnapshot { get; set; }
    }
}
