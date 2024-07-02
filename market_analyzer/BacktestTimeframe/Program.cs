using Application.Services.Providers.Range;
using Backtesting;
using Grpc.Core;
using Grpc.Terminal;
using Grpc.Terminal.Enums;
using Infrastructure.GrpcServerTerminal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MiniExcelLibs;
using OoplesFinance.StockIndicators.Enums;
using Serilog;
using Skender.Stock.Indicators;
using Spectre.Console;

internal class Program
{
    private static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        var builder = Host.CreateDefaultBuilder(args);

        builder.UseSerilog((context, services, configuration) => configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext());

        builder.ConfigureServices((context, services) =>
        {
            services.AddGrpcServerOptions(configure =>
                context.Configuration.GetSection("GrpcServer").Bind(configure));

            services.AddMarketDataWrapper();
        });

        var host = builder.Build();

        var marketDataWrapper = host.Services.GetRequiredService<IMarketDataWrapper>();

        var symbol = "WDON24";
        var slippage = 0.5;
        var fromDate = new DateTime(2024, 6, 17, 6, 0, 0, DateTimeKind.Utc);
        var toDate = fromDate.Date.AddDays(1);

        var rates = await GetRatesAsync(marketDataWrapper, symbol, fromDate, toDate);

        var quotes = rates
            .Select(it => new Quote
            {
                Close = Convert.ToDecimal(it.Close!.Value),
                Date = it.Time.ToDateTime(),
                High = Convert.ToDecimal(it.High!.Value),
                Low = Convert.ToDecimal(it.Low!.Value),
                Open = Convert.ToDecimal(it.Open!.Value),
                Volume = Convert.ToDecimal(it.Volume!.Value),
            }).ToArray();

        var rangeChart = await CreateRangeChartAsync(marketDataWrapper, symbol, fromDate, toDate, initialBrickSize: 2, quotes);

        var result = Backtest(quotes, rangeChart, slippage);

        await File.Open("bricks.xlsx", FileMode.OpenOrCreate).SaveAsAsync(result);
    }

    private static List<Trade> Backtest(Quote[] quotes, RangeChart rangeChart, double slippage)
    {
        var buyPrice = 0D;
        var sellPrice = 0D;
        var profit = 0D;
        var operations = 0;

        var rows = new List<Trade>();

        for (var i = 0; i < rangeChart.Bricks.Count; i++)
        {
            var previous = i > 0 ? rangeChart.Bricks.ElementAt(i - 1) : default;
            var current = rangeChart.Bricks.ElementAt(i);

            var localSignal = default(Signal?);
            var price = default(double?);

            if (buyPrice == 0 && current.Open > previous?.Open)
            {
                localSignal = Signal.Buy;
                price = buyPrice = current.Open + slippage;
                if (sellPrice > 0)
                {
                    profit += sellPrice - buyPrice;
                }
                sellPrice = 0;
                operations++;
            }

            if (sellPrice == 0 && current.Open < previous?.Open)
            {
                localSignal = Signal.Sell;
                price = sellPrice = current.Open - slippage;
                if (buyPrice > 0)
                {
                    profit += sellPrice - buyPrice;
                }
                buyPrice = 0;
                operations++;
            }

            rows.Add(new()
            {
                Date = current.Date.ToString("dd/MM/yyyy HH:mm:ss.fff"),
                Signal = localSignal,
                Close = current.Close,
                High = current.High,
                Low = current.Low,
                Volume = current.Volume,
                Open = current.Open,
                Profit = profit,
                Price = price
            });
        }

        Console.WriteLine($"Trades: {operations}, Profit: {profit}");

        return rows;
    }

    private static async Task<List<Rate>> GetRatesAsync(IMarketDataWrapper marketDataWrapper, string symbol, DateTime fromDate, DateTime toDate)
    {
        using var reply = marketDataWrapper.StreamRatesRangeFromTicksAsync(
            symbol,
            fromDate,
            toDate,
            TimeSpan.FromSeconds(10),
            10_000_000,
            default);

        var rates = new List<Rate>();

        await foreach (var item in reply.ResponseStream.ReadAllAsync())
        {
            rates.AddRange(item.Rates);
        }

        return rates;
    }

    private static async Task<RangeChart> CreateRangeChartAsync(IMarketDataWrapper marketDataWrapper, string symbol, DateTime fromDate, DateTime toDate, double initialBrickSize, IEnumerable<Quote> quotes)
    {
        var atr = quotes.GetAtr(100).ToArray();

        foreach (var item in atr)
        {
            item.Atr *= 2;
        }

        var reply = await marketDataWrapper.GetTicksRangeBytesAsync(
            symbol,
            fromDate,
            toDate,
            CopyTicks.Trade,
            [RangeChartExtensions.FIELD_TIME_MSC, RangeChartExtensions.FIELD_LAST, RangeChartExtensions.FIELD_VOLUME_REAL],
            default);

        var rangeChart = new RangeChart(brickSize: initialBrickSize);

        rangeChart.CheckNewPrice(reply.Bytes.Select(it => (byte)it).ToArray(), null, atr);

        return rangeChart;
    }
}