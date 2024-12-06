using BadukServer.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace BadukServer.Orleans.Grains;

// [Reentrant]
// [StatelessWorker(maxLocalWorkers: 12)]
public class PushNotifierGrain : Grain, IPushNotifierGrain
{
    private readonly ILogger<PushNotifierGrain> _logger;
    private string ConnectionId => this.GetPrimaryKeyString();
    private string _player = null!;
    private PlayerType? _playerType = null!;
    private readonly ISignalRHubService _hubService;
    private bool _isInitialized = false;

    public PushNotifierGrain(ILogger<PushNotifierGrain> logger, ISignalRHubService hub)
    {
        _logger = logger;
        _hubService = hub;
    }


    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {

        await base.OnActivateAsync(cancellationToken);
    }

    public ValueTask InitializeNotifier(string playerId, PlayerType playerType)
    {
        _player = playerId;
        _playerType = playerType;
        _hubService.AddToGroup(ConnectionId, playerType.ToTypeString(), CancellationToken.None);
        _isInitialized = true;

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

    public async void SendMessageToMe(SignalRMessage message)
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

    public async void SendMessageToAllUsers(SignalRMessage message)
    {
        try
        {
            _logger.LogInformation("Notification sent to allusers, <message>{message}<message>", message);
            await _hubService.SendToGroup("userUpdate", "Users", message, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting to host");
        }
    }

    public async void SendMessageToSameType(SignalRMessage message)
    {
        try
        {
            var group = _playerType?.ToTypeString()!;
            _logger.LogInformation("Notification sent to <group>{group}<group>, <message>{message}<message>", group, message);
            await _hubService.SendToGroup("userUpdate", group, message, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting to host");
        }
    }
}