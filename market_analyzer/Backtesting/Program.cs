using Application.Services.Providers.Range;
using Grpc.Terminal.Enums;
using Infrastructure.GrpcServerTerminal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OoplesFinance.StockIndicators;
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

var fromDate = new DateTime(2024, 06, 20, 6, 0, 0, DateTimeKind.Utc);
var toDate = fromDate.Date.AddDays(1);

var rangeChart = new RangeChart(brickSize: 50);

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

var stockData = new StockData(tickerData)
    .CalculateKeltnerChannels();

foreach (var result in stockData.OutputValues)
{ 
    Console.WriteLine(result);  
}

