using BadukServer;

public interface IGameGrain : IGrainWithStringKey
{
    Task CreateGame(int rows, int columns, TimeControlData timeControl, StoneSelectionType stoneSelectionType, string gameCreator);
    Task<(Game, PublicUserInfo)> JoinGame(String player, string time);
    Task<Game> GetGame();
    Task<Dictionary<string, StoneType>> GetPlayers();
    Task<GameState> GetState();
    Task<List<MoveData>> GetMoves();
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

    Task<Game> StartMatch(Match match, string matchedPlayerId);
}