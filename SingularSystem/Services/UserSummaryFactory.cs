using SingularSystem.DTOs.Users;
using SingularSystem.Interfaces;
using SingularSystem.Models.Entities;
using Microsoft.AspNetCore.Identity;

namespace SingularSystem.Services
{
    public class UserSummaryFactory : IUserSummaryFactory
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UserSummaryFactory(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<UserSummaryResponse?> CreateAsync(ApplicationUser? user)
        {
            if (user == null)
                return null;

            var roles = await _userManager.GetRolesAsync(user);

            return Map(user, roles);
        }

        public async Task<Dictionary<string, UserSummaryResponse>> CreateManyAsync(
            IEnumerable<ApplicationUser> users
        )
        {
            var userList = users.DistinctBy(u => u.Id).ToList();

            var result = new Dictionary<string, UserSummaryResponse>();

            foreach (var user in userList)
            {
                var roles = await _userManager.GetRolesAsync(user);
                result[user.Id] = Map(user, roles);
            }

            return result;
        }

        private static UserSummaryResponse Map(
            ApplicationUser user,
            IEnumerable<string> roles
        )
        {
            return new UserSummaryResponse
            {
                ID = user.Id,
                FullName = user.FullName,
                City = user.City,
                State = user.State,
                SchoolID = user.SchoolID,
                SchoolName = user.School?.Name,
                Roles = roles.ToList()
            };
        }
    }
}
