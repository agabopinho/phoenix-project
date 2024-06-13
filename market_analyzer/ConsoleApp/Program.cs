﻿using Application.Models;
using Application.Options;
using Application.Services;
using Application.Services.Providers.Date;
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

    services.AddSingleton<OnlineDateProvider>();
    services.AddSingleton<IDateProvider, OnlineDateProvider>();

    services.AddSingleton<State>();
    services.AddSingleton<LoopService>();
    services.AddSingleton<MarketDataLoopService>();

    services.AddHostedService<StrategyService>();
    services.AddHostedService<MarketDataService>();
});

await builder.Build().RunAsync();