using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SingularSystem.Domain.Plans;
using SingularSystem.Domain.Tenancy;
using SingularSystem.Models;

namespace SingularSystem.Controllers
{
    [ApiController]
    public class TenantsController : ControllerBase
    {
        private readonly ITenantProfessionalConfigService _configService;
        private readonly ITenantContext _tenantContext;

        public TenantsController(
            ITenantProfessionalConfigService configService,
            ITenantContext tenantContext)
        {
            _configService = configService;
            _tenantContext = tenantContext;
        }

        [Authorize(Policy = "AdminOrSchoolAdmin")]
        [HttpGet("api/v1/tenants/me/professional-config")]
        public async Task<IActionResult> GetMyProfessionalConfig()
        {
            if (!_tenantContext.IsPrivatePlan || string.IsNullOrEmpty(_tenantContext.TenantId))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "O usuário atual não possui escola associada."
                });
            }

            var config = await _configService.GetEffectiveAsync(_tenantContext.TenantId);
            return Ok(new ApiResponse<ProfessionalConfigDto>
            {
                Success = true,
                Data = config
            });
        }

        [Authorize(Policy = "SchoolAdmin")]
        [HttpPut("api/v1/tenants/me/professional-config")]
        public async Task<IActionResult> UpdateMyProfessionalConfig([FromBody] UpdateProfessionalConfigRequest request)
        {
            if (!_tenantContext.IsPrivatePlan || string.IsNullOrEmpty(_tenantContext.TenantId))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Acesso negado."
                });
            }

            var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var result = await _configService.UpdateBySchoolAdminAsync(
                _tenantContext.TenantId,
                callerId,
                request.PsychologistEnabled,
                request.PsychopedagogueEnabled,
                request.SocialWorkEnabled
            );

            if (result.Status == ProfessionalConfigUpdateStatus.Forbidden)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Acesso negado."
                });
            }

            return Ok(new ApiResponse<ProfessionalConfigDto>
            {
                Success = true,
                Message = "Configuração atualizada com sucesso.",
                Data = result.Config!
            });
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpPut("api/v1/admin/tenants/{tenantId}/professional-config")]
        public async Task<IActionResult> UpdateTenantProfessionalConfigByAdmin(string tenantId, [FromBody] ProfessionalConfigDto request)
        {
            var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var result = await _configService.UpdateByAdminAsync(
                tenantId,
                callerId,
                request.PsychologistEnabled,
                request.PsychopedagogueEnabled,
                request.SocialWorkEnabled
            );

            return Ok(new ApiResponse<ProfessionalConfigDto>
            {
                Success = true,
                Message = "Configuração atualizada com sucesso pelo administrador.",
                Data = result
            });
        }
    }
}
