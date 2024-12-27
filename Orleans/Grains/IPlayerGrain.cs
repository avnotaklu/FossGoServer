using BadukServer;
using BadukServer.Orleans.Grains;

public interface IPlayerGrain : IGrainWithStringKey
{
    Task<HashSet<string>> GetActiveGames();
    Task ConnectPlayer(string connectionId, PlayerType playerType);
    Task<string> CreateGame(GameCreationDto creationData, DateTime time);
    Task<(Game game, DateTime? joinTime, PlayerInfo? otherPlayerData)> JoinGame(string gameId);
    Task InformMyJoin(Game game, List<PlayerInfo> players, DateTime time, PlayerJoinMethod joinMethod);
    Task LeaveGame(string gameId);
    Task AddActiveGame(string gameId);
    public Task<string?> GetConnectionId();
}