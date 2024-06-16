using Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Application.Workers;

public class LoopBackgroundService(IEnumerable<ILoopService> loops, IServiceProvider serviceProvider) : BackgroundService
{
    private readonly Stopwatch _stopwatch = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tasks = new List<Task>();

        foreach (var loop in loops)
        {
            var task = Task.Factory.StartNew(
                function: () => LoopAsync(loop, stoppingToken),
                creationOptions: TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach);

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
    }

    private async Task LoopAsync(ILoopService loop, CancellationToken stoppingToken)
    {
        var logger = (ILogger)serviceProvider.GetRequiredService(typeof(ILogger<>).MakeGenericType(loop.GetType()));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _stopwatch.Restart();

                if (await loop.StoppedAsync(stoppingToken))
                {
                    continue;
                }

                if (!await loop.CanRunAsync(stoppingToken))
                {
                    continue;
                }

                await loop.RunAsync(stoppingToken);

                _stopwatch.Stop();

                logger.LogTrace("Run in {@ms}ms.", _stopwatch.Elapsed.TotalMilliseconds);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Loop execution error.");
            }
        }
    }
}
