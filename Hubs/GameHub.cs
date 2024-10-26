using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace BadukServer.Hubs;

/// <summary>
/// The hub which Web clients connect to receive location updates. Messages are broadcast by <see cref="RemoteGameHub"/> using <see cref="IHubContext{GameHub}"/>.
/// </summary>
public sealed class GameHub : Hub
{

    private readonly ILogger<AuthenticationController> _logger;

    [ActivatorUtilitiesConstructor]
    public GameHub(ILogger<AuthenticationController> logger) {
        _logger = logger;
    }
    public override Task OnConnectedAsync()
    {
        _logger.LogInformation("User connected : {id}", Context.ConnectionId);
        return base.OnConnectedAsync();
    }
}
