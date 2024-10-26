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
    private readonly IHubContext<GameHub> _hubContext;

    // private List<(SiloAddress Host, IRemoteLocationHub Hub)> _hubs = new();
    public PushNotifierGrain(ILogger<PushNotifierGrain> logger, IHubContext<GameHub> hubContext)

    {
        _logger = logger;
        // _hub = hubReference;
        _hubContext = hubContext;
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

    public ValueTask SendMessage(SignalRMessage message, string gameGroup)
    {
        // Add a message to the send queue
        // _messageQueue.Enqueue(message);
        return BroadcastUpdates(message, gameGroup);
        // return new(Flush());
    }

    // private Task Flush()
    // {
    //     if (_flushTask.IsCompleted)
    //     {
    //         _flushTask = FlushInternal();
    //     }

    //     return _flushTask;

    //     async Task FlushInternal()
    //     {
    //         const int maxMessagesPerBatch = 100;
    //         if (_messageQueue.Count == 0) return;

    //         while (_messageQueue.Count > 0)
    //         {
    //             // Send all messages to all SignalR hubs
    //             var messagesToSend = new List<SignalRMessage>(Math.Min(_messageQueue.Count, maxMessagesPerBatch));
    //             while (messagesToSend.Count < maxMessagesPerBatch && _messageQueue.TryDequeue(out SignalRMessage? msg))
    //                 messagesToSend.Add(msg);

    //             // var tasks = new List<Task>(_hubs.Count);
    //             // var batch = new JoinMessagesBatch(messagesToSend);
    //             //
    //             // foreach ((SiloAddress Host, IRemoteGameHub Hub) hub in _hubs)
    //             // {
    //             //     tasks.Add(BroadcastUpdates(hub.Host, hub.Hub, batch, connectionId, _logger));
    //             // }

    //             var batch = new SignalRMessagesBatch(messagesToSend);

    //             // await BroadcastUpdates(_hub.RemoteGameHub, batch, ConnectionId, _logger);
    //             await BroadcastUpdates(batch, ConnectionId, _logger);
    //         }
    //     }
    // }

    // private static async Task BroadcastUpdates(IRemoteGameHub hub, JoinMessagesBatch message,
    //     string connectionId, ILogger logger)
    // {
    //     try
    //     {
    //         await hub.BroadcastUpdates(message, connectionId);
    //     }
    //     catch (Exception ex)
    //     {
    //         logger.LogError(ex, "Error broadcasting to host");
    //     }
    // }

    private ValueTask BroadcastUpdates(SignalRMessage message, string gameGroup)
    {
        try
        {
            return _BroadcastUpdates(message, gameGroup);
            // return ValueTask.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting to host");
            return ValueTask.CompletedTask;
        }
    }


    // TODO: This was done with GrainObserver [IRemoteGameHub], idk why
    public ValueTask _BroadcastUpdates(SignalRMessage messages, string gameGroup) =>
        new(_hubContext.Clients.Client(ConnectionId).SendAsync(
            "gameUpdate", messages, CancellationToken.None));
}