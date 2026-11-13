using SingularSystem.Domain.Tenancy;
using SingularSystem.Models.Entities;
using System.Collections.Generic;
using ModelHelpers = SingularSystem.Models.Helpers;

namespace SingularSystem.Tests.TestHelpers
{
    public static class LegacyAccessHelper
    {
        public static bool LegacyHasStudentAccess(ApplicationUser user, Student student, IList<string> roles, ITenantContext? tenantContext)
        {
            if (roles.Contains("Admin")) return true;

            if (tenantContext?.IsPrivatePlan == true)
                return !string.IsNullOrEmpty(student.TenantId) && student.TenantId == tenantContext.TenantId;

            if (roles.Contains("Coordinator"))
            {
                return (ModelHelpers.CityMatches(student.City, user.City) && ModelHelpers.StateMatches(student.State, user.State)) ||
                       (student.School != null && ModelHelpers.CityMatches(student.School.City, user.City) && ModelHelpers.StateMatches(student.School.State, user.State));
            }

            if (roles.Contains("Principal"))
            {
                return (ModelHelpers.CityMatches(student.City, user.City) && ModelHelpers.StateMatches(student.State, user.State)) ||
                       (student.School != null && ModelHelpers.CityMatches(student.School.City, user.City) && ModelHelpers.StateMatches(student.School.State, user.State));
            }

            if (roles.Contains("Teacher") || roles.Contains("Psychologist") || roles.Contains("SocialWork") ||
                roles.Contains("MunicipalManager") || roles.Contains("Secretary") ||
                roles.Contains("Psychopedagogue"))
            {
                bool sameMunicipality = (ModelHelpers.CityMatches(student.City, user.City) && ModelHelpers.StateMatches(student.State, user.State)) ||
                                        (student.School != null && ModelHelpers.CityMatches(student.School.City, user.City) && ModelHelpers.StateMatches(student.School.State, user.State));

                bool sameSchool = user.SchoolID != null && student.SchoolID == user.SchoolID;

                return sameMunicipality || sameSchool;
            }

            return (ModelHelpers.CityMatches(student.City, user.City) && ModelHelpers.StateMatches(student.State, user.State)) ||
                   (student.School != null && ModelHelpers.CityMatches(student.School.City, user.City) && ModelHelpers.StateMatches(student.School.State, user.State));
        }
    }
}
