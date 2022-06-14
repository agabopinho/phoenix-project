using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace MarketAnalyzer.Controllers
{
    // [Authorize]
    [ApiController]
    [Route("[controller]")]
    // [RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly IDatabase _database;
        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(IDatabase database, ILogger<WeatherForecastController> logger)
        {
            _database = database;
            _logger = logger;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public IEnumerable<WeatherForecast> GetAsync()
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }
    }
}