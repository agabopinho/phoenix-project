using Application.BackgroupServices;
using Application.Options;
using Application.Services;
using Application.Services.Providers.Cycle;
using Application.Services.Providers.Database;
using Application.Services.Providers.Rates;
using Infrastructure.GrpcServerTerminal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using StackExchange.Redis;

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

builder.ConfigureServices(services =>
{
    services.AddSingleton(_ =>
    {
        var options = new ConfigurationOptions
        {
            EndPoints = { "cache:6379" },
            Password = "istrusted"
        };

        var multiplexer = ConnectionMultiplexer.Connect(options);
        return multiplexer.GetDatabase(0);
    });

    services.AddMarketDataWrapper(configure =>
        configure.Endpoint = "http://host.docker.internal:5051");
    services.AddOrderManagementWrapper(configure =>
        configure.Endpoint = "http://host.docker.internal:5051");

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

    services.AddOperationSettings(configure =>
    {
        configure.MarketData.Symbol = "WINQ22";
        configure.MarketData.Date = new(2022, 6, 30);
        configure.MarketData.Timeframe = TimeSpan.FromSeconds(5);
        configure.MarketData.TimeZoneId = "America/Sao_Paulo";

        configure.Backtest.Enabled = true;
        configure.Backtest.Start = new TimeOnly(9, 10, 0);
        configure.Backtest.End = new TimeOnly(17, 30, 0);
        configure.Backtest.Step = TimeSpan.FromMilliseconds(200);

        configure.Order.Deviation = 10;
        configure.Order.Magic = 467276;
        configure.Order.ExecOrder = false;

        configure.Indicator.Window = TimeSpan.FromMinutes(5);
        configure.Indicator.Length = 3;
        configure.Indicator.SignalShift = 1;

        configure.Infra.ChunkSize = 5000;
    });

    services.AddSingleton<IBacktestDatabaseProvider, BacktestDatabaseProvider>();
    services.AddSingleton<ILoopService, LoopService>();
    services.AddHostedService<WorkerService>();
});

await builder.Build().RunAsync();