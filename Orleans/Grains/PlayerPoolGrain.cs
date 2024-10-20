

public class PlayerPoolGrain : Grain, IPlayerPoolGrain
{
    private HashSet<string> activePlayers = [];

    public Task AddActivePlayer(string playerId)
    {
        activePlayers.Add(playerId);
        return Task.CompletedTask;
    }

    public Task<HashSet<string>> GetActivePlayers()
    {
        return Task.FromResult(activePlayers);
    }

    public Task RemoveActivePlayer(string playerId)
    {
        activePlayers.Remove(playerId);
        return Task.CompletedTask;
    }
}