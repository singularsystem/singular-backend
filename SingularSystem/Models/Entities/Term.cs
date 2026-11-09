namespace SingularSystem.Models.Entities
{
    using System.ComponentModel.DataAnnotations;

    public class Term
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, MaxLength(80)]
        public string Key { get; set; } = "AEE_TERMO_RESPONSABILIDADE"; 

        [Required, MaxLength(40)]
        public string Version { get; set; } = "2026-02-02"; 

        [Required, MaxLength(200)]
        public string Title { get; set; } = "Termo de Responsabilidade e Uso do Sistema AEE";

        [Required]
        public string Content { get; set; } = string.Empty; 

        [Required, MaxLength(20)]
        public string ContentFormat { get; set; } = "markdown";

        public bool IsActive { get; set; } = true;

        public DateTime PublishedAt { get; set; } = DateTime.UtcNow;

        [Required, MaxLength(64)]
        public string ContentHash { get; set; } = string.Empty;

        public ICollection<UserTermAcceptance> Acceptances { get; set; } = new List<UserTermAcceptance>();
    }
}
