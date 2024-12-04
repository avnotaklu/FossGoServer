using BadukServer;
using BadukServer.Orleans.Grains;

public interface IGameGrain : IGrainWithStringKey
{
    Task CreateGame(GameCreationData creationData, PlayerInfo? gameCreator, GameType gameType);
    Task<(Game game, PlayerInfo? otherPlayerInfo, bool justJoined)> JoinGame(PlayerInfo player, string time);
    Task<Game> GetGame();
    // Task<Dictionary<string, StoneType>> GetPlayers();
    // Task<GameState> GetState();
    // Task<List<GameMove>> GetMoves();
    Task<(bool moveSuccess, Game game)> MakeMove(MovePosition move, string playerId);
    Task<Game> ContinueGame(string playerId);
    Task<Game> AcceptScores(string playerId);
    Task<Game> ResignGame(string playerId);
    Task<PlayerTimeSnapshot> TimeoutCurrentPlayer();
    Task<Game> EditDeadStone(RawPosition position, DeadStoneState state, string editorPlayer);

    /// <summary>
    /// Set grain state to a supplied game
    /// </summary>
    /// <param name="playerId"></param>
    /// <returns></returns>
    Task<Game> ResetGame(Game game);

    Task<Game> StartMatch(Match match, List<PlayerInfo> playerInfos);
}