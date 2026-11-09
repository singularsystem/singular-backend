using SingularSystem.Models;
using SingularSystem.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace SingularSystem.Controllers
{
    public class BaseController : ControllerBase
    {
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
            if (roles.Contains("Admin")) return true;

            if (roles.Contains("Coordinator"))
            {
                return (Helpers.CityMatches(student.City, user.City) && Helpers.StateMatches(student.State, user.State)) ||
                       (student.School != null && Helpers.CityMatches(student.School.City, user.City) && Helpers.StateMatches(student.School.State, user.State));
            }

            if (roles.Contains("Principal"))
            {
                return (Helpers.CityMatches(student.City, user.City) && Helpers.StateMatches(student.State, user.State)) ||
                       (student.School != null && Helpers.CityMatches(student.School.City, user.City) && Helpers.StateMatches(student.School.State, user.State));
            }

            if (roles.Contains("Teacher") || roles.Contains("Psychologist") || roles.Contains("SocialWork") ||
                roles.Contains("MunicipalManager") || roles.Contains("Secretary") ||
                roles.Contains("Psychopedagogue"))
            {
                bool sameMunicipality = (Helpers.CityMatches(student.City, user.City) && Helpers.StateMatches(student.State, user.State)) ||
                                        (student.School != null && Helpers.CityMatches(student.School.City, user.City) && Helpers.StateMatches(student.School.State, user.State));

                bool sameSchool = user.SchoolID != null && student.SchoolID == user.SchoolID;

                return sameMunicipality || sameSchool;
            }

            return (Helpers.CityMatches(student.City, user.City) && Helpers.StateMatches(student.State, user.State)) ||
                   (student.School != null && Helpers.CityMatches(student.School.City, user.City) && Helpers.StateMatches(student.School.State, user.State));
        }
    }
}
