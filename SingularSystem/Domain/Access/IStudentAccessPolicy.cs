using System.Collections.Generic;
using SingularSystem.Domain.Tenancy;
using SingularSystem.Models.Entities;

namespace SingularSystem.Domain.Access
{
    public interface IStudentAccessPolicy
    {
        /// <summary>
        /// Decide se o usuário (com suas roles) pode acessar o aluno no contexto de tenant dado.
        /// tenant nulo equivale a Plano_Publico.
        /// </summary>
        bool CanAccess(ApplicationUser user, Student student, IEnumerable<string> roles, ITenantContext? tenant);
    }
}
