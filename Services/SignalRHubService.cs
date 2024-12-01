using BadukServer.Hubs;
using Microsoft.AspNetCore.SignalR;

public interface ISignalRHubService
{
    public ValueTask SendToClient(string connectionId, string methodName, object data, CancellationToken cancellationToken);
    public ValueTask SendToAll(string methodName, object data, CancellationToken cancellationToken);
    public ValueTask SendToGroup(string methodName, string group, object data, CancellationToken cancellationToken);
    public ValueTask AddToGroup(string connectionId, string group, CancellationToken cancellationToken);
}

public class SignalRHubService : ISignalRHubService
{
    private readonly IHubContext<MainHub> _hubContext;
    private readonly ILogger<SignalRHubService> _logger;

    public SignalRHubService(IHubContext<MainHub> hubContext, ILogger<SignalRHubService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public ValueTask SendToClient(string connectionId, string methodName, object data, CancellationToken cancellationToken)
    {
        return new(_hubContext.Clients.Client(connectionId).SendAsync(methodName, data, cancellationToken));
    }
    public ValueTask SendToAll(string methodName, object data, CancellationToken cancellationToken)
    {
        return new(_hubContext.Clients.All.SendAsync(methodName, data, cancellationToken));
    }

    public ValueTask SendToGroup(string methodName, string group, object data, CancellationToken cancellationToken)
    {
        return new(_hubContext.Clients.Group(group).SendAsync(methodName, data, cancellationToken));
    }

    public ValueTask AddToGroup(string connectionId, string group, CancellationToken cancellationToken)
    {
        return new(_hubContext.Groups.AddToGroupAsync(connectionId, group, cancellationToken));
    }
}
