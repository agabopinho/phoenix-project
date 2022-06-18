using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HomeController : ControllerBase
    {
        public HomeController(ILogger<HomeController> logger)
        {
        }

        [HttpGet]
        public int[] GetAsync()
            => Enumerable.Range(1, 5).ToArray();
    }
}