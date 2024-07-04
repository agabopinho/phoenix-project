using Application.Models;
using Application.Options;
using Application.Services;
using Application.Services.Providers;
using Application.Services.Providers.Date;
using Application.Services.Strategies;
using Application.Workers;
using Infrastructure.GrpcServerTerminal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

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
    services.AddOrderManagementWrapper();

    services.AddOperationSettings(configure
        => context.Configuration.GetSection("Operation").Bind(configure));

    services.AddSingleton<OnlineDate>();
    services.AddSingleton<IDate, OnlineDate>();

    services.AddSingleton<State>();
    services.AddSingleton<OrderWrapper>();

    services.AddSingleton<ILoopService, PositionLoopService>();
    services.AddSingleton<ILoopService, OrdersLoopService>();
    services.AddSingleton<ILoopService, LastTickLoopService>();
    services.AddSingleton<ILoopService, MarketDataLoopService>();
    services.AddSingleton<ILoopService, SanityTestLoopService>();

    services.AddSingleton<ILoopService, OpenBuyPositionLoopService>();
    services.AddSingleton<ILoopService, OpenSellPositionLoopService>();
    services.AddSingleton<ILoopService, PositionBuyLoopService>();
    services.AddSingleton<ILoopService, PositionSellLoopService>();

    services.AddHostedService<LoopBackgroundService>();
});

await builder.Build().RunAsync();