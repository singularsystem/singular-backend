using System.Collections.Generic;

namespace SingularSystem.DTOs;

public class PublicValidateDocumentRequest
{
    public string Token { get; set; } = string.Empty;
}

public class PublicValidationSignatureResponse
{
    public string Role { get; set; } = string.Empty;
    public bool Signed { get; set; }
}

public class PublicValidationResponse
{
    public bool Valid { get; set; }
    public string? DocumentType { get; set; }
    public string? IssuedAt { get; set; }
    public List<PublicValidationSignatureResponse> Signatures { get; set; } = new();
}
