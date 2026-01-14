using Aprs.Application.Common.Behaviors;
using Aprs.Domain.Interfaces;
using Aprs.Infrastructure.Network;
using Aprs.Infrastructure.Parsers;
using Aprs.Infrastructure.Persistence;
using Aprs.Infrastructure.Repositories;
using Aprs.Sdk;
using Aprs.Worker;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Serilog;

// Setup Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);
    builder.Services.AddSerilog();

    // Infrastructure / Domain Services
    builder.Services.AddSingleton<IAprsStreamClient, AprsIsClient>();
    builder.Services.AddSingleton<IPacketParser, AprsPacketParser>();
    builder.Services.AddSingleton<AprsClient>();

    // Database
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is required. Set it via environment variable or appsettings.json");

    builder.Services.AddDbContext<AprsDbContext>(options =>
    {
        options.UseNpgsql(connectionString);
    });

    builder.Services.AddScoped<IPacketRepository, PacketRepository>();

    // Caching (Redis)
    var redisConfig = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    builder.Services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(sp =>
    {
        return StackExchange.Redis.ConnectionMultiplexer.Connect(redisConfig);
    });
    builder.Services.AddSingleton<Aprs.Application.Interfaces.ICacheService, Aprs.Infrastructure.Services.RedisCacheService>();

    // FluentValidation
    builder.Services.AddValidatorsFromAssembly(typeof(Aprs.Application.Packets.Commands.IngestPacket.IngestPacketCommand).Assembly);

    // MediatR with Validation Pipeline
    builder.Services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssembly(typeof(Aprs.Application.Packets.Commands.IngestPacket.IngestPacketCommand).Assembly);
        cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    });

    // Worker
    builder.Services.AddHostedService<IngestionWorker>();

    var host = builder.Build();
    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application start-up failed");
}
finally
{
    Log.CloseAndFlush();
}
