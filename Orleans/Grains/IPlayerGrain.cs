public interface IPlayerGrain : IGrainWithStringKey
{
    // Task<PairingSummary[]> GetAvailableGames();
    // Task<PairingSummary[]> GetAvailableGames();
    // Task<List<GameSummary>> GetGameSummaries();

    Task<string> CreateGame(int rows, int columns, int timeInSeconds, string user_id);

    // join an existing game
    Task<string> JoinGame(string gameId);

    Task LeaveGame(string gameId);

}