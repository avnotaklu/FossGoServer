using BadukServer;
using BadukServer.Orleans.Grains;

public interface IPlayerGrain : IGrainWithStringKey
{
    // Task<PairingSummary[]> GetAvailableGames();
    Task<List<string>> GetCreatedGames();
    // Task<List<GameSummary>> GetGameSummaries();

    Task InitializePlayer(string connectionId, PlayerType playerType);
    // Task<bool> IsInitializedByOtherDevice(string connectionId);
    Task<string> CreateGame(GameCreationDto creationData, DateTime time);
    // join an existing game
    Task<(Game game, PlayerInfo? otherPlayerData)> JoinGame(string gameId, DateTime time);

    Task InformMyJoin(Game game, List<PlayerInfo> players, DateTime time, PlayerJoinMethod joinMethod);
    Task LeaveGame(string gameId);

    public Task<string?> GetConnectionId();

}