using BadukServer.Hubs;
using BadukServer.Orleans.Grains;
using Microsoft.AspNetCore.SignalR;

namespace BadukServer;

public class HubReference : BackgroundService
{
    private readonly RemoteGameHub _remoteGameHub;
    public readonly IHubContext<GameHub> HubContext;
    public IRemoteGameHub RemoteGameHub;
    private readonly IGrainFactory _grainFactory;

    public HubReference(
        IGrainFactory grainFactory,
        IHubContext<GameHub> hubContext
    )
    {
        HubContext = hubContext;
        _remoteGameHub = new RemoteGameHub(hubContext);
        _grainFactory = grainFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        RemoteGameHub = _grainFactory.CreateObjectReference<IRemoteGameHub>(_remoteGameHub);
        // while (!stoppingToken.IsCancellationRequested)
        // {

        // }

        // TODO: There's supposed to be a infinite while loop here, idk why
    }
}