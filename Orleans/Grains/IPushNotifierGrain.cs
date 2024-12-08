namespace BadukServer.Orleans.Grains;

// String key is user id
public interface IPushNotifierGrain : IGrainWithStringKey
{
    public ValueTask SendMessageToMe(SignalRMessage message);
    public ValueTask SendMessageToSameType(SignalRMessage message);
    public ValueTask SendMessageToAllUsers(SignalRMessage message);
    public Task<string> GetConnectionId();
    public Task<string> GetPlayerId();
    ValueTask InitializeNotifier(string playerId, PlayerType playerType);
}
