using System.Collections.Generic;
using System.Linq;
using SingularSystem.Domain.Access;
using SingularSystem.Domain.Tenancy;
using SingularSystem.Models;
using SingularSystem.Models.Entities;

namespace SingularSystem.Services
{
    public sealed class StudentAccessPolicy : IStudentAccessPolicy
    {
        public bool CanAccess(ApplicationUser user, Student student, IEnumerable<string> roles, ITenantContext? tenant)
        {
            var roleSet = roles as ICollection<string> ?? roles.ToList();

            if (roleSet.Contains("Admin")) return true;

            if (tenant?.IsPrivatePlan == true)
                return !string.IsNullOrEmpty(student.TenantId) && student.TenantId == tenant.TenantId;

            bool SameMunicipality() =>
                (Helpers.CityMatches(student.City, user.City) && Helpers.StateMatches(student.State, user.State)) ||
                (student.School != null && Helpers.CityMatches(student.School.City, user.City)
                                        && Helpers.StateMatches(student.School.State, user.State));

            if (roleSet.Contains("Coordinator") || roleSet.Contains("Principal")) return SameMunicipality();

            if (roleSet.Contains("Teacher") || roleSet.Contains("Psychologist") || roleSet.Contains("SocialWork") ||
                roleSet.Contains("MunicipalManager") || roleSet.Contains("Secretary") || roleSet.Contains("Psychopedagogue"))
            {
                bool sameSchool = user.SchoolID != null && student.SchoolID == user.SchoolID;
                return SameMunicipality() || sameSchool;
            }

            return SameMunicipality();
        }
    }
}
