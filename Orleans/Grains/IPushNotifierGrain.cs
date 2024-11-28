namespace BadukServer.Orleans.Grains;

// String key is user id
public interface IPushNotifierGrain : IGrainWithStringKey
{
    public ValueTask SendMessageToMe(SignalRMessage message);
    // public ValueTask SendMessageToAll(SignalRMessage message, string gameGroup);
    public Task<string> GetConnectionId();
    ValueTask InitializeNotifier(string connectionId);
}
