namespace Application.Services
{
    public interface ILoopService
    {
        Task RunAsync(CancellationToken cancellationToken);
    }
}
