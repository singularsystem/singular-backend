using System;

namespace SingularSystem.Models.Entities;

public class IssuerProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public Guid? TenantId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? ProfessionalRegistration { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? LogoObjectKey { get; set; }
    public string? LogoContentType { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
