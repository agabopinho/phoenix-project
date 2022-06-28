using Application.BackgroupServices;
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

    services.AddSingleton<IMarketDataWrapper, MarketDataWrapper>();
    services.AddSingleton<IOrderManagementWrapper, OrderManagementWrapper>();

    services.AddSingleton<IRatesStateService, RatesStateService>();
    services.AddSingleton<ILoopService, LoopService>();

    services.AddHostedService<WorkerService>();
});

await builder
    .Build()
    .RunAsync();