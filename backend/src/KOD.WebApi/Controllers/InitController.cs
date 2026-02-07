using Microsoft.AspNetCore.Mvc;

namespace KOD.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public sealed class InitController : ControllerBase
    {
        [HttpGet]
        public ActionResult<string> Get() => Ok("Hello!");
    }
}
