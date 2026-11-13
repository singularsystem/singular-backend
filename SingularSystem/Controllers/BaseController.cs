using SingularSystem.Domain.Access;
using SingularSystem.Domain.Tenancy;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace SingularSystem.Controllers
{
    public class BaseController : ControllerBase
    {
        protected ITenantContext? TenantContext => HttpContext?.RequestServices?.GetService<ITenantContext>();

        protected string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException("UserId não encontrado no token");

        protected Guid CurrentUserGuid
        {
            get
            {
                if (!Guid.TryParse(CurrentUserId, out var guid))
                    throw new UnauthorizedAccessException("UserId inválido");

                return guid;
            }
        }

        protected bool HasStudentAccess(ApplicationUser user, Student student, IList<string> roles)
        {
            var policy = HttpContext?.RequestServices?.GetService<IStudentAccessPolicy>();
            if (policy is null)
            {
                HttpContext?.RequestServices?.GetService<ILoggerFactory>()?
                    .CreateLogger("StudentAccess")
                    .LogError("IStudentAccessPolicy não registrada; acesso ao aluno negado.");
                return false;
            }
            return policy.CanAccess(user, student, roles, TenantContext);
        }

        /// <summary>
        /// Atalho para controllers cujo ramo Público não muda: só aplica a trava de tenant.
        /// </summary>
        protected bool IsOutsideTenant(ApplicationUser user, Student student, IList<string> roles)
            => TenantContext?.IsPrivatePlan == true && !HasStudentAccess(user, student, roles);
    }
}
