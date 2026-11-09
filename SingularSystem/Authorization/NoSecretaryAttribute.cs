using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SingularSystem.Models;
using Microsoft.AspNetCore.Http;

namespace SingularSystem.Authorization
{
    public class NoSecretaryAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var user = context.HttpContext.User;
            if (user != null && user.IsInRole("Secretary"))
            {
                context.Result = new ObjectResult(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Acesso negado: Secretários não possuem permissão para realizar esta ação ou visualizar estes dados detalhados."
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;
            }
            base.OnActionExecuting(context);
        }
    }
}
