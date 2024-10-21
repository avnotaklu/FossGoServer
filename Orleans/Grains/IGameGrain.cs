using BadukServer;

public interface IGameGrain : IGrainWithStringKey {
    Task CreateGame(int rows, int columns, int timeInSeconds);
    Task<Game> AddPlayerToGame(String player);
    Task<Game> GetGame();
    Task<List<string>> GetPlayers();
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
