using Application.Models;
using Application.Options;
using Application.Services.Providers.Date;
using Grpc.Terminal;
using Infrastructure.GrpcServerTerminal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Services.MarketData;

public class RatesMarketDataLoopService(
    IMarketDataWrapper marketDataWrapper,
    IDate dateProvider,
    State state,
    IOptionsMonitor<OperationOptions> operationSettings,
    ILogger<RatesMarketDataLoopService> logger
) : ILoopService
{
    private const int AHEAD_SECONDS = 30;

    public const string RATES_KEY = "rates";

    private readonly Dictionary<DateTime, Rate> _rates = [];

    private DateTime _currentTime;
    private Rate? _lastRate;
    private int _previousRateCount;
    private int _newRates;

    private void PreExecution()
    {
        _currentTime = dateProvider.LocalDateSpecifiedUtcKind();
    }

    public Task<bool> StoppedAsync(CancellationToken stoppingToken)
    {
        return Task.FromResult(false);
    }

    public Task<bool> CanRunAsync(CancellationToken stoppingToken)
    {
        return Task.FromResult(true);
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        PreExecution();

        if (_rates.Count == 0)
        {
            logger.LogInformation("Loading data from: {fromDate}", GetFromDate());
        }

        await CheckNewPrice(cancellationToken);

        state.SetRatesCharts(RATES_KEY, _rates.Values);

        if (_newRates > 0)
        {
            logger.LogInformation("NewRates: {rate}", _newRates);

            var rates = _rates.Values.ToArray();

            if (rates.Length >= 1)
            {
                logger.LogInformation("Rates[^1]: {lastRate}", rates[^1]);
            }
        }
    }

    private async Task CheckNewPrice(CancellationToken cancellationToken)
    {
        var rates = await GetRatesAsync(cancellationToken);

        _previousRateCount = _rates.Count;

        var lastTempRate = default(Rate);

        foreach (var rate in rates)
        {
            _rates[rate.Time.ToDateTime()] = rate;

            lastTempRate = rate;
        }

        if (lastTempRate is not null)
        {
            _lastRate = lastTempRate;
        }

        _newRates = _rates.Count - _previousRateCount;
    }

    private async Task<IEnumerable<Rate>> GetRatesAsync(CancellationToken cancellationToken)
    {
        var fromDate = GetFromDate();
        var toDate = _currentTime.AddSeconds(AHEAD_SECONDS);

        var ratesReply = await marketDataWrapper.GetRatesRangeFromTicksAsync(
            operationSettings.CurrentValue.Symbol!,
            fromDate,
            toDate,
            operationSettings.CurrentValue.Timeframe,
            cancellationToken);

        state.CheckResponseStatus(ResponseType.GetRatesFromTicks, ratesReply.ResponseStatus);

        return ratesReply.Rates;
    }

    private DateTime GetFromDate()
    {
        if (_lastRate?.Time is not null)
        {
            return _lastRate.Time.ToDateTime();
        }

        var resumeFrom = operationSettings.CurrentValue.ResumeFrom;

        if (resumeFrom is not null)
        {
            return DateTime.SpecifyKind(resumeFrom.Value, DateTimeKind.Utc);
        }

        return _currentTime - _currentTime.TimeOfDay;
    }
}
