using BadukServer.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace BadukServer.Orleans.Grains;


// NOTE: this is unused in the current implementation
/// <summary>
/// Broadcasts game messages to clients which are connected to the local SignalR hub.
/// </summary>
internal sealed class RemoteGameHub : IRemoteGameHub
{
    private readonly IHubContext<MainHub> _hub;

    public RemoteGameHub(IHubContext<MainHub> hub) => _hub = hub;

    // Send a message to every client which is connected to the hub
    public ValueTask BroadcastUpdates(SignalRMessagesBatch messages, string connectionId) =>
        new(_hub.Clients.Client(connectionId).SendAsync(
            "gameUpdate", messages, CancellationToken.None));
}
