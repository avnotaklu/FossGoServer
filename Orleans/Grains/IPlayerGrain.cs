using BadukServer;

public interface IPlayerGrain : IGrainWithStringKey
{
    // Task<PairingSummary[]> GetAvailableGames();
    Task<List<string>> GetAvailableGames();
    // Task<List<GameSummary>> GetGameSummaries();

    Task InitializePlayer(string connectionId);
    Task<string> CreateGame(int rows, int columns, int timeInSeconds, Stone stone);
    // join an existing game
    Task<string> JoinGame(string gameId);
    Task LeaveGame(string gameId);

}