namespace BadukServer.Orleans.Grains;

// String key is user id
public interface IPushNotifierGrain : IGrainWithStringKey
{
    ValueTask SendMessage(JoinMessage message);
    ValueTask InitializeNotifier(string connectionId);
}
