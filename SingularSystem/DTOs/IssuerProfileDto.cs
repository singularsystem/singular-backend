using System;

namespace SingularSystem.DTOs;

public sealed record IssuerProfileResponse(
    Guid Id,
    string DisplayName,
    string? ProfessionalRegistration,
    string? Phone,
    string? Email,
    string? LogoUrl,
    DateTime UpdatedAt);

public sealed record UpdateIssuerProfileRequest(
    string DisplayName,
    string? ProfessionalRegistration,
    string? Phone,
    string? Email);

public sealed record IssuerLogoUploadResponse(string LogoUrl);
