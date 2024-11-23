using BadukServer;

public interface IPlayerGrain : IGrainWithStringKey
{
    // Task<PairingSummary[]> GetAvailableGames();
    Task<List<string>> GetCreatedGames();
    // Task<List<GameSummary>> GetGameSummaries();

    Task InitializePlayer(string connectionId);
    // Task<bool> IsInitializedByOtherDevice(string connectionId);
    Task<string> CreateGame(int rows, int columns, TimeControl timeControl,  StoneSelectionType stone, string time);
    // join an existing game
    Task<string> JoinGame(string gameId, string time);
    Task LeaveGame(string gameId);

}