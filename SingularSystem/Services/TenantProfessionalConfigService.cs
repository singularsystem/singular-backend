using SingularSystem.Domain.Plans;
using SingularSystem.Models.Entities;

namespace SingularSystem.Services
{
    public class TenantProfessionalConfigService : ITenantProfessionalConfigService
    {
        private readonly ITenantProfessionalConfigRepository _repository;

        public TenantProfessionalConfigService(ITenantProfessionalConfigRepository repository)
        {
            _repository = repository;
        }

        public async Task<ProfessionalConfigDto> GetEffectiveAsync(string tenantId)
        {
            var config = await _repository.GetByTenantIdAsync(tenantId);
            if (config == null)
            {
                // Padrões quando não há registro: Psychologist=true, Psychopedagogue=true, SocialWork=false
                return new ProfessionalConfigDto(true, true, false);
            }

            return new ProfessionalConfigDto(
                config.PsychologistEnabled,
                config.PsychopedagogueEnabled,
                config.SocialWorkEnabled
            );
        }

        public async Task<ProfessionalConfigUpdateResult> UpdateBySchoolAdminAsync(
            string tenantId,
            string callerId,
            bool psychologistEnabled,
            bool psychopedagogueEnabled,
            bool? socialWorkEnabled)
        {
            var current = await _repository.GetByTenantIdAsync(tenantId);
            var currentSocialWork = current?.SocialWorkEnabled ?? false;

            if (socialWorkEnabled.HasValue && socialWorkEnabled.Value != currentSocialWork)
            {
                return new ProfessionalConfigUpdateResult(ProfessionalConfigUpdateStatus.Forbidden, null);
            }

            var config = current ?? new TenantProfessionalConfig
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId
            };

            config.PsychologistEnabled = psychologistEnabled;
            config.PsychopedagogueEnabled = psychopedagogueEnabled;
            config.SocialWorkEnabled = currentSocialWork;
            config.UpdatedAt = DateTime.UtcNow;
            config.UpdatedBy = callerId;

            await _repository.UpsertAsync(config);

            return new ProfessionalConfigUpdateResult(
                ProfessionalConfigUpdateStatus.Success,
                new ProfessionalConfigDto(config.PsychologistEnabled, config.PsychopedagogueEnabled, config.SocialWorkEnabled)
            );
        }

        public async Task<ProfessionalConfigDto> UpdateByAdminAsync(
            string tenantId,
            string callerId,
            bool psychologistEnabled,
            bool psychopedagogueEnabled,
            bool socialWorkEnabled)
        {
            var current = await _repository.GetByTenantIdAsync(tenantId);
            var config = current ?? new TenantProfessionalConfig
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId
            };

            config.PsychologistEnabled = psychologistEnabled;
            config.PsychopedagogueEnabled = psychopedagogueEnabled;
            config.SocialWorkEnabled = socialWorkEnabled;
            config.UpdatedAt = DateTime.UtcNow;
            config.UpdatedBy = callerId;

            await _repository.UpsertAsync(config);

            return new ProfessionalConfigDto(
                config.PsychologistEnabled,
                config.PsychopedagogueEnabled,
                config.SocialWorkEnabled
            );
        }

        public async Task<bool> IsTypeEnabledAsync(string tenantId, string type)
        {
            if (string.IsNullOrWhiteSpace(type)) return false;

            var trimmed = type.Trim();
            if (trimmed.Equals("Teacher", StringComparison.OrdinalIgnoreCase))
                return true;

            var config = await GetEffectiveAsync(tenantId);

            if (trimmed.Equals("Psychologist", StringComparison.OrdinalIgnoreCase))
                return config.PsychologistEnabled;

            if (trimmed.Equals("Psychopedagogue", StringComparison.OrdinalIgnoreCase))
                return config.PsychopedagogueEnabled;

            if (trimmed.Equals("SocialWork", StringComparison.OrdinalIgnoreCase))
                return config.SocialWorkEnabled;

            return false;
        }
    }
}
