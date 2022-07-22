using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Application.Services.Providers.Rates.BacktestRates
{
    public class BacktestRatesRepository : IBacktestRatesRepository
    {
        private readonly string _connStr;

        public BacktestRatesRepository(IConfiguration configuration)
        {
            _connStr = configuration.GetConnectionString("BacktestSqlServer");
        }

        public async Task<IEnumerable<TickData>> GetTicksAsync(string symbol, DateOnly date, CancellationToken cancellationToken)
        {
            var connection = new SqlConnection(_connStr);
            var command = "" +
                "select * from trade " +
                "where symbol=@symbol and convert(date, [time]) = @date";

            return await connection.QueryAsync<TickData>(command, new { symbol, date = date.ToDateTime(TimeOnly.MinValue) },
                commandTimeout: 60);
        }
    }
}
