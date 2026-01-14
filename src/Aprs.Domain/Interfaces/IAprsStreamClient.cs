using System;
using System.Threading;
using System.Threading.Tasks;

namespace Aprs.Domain.Interfaces;

public interface IAprsStreamClient : IDisposable, IAsyncDisposable
{
    event Action<string> MessageReceived;
    event Action<bool> Validated;
    event Action Disconnected;

    bool IsConnected { get; }

    Task ConnectAsync(string server, int port, string callsign, string password, string filter, CancellationToken cancellationToken);
    Task DisconnectAsync();
}
