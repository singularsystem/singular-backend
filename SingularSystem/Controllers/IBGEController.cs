using SingularSystem.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SingularSystem.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class IBGEController : ControllerBase
    {
        private readonly IIBGEService _ibgeService;
        private readonly ILogger<IBGEController> _logger;

        public IBGEController(IIBGEService ibgeService, ILogger<IBGEController> logger)
        {
            _ibgeService = ibgeService;
            _logger = logger;
        }

        [HttpGet("by-uf/{uf}")]
        [AllowAnonymous]
        public async Task<IResult> CitiesByUF(string uf)
        {
            try
            {
                if (string.IsNullOrEmpty(uf))
                {
                    return Results.BadRequest("UF é obrigatória");
                }

                if (uf.Length > 2)
                {
                    return Results.BadRequest("UF deve ter 2 caracteres");
                }


                var cities = await _ibgeService.GetCitiesByState(uf.ToUpper());

                return Results.Ok(cities);
            }
            catch (ApplicationException ex)
            {
                _logger.LogInformation(ex.ToString());
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }
    }
}
