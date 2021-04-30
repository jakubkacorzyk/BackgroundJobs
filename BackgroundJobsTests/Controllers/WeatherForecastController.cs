using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BackgroundJobsTests.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private static bool _isWeatherJobDone;
        private static int _numberOfFails = 0;

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
                {
                    Date = DateTime.Now.AddDays(index),
                    TemperatureC = rng.Next(-20, 55),
                    Summary = Summaries[rng.Next(Summaries.Length)]
                })
                .ToArray();
        }
        
        [HttpGet("/jobTest")]
        public void BackgroundJobTest()
        {
            BackgroundJob.Enqueue(() => GetWeather());
        }

        [HttpGet("/jobTest/fails/{numberOfFailures}")]
        public void BackgroundJob3xFailedTest(int numberOfFailures)
        {
            _numberOfFails = 0;
            BackgroundJob.Enqueue(() => GetWeatherFailures(numberOfFailures));
        }

        [HttpGet("/jobTest/check")]
        public bool CheckJob()
        {
            return _isWeatherJobDone;
        }

        [HttpGet("/continuationTest")]
        public void ContinuationJobTest()
        {
            var jobId = BackgroundJob.Schedule(
                () => Console.WriteLine("Delayed!"),
                TimeSpan.FromSeconds(10));
            BackgroundJob.ContinueJobWith(
                jobId,
                () => GetWeather());
        }

        public static async Task GetWeather()
        {
            _isWeatherJobDone = false;
            var rng = new Random();
            Enumerable.Range(1, 5).Select(index => new WeatherForecast
                {
                    Date = DateTime.Now.AddDays(index),
                    TemperatureC = rng.Next(-20, 55),
                    Summary = Summaries[rng.Next(Summaries.Length)]
                })
                .ToArray();
            await Task.Delay(10000);
            _isWeatherJobDone = true;
        }
        
        public static async Task GetWeatherFailures(int fails)
        {
            _isWeatherJobDone = false;
            var rng = new Random();
            Enumerable.Range(1, 5).Select(index => new WeatherForecast
                {
                    Date = DateTime.Now.AddDays(index),
                    TemperatureC = rng.Next(-20, 55),
                    Summary = Summaries[rng.Next(Summaries.Length)]
                })
                .ToArray();
            await Task.Delay(10000);
            if (_numberOfFails <= fails)
            {
                _numberOfFails++;
                throw new Exception($"Failure number {_numberOfFails}/{fails}");
            }
            _isWeatherJobDone = true;
        }
    }
}