using Microsoft.AspNetCore.Mvc;

namespace OneIncTestApp.API.Controllers
{
    [Route("health"), Route("_health")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class HealthController : ControllerBase
    {
        [Route("")]
        public IActionResult Index()
        {
            return Ok("healthy");
        }
    }
}
