namespace Application.Services;

public interface ILoopService
{
    Task<bool> StoppedAsync(CancellationToken stoppingToken);
    Task<bool> CanRunAsync(CancellationToken stoppingToken);
    Task RunAsync(CancellationToken cancellationToken);
}
