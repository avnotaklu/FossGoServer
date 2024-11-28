using BadukServer.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace BadukServer.Orleans.Grains;

// [Reentrant]
// [StatelessWorker(maxLocalWorkers: 12)]
public class PushNotifierGrain : Grain, IPushNotifierGrain
{
    private readonly Queue<SignalRMessage> _messageQueue = new();
    private readonly ILogger<PushNotifierGrain> _logger;
    private string? _connectionId;
    private string ConnectionId => _connectionId ?? throw new NullReferenceException("_connectionId is not set");
    // private readonly HubReference _hub;
    private readonly ISignalRGameHubService _hubService;

    // private List<(SiloAddress Host, IRemoteLocationHub Hub)> _hubs = new();
    public PushNotifierGrain(ILogger<PushNotifierGrain> logger, ISignalRGameHubService hub)

    {
        _logger = logger;
        _hubService = hub;
    }

    private Task _flushTask = Task.CompletedTask;

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {

        await base.OnActivateAsync(cancellationToken);
    }

    public override async Task OnDeactivateAsync(DeactivationReason deactivationReason,
        CancellationToken cancellationToken)
    {
        await base.OnDeactivateAsync(deactivationReason, cancellationToken);
    }
    public ValueTask InitializeNotifier(string connectionId)
    {
        _connectionId = connectionId ?? throw new ArgumentNullException(nameof(connectionId));
        return new ValueTask();
    }

    public Task<string> GetConnectionId() {
        return Task.FromResult(ConnectionId);
    }

    public ValueTask SendMessageToMe(SignalRMessage message)
    {
        try
        {
            _logger.LogInformation("Notification sent to <user>{user}<user>, <message>{message}<message>", ConnectionId, message);
            return _hubService.SendToClient(ConnectionId, "userUpdate", message, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting to host");
            return ValueTask.CompletedTask;
        }
    }


    // public ValueTask _SendUpdateToClient(SignalRMessage messages, string connectionId)
    // {
    // }

    // public ValueTask _SendUpdatesAll(SignalRMessage messages, string gameGroup)
    // {
    // }
}