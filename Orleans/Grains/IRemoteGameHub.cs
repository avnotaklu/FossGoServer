namespace BadukServer.Orleans.Grains;

public interface IRemoteGameHub : IGrainObserver
{
    // TODO: this message used to be batch of messages, what's gonna happen with one message?
    ValueTask BroadcastUpdates(SignalRMessagesBatch message, string connectionId);
}
