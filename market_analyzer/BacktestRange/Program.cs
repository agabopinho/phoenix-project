using Application.Services.Providers.Range;
using Backtesting;
using Grpc.Terminal.Enums;
using Infrastructure.GrpcServerTerminal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MiniExcelLibs;
using OoplesFinance.StockIndicators;
using OoplesFinance.StockIndicators.Enums;
using OoplesFinance.StockIndicators.Models;
using Serilog;
using Spectre.Console;

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

var fromDate = new DateTime(2024, 6, 21, 6, 0, 0, DateTimeKind.Utc);
var toDate = fromDate.Date.AddDays(1);

var rangeChart = new RangeChart(brickSize: 50);
var slippage = 0;

var reply = await marketDataWrapper.GetTicksRangeBytesAsync(
    "WIN$N",
    fromDate,
    toDate,
    CopyTicks.Trade,
    [RangeChartExtensions.FIELD_TIME_MSC, RangeChartExtensions.FIELD_LAST, RangeChartExtensions.FIELD_VOLUME_REAL],
    default);

rangeChart.CheckNewPrice(reply.Bytes.Select(it => (byte)it).ToArray());

var tickerData = rangeChart.Bricks
    .Select(it => new TickerData
    {
        Close = it.Close,
        Date = it.Date,
        High = it.High,
        Low = it.Low,
        Open = it.Open,
        Volume = it.Volume,
    });

var stockData = new StockData(tickerData).CalculateMidpoint();

var result = Backtest(rangeChart, stockData, slippage);

await File.Open("bricks.xlsx", FileMode.OpenOrCreate).SaveAsAsync(result);

static List<Trade> Backtest(RangeChart rangeChart, StockData stockData, double slippage)
{
    var buyPrice = 0D;
    var sellPrice = 0D;
    var profit = 0D;
    var operations = 0;

    var rows = new List<Trade>();

    for (var i = 0; i < rangeChart.Bricks.Count; i++)
    {
        var previous = i > 0 ? rangeChart.Bricks.ElementAt(i - 1) : default;
        var previousSignal = i > 0 ? stockData.SignalsList[i - 1] : default;
        var current = rangeChart.Bricks.ElementAt(i);
        var localSignal = default(Signal?);
        var price = default(double?);

        if (buyPrice == 0 && previousSignal is (Signal.Buy or Signal.StrongBuy))
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

        if (sellPrice == 0 && previousSignal is (Signal.Sell or Signal.StrongSell))
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
            TicksCount = current.TicksCount,
            Type = current.Type,
            Profit = profit,
            Price = price
        });
    }

    Console.WriteLine($"Trades: {operations}, Profit: {profit}");

    return rows;
}
