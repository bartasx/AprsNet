using System.Threading.RateLimiting;
using Aprs.Api.Hubs;
using Aprs.Api.Services;
using Aprs.Application.Common.Behaviors;
using Aprs.Infrastructure.Persistence;
using Asp.Versioning;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();
builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "APRS.NET API",
        Version = "v1",
        Description = "High-performance APRS packet ingestion and query API"
    });
});

// OpenTelemetry Metrics
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: "AprsNet.Api", serviceVersion: "1.0.0"))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter("AprsNet.Metrics")
        .AddPrometheusExporter());

// SignalR for real-time streaming
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.MaximumReceiveMessageSize = 32 * 1024; // 32KB max message
});
builder.Services.AddSingleton<IPacketBroadcaster, SignalRPacketBroadcaster>();

// API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-Api-Version"));
});

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("fixed", limiterOptions =>
    {
        limiterOptions.PermitLimit = 100;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 10;
    });

    options.AddSlidingWindowLimiter("sliding", limiterOptions =>
    {
        limiterOptions.PermitLimit = 1000;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.SegmentsPerWindow = 4;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 10;
    });
});

// Health Checks
builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is required"),
        name: "postgresql",
        tags: ["db", "sql", "postgresql"])
    .AddRedis(
        builder.Configuration.GetConnectionString("Redis")
            ?? "localhost:6379",
        name: "redis",
        tags: ["cache", "redis"]);

// Database
builder.Services.AddDbContext<AprsDbContext>(options =>
{
    var conn = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("DefaultConnection string is required");
    options.UseNpgsql(conn);
});

// Repositories
builder.Services.AddScoped<Aprs.Domain.Interfaces.IPacketRepository, Aprs.Infrastructure.Repositories.PacketRepository>();

// Caching (Redis)
builder.Services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(sp =>
{
    var configuration = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    return StackExchange.Redis.ConnectionMultiplexer.Connect(configuration);
});
builder.Services.AddSingleton<Aprs.Application.Interfaces.ICacheService, Aprs.Infrastructure.Services.RedisCacheService>();

// FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(Aprs.Application.Packets.Queries.GetPackets.GetPacketsQuery).Assembly);

// MediatR with Validation Pipeline
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Aprs.Application.Packets.Queries.GetPackets.GetPacketsQuery).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthorization();

// Map Health Checks
app.MapHealthChecks("/health");

// Map Prometheus metrics endpoint
app.MapPrometheusScrapingEndpoint("/metrics");

// Map SignalR Hub for real-time packet streaming
app.MapHub<PacketHub>("/hubs/packets");

app.MapControllers();

app.Run();
