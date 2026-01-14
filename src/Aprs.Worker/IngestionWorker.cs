using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Aprs.Application.Packets.Commands.IngestPacket;
using Aprs.Domain.Entities;
using Aprs.Sdk;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aprs.Worker;

/// <summary>
/// Background service that ingests APRS packets from APRS-IS and processes them.
/// Uses Channel&lt;T&gt; for backpressure handling to prevent memory issues under high load.
/// </summary>
public sealed class IngestionWorker : BackgroundService
{
    private readonly AprsClient _client;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<IngestionWorker> _logger;
    private readonly string _callsign;
    private readonly string _password;
    private readonly string _filter;
    private readonly IConfiguration _configuration;
    
    // Channel for backpressure - bounded to prevent memory issues
    private readonly Channel<AprsPacket> _packetChannel;
    private const int ChannelCapacity = 10_000;
    private const int ProcessorCount = 4;

    public IngestionWorker(
        AprsClient client, 
        IServiceScopeFactory scopeFactory, 
        ILogger<IngestionWorker> logger,
        IConfiguration configuration)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        
        _callsign = _configuration["Aprs:Callsign"] ?? "N0CALL";
        _password = _configuration["Aprs:Password"] ?? "-1";
        _filter = _configuration["Aprs:Filter"] ?? "r/52/21/500";
        
        // Create bounded channel for backpressure
        _packetChannel = Channel.CreateBounded<AprsPacket>(new BoundedChannelOptions(ChannelCapacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = false,
            SingleWriter = true
        });
        
        if (_callsign == "N0CALL")
        {
            _logger.LogWarning("Using default unconfigured callsign 'N0CALL'. Set 'Aprs:Callsign' in configuration.");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Ingestion Worker starting with {ProcessorCount} processors and channel capacity {Capacity}",
            ProcessorCount, ChannelCapacity);

        _client.PacketReceived += OnPacketReceived;
        _client.Disconnected += OnClientDisconnected;

        // Start multiple packet processors
        var processorTasks = new List<Task>(ProcessorCount);
        for (var i = 0; i < ProcessorCount; i++)
        {
            var processorId = i;
            processorTasks.Add(Task.Run(() => ProcessPacketsAsync(processorId, stoppingToken), stoppingToken));
        }

        // Auto-reconnect loop
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!_client.IsConnected)
                {
                    await _client.ConnectAsync(_callsign, _password, _filter, cancellationToken: stoppingToken);
                }
                
                // Wait a bit to check health
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                
                // Log channel stats periodically
                var count = _packetChannel.Reader.Count;
                if (count > ChannelCapacity / 2)
                {
                    _logger.LogWarning("Packet channel is {Percent}% full ({Count}/{Capacity})", 
                        count * 100 / ChannelCapacity, count, ChannelCapacity);
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Ingestion Loop. Retrying in 5s...");
                await Task.Delay(5000, stoppingToken);
            }
        }

        // Signal channel completion and wait for processors
        _packetChannel.Writer.Complete();
        
        try
        {
            await Task.WhenAll(processorTasks).WaitAsync(TimeSpan.FromSeconds(30), stoppingToken);
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("Packet processors did not complete within timeout.");
        }

        await _client.DisconnectAsync();
        _logger.LogInformation("Ingestion Worker stopped.");
    }

    private void OnClientDisconnected()
    {
        _logger.LogWarning("APRS Client Disconnected. Worker loop will attempt reconnect.");
    }

    private void OnPacketReceived(AprsPacket packet)
    {
        // Write to channel - will drop oldest if full (backpressure)
        if (!_packetChannel.Writer.TryWrite(packet))
        {
            _logger.LogWarning("Failed to enqueue packet from {Sender} - channel may be full", packet.Sender);
        }
    }

    private async Task ProcessPacketsAsync(int processorId, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Packet processor {ProcessorId} started", processorId);
        var processedCount = 0;

        try
        {
            await foreach (var packet in _packetChannel.Reader.ReadAllAsync(cancellationToken))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                    
                    await mediator.Send(new IngestPacketCommand(packet), cancellationToken);
                    processedCount++;
                    
                    if (processedCount % 1000 == 0)
                    {
                        _logger.LogDebug("Processor {ProcessorId} has processed {Count} packets", processorId, processedCount);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Processor {ProcessorId}: Error processing packet from {Sender}", 
                        processorId, packet.Sender);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
        }
        
        _logger.LogInformation("Packet processor {ProcessorId} stopped. Total processed: {Count}", processorId, processedCount);
    }
}
