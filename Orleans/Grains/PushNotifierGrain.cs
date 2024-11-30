using BadukServer.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace BadukServer.Orleans.Grains;

// [Reentrant]
// [StatelessWorker(maxLocalWorkers: 12)]
public class PushNotifierGrain : Grain, IPushNotifierGrain
{
    private readonly Queue<SignalRMessage> _messageQueue = new();
    private readonly ILogger<PushNotifierGrain> _logger;
    private string ConnectionId => this.GetPrimaryKeyString();
    private string _player = null!;
    private readonly ISignalRHubService _hubService;

    public PushNotifierGrain(ILogger<PushNotifierGrain> logger, ISignalRHubService hub)
    {
        _logger = logger;
        _hubService = hub;
    }

    private Task _flushTask = Task.CompletedTask;

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {

        await base.OnActivateAsync(cancellationToken);
    }

    public ValueTask InitializeNotifier(string playerId)
    {
        _player = playerId;
        return new ValueTask();
    }

    public Task<string> GetPlayerId()
    {
        return Task.FromResult(_player);
    }


    public override async Task OnDeactivateAsync(DeactivationReason deactivationReason,
        CancellationToken cancellationToken)
    {
        await base.OnDeactivateAsync(deactivationReason, cancellationToken);
    }

    public Task<string> GetConnectionId()
    {
        return Task.FromResult(ConnectionId);
    }

    public async ValueTask SendMessageToMe(SignalRMessage message)
    {
        try
        {
            _logger.LogInformation("Notification sent to <user>{user}<user>, <message>{message}<message>", ConnectionId, message);
            await _hubService.SendToClient(ConnectionId, "userUpdate", message, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting to host");
        }
    }
}