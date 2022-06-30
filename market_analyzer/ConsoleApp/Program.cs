using Application.BackgroupServices;
using Application.Options;
using Application.Services;
using Infrastructure.GrpcServerTerminal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

    services.AddSingleton<IOrderCreator, OrderCreator>();
    services.AddSingleton<IMarketDataWrapper, MarketDataWrapper>();
    services.AddSingleton<IOrderManagementWrapper, OrderManagementWrapper>();

    services.AddSingleton<IRatesStateService, RatesStateService>();
    services.AddSingleton<ILoopService, LoopService>();

    services.AddOperationSettings(configure =>
    {
        configure.Symbol = "WINQ22";
        configure.Date = new(2022, 6, 30);
        configure.ChunkSize = 5000;
        configure.Timeframe = TimeSpan.FromSeconds(2);
        configure.Deviation = 10;
        configure.Magic = 467276;
        configure.ExecOrder = false;
        configure.IndicatorWindow = TimeSpan.FromMinutes(3);
        configure.IndicatorLength = 3;
        configure.IndicatorSignalShift = 1;
    });

    services.AddHostedService<WorkerService>();
});

await builder.Build().RunAsync();