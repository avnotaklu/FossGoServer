using BadukServer.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace BadukServer.Orleans.Grains;

/// <summary>
/// Broadcasts location messages to clients which are connected to the local SignalR hub.
/// </summary>
internal sealed class RemoteGameHub : IRemoteGameHub
{
    private readonly IHubContext<GameHub> _hub;

    public RemoteGameHub(IHubContext<GameHub> hub) => _hub = hub;

    // Send a message to every client which is connected to the hub
    public ValueTask BroadcastUpdates(SignalRMessagesBatch messages, string connectionId) =>
        new(_hub.Clients.Client(connectionId).SendAsync(
            "gameUpdate", messages, CancellationToken.None));
}
