using Microsoft.AspNetCore.Mvc;

namespace KOD.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InitController : ControllerBase
    {
        [HttpGet]
        public ActionResult<string> Get()
        {
            return "Project inited.";
        }
    }
}
