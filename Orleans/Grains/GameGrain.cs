
public class GameGrain : Grain, IGameGrain
{

    public List<Guid> playerIds = [];
    private int currentMove;
    private GameState gameState;
    private Guid winner;
    private Guid loser;

    private List<GameMove> _moves = new();
    private int[,] _board = null!;
    private string _name = null!;

    public Task<GameState> AddPlayerToGame(Guid player)
    {
        throw new NotImplementedException();
    }

    public Task<List<GameMove>> GetMoves()
    {
        throw new NotImplementedException();
    }

    public Task<GameState> GetState()
    {
        throw new NotImplementedException();
    }

    public Task<GameState> MakeMove(GameMove move)
    {
        throw new NotImplementedException();
    }
}