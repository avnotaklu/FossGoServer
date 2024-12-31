using Orleans.Streams;

namespace BadukServer.Orleans.Grains;

// String key is user id
public interface IPushNotifierGrain : IGrainWithStringKey
{
    public ValueTask SendMessageToMe(SignalRMessage message);
    public ValueTask SendMessageToSameType(SignalRMessage message);
    public ValueTask SendMessageToAllUsers(SignalRMessage message);
    public Task<string> GetConnectionId();
    public Task<string> GetPlayerId();
    public Task SetConnectionStrength(ConnectionStrength strength);
    public Task<ConnectionStrength> GetConnectionStrength();
    public Task<IAsyncStream<ConnectionStrength>> ConnectionStrengthStream();
    ValueTask InitializeNotifier(string playerId, PlayerType playerType);

    Task PlayerConnectionChanged();
}
