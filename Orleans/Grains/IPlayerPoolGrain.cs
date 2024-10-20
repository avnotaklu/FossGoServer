public interface IPlayerPoolGrain : IGrainWithIntegerKey {
    public Task<HashSet<string>> GetActivePlayers();
    public Task AddActivePlayer(string playerId);
    public Task RemoveActivePlayer(string playerId);
}