using System;

namespace SingularSystem.Models.Entities;

public class DocumentValidation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public byte[] TokenHash { get; set; } = Array.Empty<byte>();
    public string TokenProtected { get; set; } = string.Empty;
    public DateTime IssuedAtUtc { get; set; } = DateTime.UtcNow;
}
