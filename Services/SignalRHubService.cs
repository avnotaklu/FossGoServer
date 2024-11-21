using BadukServer.Hubs;
using Microsoft.AspNetCore.SignalR;

public interface ISignalRGameHubService
{
    public ValueTask SendToClient(string connectionId, string methodName, object data, CancellationToken cancellationToken);
    public ValueTask SendToAll(string methodName, object data, CancellationToken cancellationToken);
}

public class SignalRGameHubService : ISignalRGameHubService
{
    private readonly IHubContext<GameHub> _hubContext;
    private readonly ILogger<SignalRGameHubService> _logger;

    public SignalRGameHubService(IHubContext<GameHub> hubContext, ILogger<SignalRGameHubService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public ValueTask SendToClient(string connectionId, string methodName, object data, CancellationToken cancellationToken)
    {
        return new(_hubContext.Clients.Client(connectionId).SendAsync(methodName, data, cancellationToken));
    }
    public ValueTask SendToAll(string methodName, object data, CancellationToken cancellationToken) {
        return new(_hubContext.Clients.All.SendAsync(methodName, data, cancellationToken));
    }
}