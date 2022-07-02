using Application.Options;

namespace Application.Services
{
    public interface ILoopService
    {
        Task RunAsync(OperationSettings operationSettings, CancellationToken cancellationToken);
    }
}
