using BadukServer;

public interface IGameGrain : IGrainWithStringKey {
    Task CreateGame(int rows, int columns, int timeInSeconds);
    Task<Game> AddPlayerToGame(String player, StoneType stone, string time);
    Task<Game> GetGame();
    Task<Dictionary<string, StoneType>> GetPlayers();
    Task<GameState> GetState();
    Task<List<GameMove>> GetMoves();
    Task<GameState> MakeMove(GameMove move);
    // Task<GameSummary> GetSummary(Guid player);
}


[Serializable]
public enum GameState {
    WaitingForStart,
    Started,
    Ended
}
