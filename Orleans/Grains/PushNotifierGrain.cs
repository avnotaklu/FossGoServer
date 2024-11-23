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
        // Set up a timer to regularly flush the message queue
        // RegisterTimer(
        //     _ =>
        //     {
        //         Flush();
        //         return Task.CompletedTask;
        //     },
        //     null,
        //     TimeSpan.FromMilliseconds(15),
        //     TimeSpan.FromMilliseconds(15));


        // Set up a timer to regularly refresh the hubs, to respond to azure infrastructure changes
        // await RefreshHubs();
        // RegisterTimer(
        //     async _ => await RefreshHubs(),
        //     state: null,
        //     dueTime: TimeSpan.FromSeconds(60),
        //     period: TimeSpan.FromSeconds(60));

        await base.OnActivateAsync(cancellationToken);
    }

    public override async Task OnDeactivateAsync(DeactivationReason deactivationReason,
        CancellationToken cancellationToken)
    {
        // await Flush();
        await base.OnDeactivateAsync(deactivationReason, cancellationToken);
    }

    // private async ValueTask RefreshHubs()
    // {
    //     // Discover the current infrastructure
    //     IHubListGrain hubListGrain = GrainFactory.GetGrain<IHubListGrain>(Guid.Empty);
    //     _hubs = await hubListGrain.GetHubs();
    // }
    //

    public ValueTask InitializeNotifier(string connectionId)
    {
        _connectionId = connectionId ?? throw new ArgumentNullException(nameof(connectionId));
        return new ValueTask();
    }

    public ValueTask SendMessage(SignalRMessage message, string gameGroup, bool toMe = true)
    {
        _logger.LogInformation("Notification sent to <users>{users}<users>, <message>{message}<message>", toMe ? ConnectionId : "All", message);
        return SendUpdate(message, gameGroup, toMe ? ConnectionId : null);
        // return new(Flush());
    }

    private ValueTask SendUpdate(SignalRMessage message, string gameGroup, string? connectionId = null)
    {
        try
        {
            if (connectionId != null)
            {
                return _SendUpdate(message, gameGroup);
            }
            else
            {
                return _SendUpdatesAll(message, gameGroup);
            }
            // return ValueTask.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting to host");
            return ValueTask.CompletedTask;
        }
    }


    // TODO: This was done with GrainObserver [IRemoteGameHub], idk why
    public ValueTask _SendUpdate(SignalRMessage messages, string gameGroup) =>
        _hubService.SendToClient(ConnectionId, "gameUpdate", messages, CancellationToken.None);

    public ValueTask _SendUpdatesAll(SignalRMessage messages, string gameGroup) =>
        _hubService.SendToAll("gameUpdate", messages, CancellationToken.None);
}