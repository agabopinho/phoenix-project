﻿using Application.Models;
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

var fromDate = new DateTime(2024, 7, 10, 6, 0, 0, DateTimeKind.Utc);
var toDate = fromDate.Date.AddDays(1);

var symbol = "WDOQ24";
var tradesCount = 1000;

double? takeProfit = null;
double? stoploss = null;

var reply = await marketDataWrapper.GetTicksRangeBytesAsync(
    symbol,
    fromDate,
    toDate,
    CopyTicks.Trade,
    [MarketDataWrapper.FIELD_TIME_MSC, MarketDataWrapper.FIELD_LAST, MarketDataWrapper.FIELD_VOLUME_REAL],
    default);

var bytes = reply.Bytes.Select(it => (byte)it).ToArray();
var trades = Trade.CreateFromNpz(bytes).ToArray();

var bricks = trades
    .Chunk(tradesCount)
    .Select(it => new Brick
    {
        Date = it[0].Time,
        Open = it[0].Last,
        High = it.Max(it => it.Last),
        Low = it.Min(it => it.Last),
        Close = it[^1].Last,
        TicksCount = it.Length,
        Volume = it.Sum(it => it.Volume),
    })
    .ToArray();

var backtest = new Backtest(slippage: 0.5D);

var index = -1;

foreach (var current in bricks)
{
    index++;

    if (index < 4)
    {
        continue;
    }

    var index3 = bricks[index - 2];
    var index2 = bricks[index - 1];
    var index1 = bricks[index];

    backtest.AddBrick(current);

    if (takeProfit is not null && backtest.CurrentPosition?.Profit >= takeProfit)
    {
        backtest.ClosePosition();
    }

    if (stoploss is not null && backtest.CurrentPosition?.Profit <= -stoploss)
    {
        backtest.ClosePosition();
    }

    var signal2 = index1.LineUp > index2.LineUp;// && index2.LineUp > index3.LineUp;
    var signal1 = index1.LineUp < index2.LineUp;// && index2.LineUp < index3.LineUp;

    if (signal1 && (backtest.CurrentPosition is null || backtest.CurrentPosition.Type is SideType.Sell))
    {
        if (backtest.CurrentPosition?.Type is SideType.Sell)
        {
            backtest.ClosePosition();
        }
        backtest.AddPosition(SideType.Buy);
    }
    else if (signal2 && (backtest.CurrentPosition is null || backtest.CurrentPosition.Type is SideType.Buy))
    {
        if (backtest.CurrentPosition?.Type is SideType.Buy)
        {
            backtest.ClosePosition();
        }
        backtest.AddPosition(SideType.Sell);
    }

    Console.WriteLine($"{backtest.Bricks.Last().Date}\t{backtest.CurrentPosition?.Type.ToString() ?? "None"}\tProfit={backtest.CurrentPosition?.Profit.ToString() ?? "None"}\tSum={backtest.Positions.Sum(it => it.Profit)}");
}

using var stream = File.Open($"bricks-{symbol}-{tradesCount}T-{fromDate:yyyy-MM-dd}.xlsx", FileMode.OpenOrCreate);

await stream.SaveAsAsync(new Dictionary<string, object>
{
    { "bricks", backtest.Bricks },
    { "positions", backtest.Positions }
});

