using Application.Services.Providers.Range;
using BacktestRange;
using Grpc.Terminal.Enums;
using Infrastructure.GrpcServerTerminal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MiniExcelLibs;
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

var fromDate = new DateTime(2024, 7, 3, 6, 0, 0, DateTimeKind.Utc);
var toDate = fromDate.Date.AddDays(1);
var brickSize = 15D;

var rangeChart = new RangeChart(brickSize);

var reply = await marketDataWrapper.GetTicksRangeBytesAsync(
    "WINQ24",
    fromDate,
    toDate,
    CopyTicks.Trade,
    [MarketDataWrapper.FIELD_TIME_MSC, MarketDataWrapper.FIELD_LAST, MarketDataWrapper.FIELD_VOLUME_REAL],
    default);

rangeChart.CheckNewPrice(reply.Bytes.Select(it => (byte)it).ToArray());

var bricks = rangeChart.Bricks.ToArray();
var bricksList = new List<Brick>();
var lastBrick = default(Brick);

foreach (var brick in bricks[..^1])
{
    if (lastBrick?.LineUp == brick.LineUp)
    {
        continue;
    }

    bricksList.Add(brick);
    lastBrick = brick;
}

await File.Open($"bricks-{brickSize}-{fromDate:yyyy-MM-dd}.xlsx", FileMode.OpenOrCreate).SaveAsAsync(bricksList);
