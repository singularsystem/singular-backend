using SingularSystem.DTOs.Users;
using SingularSystem.Models.Entities;

namespace SingularSystem.Interfaces
{
    public interface IUserSummaryFactory
    {
        Task<UserSummaryResponse?> CreateAsync(ApplicationUser? user);
        Task<Dictionary<string, UserSummaryResponse>> CreateManyAsync(
            IEnumerable<ApplicationUser> users
        );
    }
}
