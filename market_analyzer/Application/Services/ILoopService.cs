namespace Application.Services;

public interface ILoopService
{
    Task RunAsync(CancellationToken cancellationToken);
    Task<bool> StoppedAsync(CancellationToken stoppingToken);
    Task<bool> CanRunAsync(CancellationToken stoppingToken);
}
