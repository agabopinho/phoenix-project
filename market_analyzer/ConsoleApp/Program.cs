using Application.Options;
using Application.Services;
using Application.Services.Providers.Cycle;
using Application.Services.Providers.Rates;
using Application.Services.Providers.Rates.BacktestRates;
using Application.Services.Strategies;
using Application.Workers;
using ConsoleApp.Converters;
using Infrastructure.GrpcServerTerminal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
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
    services.AddMarketDataWrapper(configure =>
        context.Configuration.GetSection("GrpcServer:MarketData").Bind(configure));
    services.AddOrderManagementWrapper(configure =>
        context.Configuration.GetSection("GrpcServer:OrderManagement").Bind(configure));

    services.AddOperationSettings(configure
        => context.Configuration.GetSection("Operation").Bind(configure));

    services.AddSingleton<IBacktestRatesRepository, BacktestRatesRepository>();

    services.AddSingleton<OnlineCycleProvider>();
    services.AddSingleton<BacktestCycleProvider>();
    services.AddSingleton<ICycleProvider>(serviceProvider =>
    {
        var options = serviceProvider.GetRequiredService<IOptions<OperationSettings>>();

        if (options.Value.Backtest.Enabled)
            return serviceProvider.GetRequiredService<BacktestCycleProvider>();

        return serviceProvider.GetRequiredService<OnlineCycleProvider>();
    });

    services.AddSingleton<InMemoryOnlineRatesProvider>();
    services.AddSingleton<BacktestRatesProvider>();
    services.AddSingleton<IRatesProvider>(serviceProvider =>
    {
        var options = serviceProvider.GetRequiredService<IOptions<OperationSettings>>();

        if (options.Value.Backtest.Enabled)
            return serviceProvider.GetRequiredService<BacktestRatesProvider>();

        return serviceProvider.GetRequiredService<InMemoryOnlineRatesProvider>();
    });

    services.AddSingleton<BacktestLoopService>();
    services.AddSingleton<LoopService>();
    services.AddSingleton<ILoopService>(serviceProvider =>
    {
        var options = serviceProvider.GetRequiredService<IOptions<OperationSettings>>();

        if (options.Value.Backtest.Enabled)
            return serviceProvider.GetRequiredService<BacktestLoopService>();

        return serviceProvider.GetRequiredService<LoopService>();
    });

    services.AddSingleton<BacktestWorkerService>();
    services.AddSingleton<WorkerService>();
    services.AddHostedService<BackgroundService>(serviceProvider =>
    {
        var options = serviceProvider.GetRequiredService<IOptions<OperationSettings>>();

        if (options.Value.Backtest.Enabled)
            return serviceProvider.GetRequiredService<BacktestWorkerService>();

        return serviceProvider.GetRequiredService<WorkerService>();
    });

    services.AddSingleton<IStrategy, StopAtr>();
    services.AddSingleton<IStrategy, StopAtrFt>();
    services.AddSingleton<IStrategy, LinearRegression>();
    services.AddSingleton<IStrategy, LinearRegressionFt>();
    services.AddSingleton<IStrategy, LastBar>();
    services.AddSingleton<IStrategy, LastBarFt>();
    services.AddSingleton<IStrategy, DoubleRsi>();
    services.AddSingleton<IStrategy, DoubleRsiFt>();
    services.AddSingleton<IStrategy, Macd>();
    services.AddSingleton<IStrategy, MacdFt>();
    services.AddSingleton<IStrategy, SuperTrend>();
    services.AddSingleton<IStrategy, SuperTrendFt>();
    services.AddSingleton<IStrategy, Vwap>();
    services.AddSingleton<IStrategy, VwapFt>();
    services.AddSingleton<IStrategy, Kama>();
    services.AddSingleton<IStrategy, KamaFt>();
    services.AddSingleton<IStrategy, HtTrendline>();
    services.AddSingleton<IStrategy, HtTrendlineFt>();
    services.AddSingleton<IStrategy, Mama>();
    services.AddSingleton<IStrategy, MamaFt>();
    services.AddSingleton<IStrategy, T3>();
    services.AddSingleton<IStrategy, T3Ft>();
    services.AddSingleton<IStrategy, Alma>();
    services.AddSingleton<IStrategy, AlmaFt>();
    services.AddSingleton<IStrategy, Sma>();
    services.AddSingleton<IStrategy, SmaFt>();
    services.AddSingleton<IStrategy, Ema>();
    services.AddSingleton<IStrategy, EmaFt>();
    services.AddSingleton<IStrategy, Wma>();
    services.AddSingleton<IStrategy, WmaFt>();
    services.AddSingleton<IStrategy, Vwma>();
    services.AddSingleton<IStrategy, VwmaFt>();
    services.AddSingleton<IStrategy, Dema>();
    services.AddSingleton<IStrategy, DemaFt>();
    services.AddSingleton<IStrategy, Epma>();
    services.AddSingleton<IStrategy, EpmaFt>();
    services.AddSingleton<IStrategy, Hma>();
    services.AddSingleton<IStrategy, HmaFt>();
    services.AddSingleton<IStrategy, Smma>();
    services.AddSingleton<IStrategy, SmmaFt>();
    services.AddSingleton<IStrategy, Tema>();
    services.AddSingleton<IStrategy, TemaFt>();
    services.AddSingleton<IStrategy, KeltnerRainbow>();
    services.AddSingleton<IStrategy, KeltnerRainbowFt>();

    services.AddSingleton<IStrategyFactory, StrategyFactory>();
});

await builder.Build().RunAsync();