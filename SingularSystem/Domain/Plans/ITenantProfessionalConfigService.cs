namespace SingularSystem.Domain.Plans
{
    public enum ProfessionalConfigUpdateStatus
    {
        Success,
        Forbidden
    }

    public record ProfessionalConfigDto(bool PsychologistEnabled, bool PsychopedagogueEnabled, bool SocialWorkEnabled);
    public record UpdateProfessionalConfigRequest(bool PsychologistEnabled, bool PsychopedagogueEnabled, bool? SocialWorkEnabled);
    public record ProfessionalConfigUpdateResult(ProfessionalConfigUpdateStatus Status, ProfessionalConfigDto? Config);

    public interface ITenantProfessionalConfigService
    {
        Task<ProfessionalConfigDto> GetEffectiveAsync(string tenantId);
        Task<ProfessionalConfigUpdateResult> UpdateBySchoolAdminAsync(string tenantId, string callerId, bool psychologistEnabled, bool psychopedagogueEnabled, bool? socialWorkEnabled);
        Task<ProfessionalConfigDto> UpdateByAdminAsync(string tenantId, string callerId, bool psychologistEnabled, bool psychopedagogueEnabled, bool socialWorkEnabled);
        Task<bool> IsTypeEnabledAsync(string tenantId, string type);
    }
}
