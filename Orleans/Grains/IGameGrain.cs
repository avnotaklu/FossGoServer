using BadukServer;

public interface IGameGrain : IGrainWithStringKey
{
    Task CreateGame(int rows, int columns, TimeControl timeControl);
    Task<Game> AddPlayerToGame(String player, StoneType stone, string time);
    Task<Game> GetGame();
    Task<Dictionary<string, StoneType>> GetPlayers();
    Task<GameState> GetState();
    Task<List<MoveData>> GetMoves();
    Task<(bool moveSuccess, Game game)> MakeMove(MovePosition move, string playerId);
    Task<Game> ContinueGame(string playerId);
    Task<Game> AcceptScores(string playerId);
    Task<Game> ResignGame(string playerId);
    Task<Game> TimeoutCurrentPlayer();
    Task<Game> EditDeadStone(Position position, DeadStoneState state);
    Task<StoneType> GetStoneFromPlayerId(string id);
    Task<StoneType> GetOtherStoneFromPlayerId(string id);
    Task<string> GetPlayerIdFromStoneType(StoneType stone);
}

