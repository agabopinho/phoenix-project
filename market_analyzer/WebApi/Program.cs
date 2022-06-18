using Application.Services;
using Serilog;
using Serilog.Events;
using StackExchange.Redis;
using WebApi.BackgroupServices;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.AddSingleton(serviceProvider =>
{
    var connection = ConnectionMultiplexer.Connect("localhost:6379");
    return connection.GetDatabase();
});

builder.Services.AddSingleton<ILoopService, LoopService>();
builder.Services.AddSingleton<IStateService, StateService>();
builder.Services.AddSingleton<IRatesService, RatesService>();

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHostedService<WorkerService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();