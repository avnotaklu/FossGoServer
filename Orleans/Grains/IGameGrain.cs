public interface IGameGrain : IGrainWithGuidKey {
    Task<GameState> AddPlayerToGame(Guid player);
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

[GenerateSerializer]
public struct GameMove
{
    [Id(0)] public Guid PlayerId { get; set; }
    [Id(1)] public int X { get; set; }
    [Id(2)] public int Y { get; set; }
}
