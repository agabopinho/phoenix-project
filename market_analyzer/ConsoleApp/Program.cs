﻿using Application.BackgroupServices;
using Application.Options;
using Application.Services;
using Application.Services.Providers.Cycle;
using Application.Services.Providers.Database;
using Application.Services.Providers.Rates;
using ConsoleApp.Converters;
using Infrastructure.GrpcServerTerminal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using StackExchange.Redis;
using System.ComponentModel;

TypeDescriptor.AddAttributes(typeof(DateOnly), new TypeConverterAttribute(typeof(DateOnlyTypeConverter)));
TypeDescriptor.AddAttributes(typeof(TimeOnly), new TypeConverterAttribute(typeof(TimeOnlyTypeConverter)));

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = Host.CreateDefaultBuilder(args);

builder.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.ConfigureServices((context, services) =>
{
    services.AddSingleton(_
        => ConnectionMultiplexer
            .Connect(context.Configuration.GetValue<string>("Redis"))
            .GetDatabase(0));

    services.AddMarketDataWrapper(configure =>
        context.Configuration.GetSection("GrpcServer:MarketData").Bind(configure));
    services.AddOrderManagementWrapper(configure =>
        context.Configuration.GetSection("GrpcServer:OrderManagement").Bind(configure));

    services.AddSingleton<OnlineCycleProvider>();
    services.AddSingleton<BacktestCycleProvider>();
    services.AddSingleton<ICycleProvider>(serviceProvider =>
    {
        var options = serviceProvider.GetRequiredService<IOptions<OperationSettings>>();

        if (options.Value.Backtest.Enabled)
            return serviceProvider.GetRequiredService<BacktestCycleProvider>();

        return serviceProvider.GetRequiredService<OnlineCycleProvider>();
    });

    services.AddSingleton<OnlineRatesProvider>();
    services.AddSingleton<BacktestRatesProvider>();
    services.AddSingleton<IRatesProvider>(serviceProvider =>
    {
        var options = serviceProvider.GetRequiredService<IOptions<OperationSettings>>();

        if (options.Value.Backtest.Enabled)
            return serviceProvider.GetRequiredService<BacktestRatesProvider>();

        return serviceProvider.GetRequiredService<OnlineRatesProvider>();
    });

    services.AddOperationSettings(configure
        => context.Configuration.GetSection("Operation").Bind(configure));

    services.AddSingleton<IBacktestDatabaseProvider, BacktestDatabaseProvider>();
    services.AddSingleton<ILoopService, LoopService>();
    services.AddHostedService<WorkerService>();
});

await builder.Build().RunAsync();