using Application.Options;
using Application.Services;
using Application.Services.Providers.Cycle;
using Application.Services.Providers.Rates;
using Application.Services.Providers.Rates.BacktestRates;
using Application.Workers;
using ConsoleApp.Converters;
using Infrastructure.GrpcServerTerminal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using System.ComponentModel;

namespace ConsoleApp;

public class Program
{
    public static async Task Main(string[] args)
    {
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

        Configure(builder);

        await builder.Build().RunAsync();
    }

    private static void Configure(IHostBuilder builder)
    {
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
        });
    }
}