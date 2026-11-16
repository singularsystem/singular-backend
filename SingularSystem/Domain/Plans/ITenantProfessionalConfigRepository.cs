using SingularSystem.Models.Entities;

namespace SingularSystem.Domain.Plans
{
    public interface ITenantProfessionalConfigRepository
    {
        Task<TenantProfessionalConfig?> GetByTenantIdAsync(string tenantId);
        Task UpsertAsync(TenantProfessionalConfig config);
    }
}
