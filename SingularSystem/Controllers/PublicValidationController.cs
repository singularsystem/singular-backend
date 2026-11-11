using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using SingularSystem.DTOs;
using SingularSystem.Interfaces;

namespace SingularSystem.Controllers;

[ApiController]
[Route("api/v1/public/documents")]
[EnableRateLimiting("public-validation")]
public class PublicValidationController : ControllerBase
{
    private readonly IDocumentValidationService _validationService;
    private readonly ILogger<PublicValidationController> _logger;

    public PublicValidationController(
        IDocumentValidationService validationService,
        ILogger<PublicValidationController> logger)
    {
        _validationService = validationService;
        _logger = logger;
    }

    [HttpPost("validate")]
    [AllowAnonymous]
    public async Task<IActionResult> Validate([FromBody] PublicValidateDocumentRequest? request)
    {
        var token = request?.Token?.Trim();

        // 8-character prefix of token hash for logging (Req 12.11)
        var hashPrefix = "none";
        if (!string.IsNullOrWhiteSpace(token))
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            hashPrefix = Convert.ToHexString(hash)[..Math.Min(8, Convert.ToHexString(hash).Length)].ToLowerInvariant();
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogInformation("Public validation failed: empty token. HashPrefix={HashPrefix}", hashPrefix);
            return NotFound(new PublicValidationResponse { Valid = false });
        }

        var result = await _validationService.ValidateAsync(token);

        if (result == null || !result.Valid)
        {
            _logger.LogInformation("Public validation failed: token not found or invalid. HashPrefix={HashPrefix}", hashPrefix);
            return NotFound(new PublicValidationResponse { Valid = false });
        }

        _logger.LogInformation("Public validation succeeded: DocumentType={DocType}, HashPrefix={HashPrefix}", result.DocumentType, hashPrefix);

        return Ok(new PublicValidationResponse
        {
            Valid = true,
            DocumentType = result.DocumentType,
            IssuedAt = result.IssuedAt.ToString("o"),
            Signatures = result.Signatures.Select(s => new PublicValidationSignatureResponse
            {
                Role = s.Role,
                Signed = s.Signed
            }).ToList()
        });
    }
}
