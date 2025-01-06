using BadukServer.Hubs;
using Microsoft.AspNetCore.SignalR;
using Orleans.Streams;

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
    private ConnectionStrength connectionStrength;
    private IDisposable _timerHandle = null!;
    private bool _isTimerActive;

    public PushNotifierGrain(ILogger<PushNotifierGrain> logger, ISignalRHubService hub)
    {
        _logger = logger;
        _hubService = hub;
        connectionStrength = new ConnectionStrength(0);
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

    public ValueTask SendMessageToMe(SignalRMessage message)
    {
        try
        {
            // _logger.LogInformation("Notification sent to <user>{user}<user>, <message>{message}<message>", ConnectionId, message);
            return _hubService.SendToClient("userUpdate", ConnectionId, message, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting to host");
            return new();
        }
    }

    public ValueTask SendMessageToAllUsers(SignalRMessage message)
    {
        try
        {
            _logger.LogInformation("Notification sent to allusers, <message>{message}<message>", message);
            return _hubService.SendToGroup("userUpdate", "Users", message, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting to host");
            return new();
        }
    }

    public ValueTask SendMessageToSameType(SignalRMessage message)
    {
        try
        {
            var group = _playerType?.ToTypeString()!;
            _logger.LogInformation("Notification sent to <group>{group}<group>, <message>{message}<message>", group, message);
            return _hubService.SendToGroup("userUpdate", group, message, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting to host");
            return new();
        }
    }

    public Task<ConnectionStrength> GetConnectionStrength()
    {
        return Task.FromResult(connectionStrength);
    }

    public Task SetConnectionStrength(ConnectionStrength strength)
    {
        connectionStrength = strength;

        if (connectionStrength.Ping < ConnectionStrength.Worst)
        {
            SetupTimerForStrengthDecay();
        }

        return Task.CompletedTask;
    }

    public Task PlayerConnectionChanged()
    {
        _timerHandle?.Dispose();
        DeactivateOnIdle();
        return Task.CompletedTask;
    }

    private Task SetupTimerForStrengthDecay()
    {
        _timerHandle?.Dispose();
        _timerHandle = this.RegisterGrainTimer(
            DecayStrength,
            this,
            TimeSpan.FromSeconds(3),
            TimeSpan.FromSeconds(3)
        );
        return Task.CompletedTask;
    }

    private async Task DecayStrength(object? _)
    {
        await SetConnectionStrength(new ConnectionStrength((int)MathF.Min(connectionStrength.Ping * 2, ConnectionStrength.Worst))); // TODO: doubling the ping is a temporary solution
    }
}