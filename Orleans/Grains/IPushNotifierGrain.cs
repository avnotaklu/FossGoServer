namespace BadukServer.Orleans.Grains;

// String key is user id
public interface IPushNotifierGrain : IGrainWithStringKey
{
    ValueTask SendMessage(SignalRMessage message, string gameGroup);
    ValueTask InitializeNotifier(string connectionId);
}
