public interface IPlayerGrain : IGrainWithStringKey
{
    // Task<PairingSummary[]> GetAvailableGames();
    // Task<PairingSummary[]> GetAvailableGames();
    // Task<List<GameSummary>> GetGameSummaries();

    Task<Guid> CreateGame();

    // join an existing game
    Task<GameState> JoinGame(Guid gameId);

    Task LeaveGame(Guid gameId);

}