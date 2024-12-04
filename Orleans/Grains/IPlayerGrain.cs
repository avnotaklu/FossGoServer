using BadukServer;
using BadukServer.Orleans.Grains;

public interface IPlayerGrain : IGrainWithStringKey
{
    // Task<PairingSummary[]> GetAvailableGames();
    Task<List<string>> GetCreatedGames();
    // Task<List<GameSummary>> GetGameSummaries();

    Task InitializePlayer(string connectionId, PlayerType playerType);
    // Task<bool> IsInitializedByOtherDevice(string connectionId);
    Task<string> CreateGame(GameCreationDto creationData, string time);
    // join an existing game
    Task<(Game game, PlayerInfo? otherPlayerData)> JoinGame(string gameId, string time);
    Task LeaveGame(string gameId);

    public Task<string> GetConnectionId();

}