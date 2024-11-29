using Microsoft.Extensions.Hosting;
using Orleans;
using System.Threading;
using System.Threading.Tasks;

public class SignalRListenerService : BackgroundService
{
    private readonly IClusterClient _clusterClient;

    public SignalRListenerService(IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Replace with your grain interface and listener logic
        // var grain = _clusterClient.GetGrain<IMyGrain>(0);
        // while (!stoppingToken.IsCancellationRequested)
        // {
        //     var data = await grain.GetUpdatesAsync(); // Hypothetical method
        //     if (data != null)
        //     {
        //         Console.WriteLine($"Data received: {data}");
        //     }

        //     // Add a delay or use notifications to avoid busy-waiting
        //     await Task.Delay(1000, stoppingToken);
        // }
        return Task.CompletedTask;
    }



    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        // Cleanup logic if needed
        await base.StopAsync(stoppingToken);
    }
}
