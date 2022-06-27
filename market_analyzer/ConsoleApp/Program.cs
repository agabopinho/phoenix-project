using Application.BackgroupServices;
using Application.Services;
using Infrastructure.Terminal;
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
        var multiplexer = ConnectionMultiplexer.Connect(new ConfigurationOptions
        {
            EndPoints = { "cache:6379" },
            // EndPoints = { "localhost:6379" },
            Password = "istrusted"
        });

        return multiplexer.GetDatabase(0);
    });

    services.AddMarketDataWrapper(configure =>
        configure.Endpoint = "http://host.docker.internal:5051");
    //configure.Endpoint = "http://localhost:5051");

    services.AddSingleton<IMarketDataWrapper, MarketDataWrapper>();

    services.AddSingleton<ITicksService, TicksService>();
    services.AddSingleton<ILoopService, LoopService>();

    services.AddHostedService<WorkerService>();
});

await builder
    .Build()
    .RunAsync();