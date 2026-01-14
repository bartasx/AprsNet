using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aprs.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Aprs.Infrastructure.Network;

public class AprsIsClient : IAprsStreamClient
{
    private readonly ILogger<AprsIsClient> _logger;
    private TcpClient? _tcpClient;
    private NetworkStream? _stream;
    private StreamReader? _reader;
    private StreamWriter? _writer;
    private CancellationTokenSource? _cts;
    private Task? _readTask;

    public event Action<string>? MessageReceived;
    public event Action<bool>? Validated;
    public event Action? Disconnected;

    public bool IsConnected => _tcpClient?.Connected ?? false;

    public AprsIsClient(ILogger<AprsIsClient> logger)
    {
        _logger = logger;
    }

    public async Task ConnectAsync(string server, int port, string callsign, string password, string filter, CancellationToken cancellationToken)
    {
        if (IsConnected) throw new InvalidOperationException("Already connected.");

        _logger.LogInformation("Connecting to APRS-IS {Server}:{Port} as {Callsign}", server, port, callsign);

        _tcpClient = new TcpClient();
        await _tcpClient.ConnectAsync(server, port, cancellationToken);
        
        _stream = _tcpClient.GetStream();
        _reader = new StreamReader(_stream, Encoding.ASCII);
        _writer = new StreamWriter(_stream, Encoding.ASCII) { AutoFlush = true };
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // Login
        string loginLine = $"user {callsign} pass {password} vers AprsNet 1.0";
        if (!string.IsNullOrWhiteSpace(filter))
        {
            loginLine += $" filter {filter}";
        }
        
        await _writer.WriteLineAsync(loginLine.AsMemory(), cancellationToken);
        _logger.LogDebug("Sent login: {LoginLine}", loginLine);

        // Start Reading Loop
        _readTask = Task.Run(() => ReadLoopAsync(_cts.Token), cancellationToken);
    }

    private async Task ReadLoopAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested && IsConnected && _reader != null)
            {
                string? line = await _reader.ReadLineAsync(token);
                if (line == null)
                {
                    _logger.LogWarning("APRS-IS Connection closed by remote host.");
                    break;
                }

                if (string.IsNullOrWhiteSpace(line)) continue;

                if (line.StartsWith('#'))
                {
                    HandleServerMessage(line);
                }
                else
                {
                    MessageReceived?.Invoke(line);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error receiving data from APRS-IS");
        }
        finally
        {
            await DisconnectAsync();
        }
    }

    private void HandleServerMessage(string line)
    {
        _logger.LogDebug("Server Message: {Line}", line);
        if (line.StartsWith("# logresp"))
        {
            // # logresp USER verified server SERVER
            bool verified = line.Contains(" verified", StringComparison.OrdinalIgnoreCase);
            Validated?.Invoke(verified);
            if (!verified)
            {
                _logger.LogWarning("APRS-IS Login Unverified: {Line}", line);
            }
            else
            {
                _logger.LogInformation("APRS-IS Login Verified.");
            }
        }
    }

    public async Task DisconnectAsync()
    {
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }

        if (_readTask != null)
        {
             try { await _readTask; } catch { }
             _readTask = null;
        }

        _writer?.Dispose();
        _reader?.Dispose();
        _stream?.Dispose();
        _tcpClient?.Dispose();

        _writer = null;
        _reader = null;
        _stream = null;
        _tcpClient = null;

        Disconnected?.Invoke();
        _logger.LogInformation("Disconnected from APRS-IS.");
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            
            _writer?.Dispose();
            _reader?.Dispose();
            _stream?.Dispose();
            _tcpClient?.Dispose();

            _writer = null;
            _reader = null;
            _stream = null;
            _tcpClient = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync().ConfigureAwait(false);
        Dispose(disposing: false);
        GC.SuppressFinalize(this);
    }
}
