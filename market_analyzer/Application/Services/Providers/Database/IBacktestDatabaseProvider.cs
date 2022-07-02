using Grpc.Terminal;

namespace Application.Services.Providers.Database
{
    public interface IBacktestDatabaseProvider
    {
        SortedList<DateTime, List<Trade>> TicksDatabase { get; }

        Task<bool> LoadAsync(string symbol, DateOnly date, int chunkSize, CancellationToken cancellationToken);
        DateTime PartitionKey(DateTime time);
    }
}
