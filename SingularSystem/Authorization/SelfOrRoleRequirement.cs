using Microsoft.AspNetCore.Authorization;

namespace SingularSystem.Authorization
{
    public class SelfOrRoleRequirement : IAuthorizationRequirement
    {
        public string[] AllowedRoles { get; }

        public SelfOrRoleRequirement(params string[] roles)
        {
            AllowedRoles = roles;
        }
    }
}
