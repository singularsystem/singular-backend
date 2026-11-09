using SingularSystem.Models.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace SingularSystem.Services
{
    public class CustomSignInManager : SignInManager<ApplicationUser>
    {
        public CustomSignInManager(UserManager<ApplicationUser> userManager, IHttpContextAccessor contextAccessor, IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory, IOptions<IdentityOptions> optionsAccessor, ILogger<SignInManager<ApplicationUser>> logger, IAuthenticationSchemeProvider schemes, IUserConfirmation<ApplicationUser> confirmation) : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
        {
        }

        public override async Task<SignInResult> PasswordSignInAsync(string userName, string password, bool isPersistent, bool lockoutOnFailure)
        {
            var user = await UserManager.FindByNameAsync(userName);

            if (user == null)
                return SignInResult.Failed;

            if (!user.IsActive)
                return SignInResult.NotAllowed;

            return await base.PasswordSignInAsync(
                userName,
                password,
                isPersistent,
                lockoutOnFailure);
        }
    }
}
