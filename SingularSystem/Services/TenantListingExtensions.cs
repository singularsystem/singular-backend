using System.Linq;
using SingularSystem.Domain.Tenancy;
using SingularSystem.Models.Entities;

namespace SingularSystem.Services
{
    public static class TenantListingExtensions
    {
        /// <summary>
        /// Restringe documentos aos alunos do tenant quando o contexto é Plano_Privado.
        /// Fora de Plano_Privado devolve a query sem alteração.
        /// </summary>
        public static IQueryable<T> WhereStudentInTenant<T>(this IQueryable<T> query, ITenantContext? tenant)
            where T : BaseDocument
        {
            if (tenant?.IsPrivatePlan != true) return query;
            var tenantId = tenant.TenantId;
            return query.Where(d => d.Student != null && d.Student.TenantId == tenantId);
        }

        /// <summary>
        /// Mesmo critério sobre a própria query de alunos.
        /// </summary>
        public static IQueryable<Student> WhereInTenant(this IQueryable<Student> query, ITenantContext? tenant)
        {
            if (tenant?.IsPrivatePlan != true) return query;
            var tenantId = tenant.TenantId;
            return query.Where(s => s.TenantId == tenantId);
        }
    }
}
