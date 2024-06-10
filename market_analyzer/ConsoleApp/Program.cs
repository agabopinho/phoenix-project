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

    services.AddSingleton<OnlineDateProvider>();
    services.AddSingleton<IDateProvider, OnlineDateProvider>();

    services.AddSingleton<LoopService>();
    services.AddSingleton<ILoopService, LoopService>();

    services.AddHostedService<WorkerService>();
});

await builder.Build().RunAsync();