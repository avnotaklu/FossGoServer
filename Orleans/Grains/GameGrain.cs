using System.Diagnostics;
using Orleans.Concurrency;

namespace BadukServer.Orleans.Grains;

public class GameGrain : Grain, IGameGrain
{
    private List<string> _playerIds;
    private int _currentMove;
    private GameState _gameState;
    private string? _winner;
    private string? _loser;

    private List<GameMove> _moves;
    private int _rows;
    private int _columns;
    private int _timeInSeconds;
    private Dictionary<string, int> _timeLeftForPlayers { get; set; }
    private int?[,] _board;
    private bool _initialized = false;


    public Task CreateGame(int rows, int columns, int timeInSeconds)
    {
        _playerIds = [];
        _timeLeftForPlayers = [];
        _rows = rows;
        _columns = columns;
        _timeInSeconds = timeInSeconds;
        _board = new int?[_rows, _columns];
        _gameState = GameState.WaitingForStart;
        _moves = [];
        _winner = null;
        _loser = null;
        _initialized = true;

        return Task.CompletedTask;
    }

    public Game InitializeGame()
    {
        return new Game(
            gameId: this.GetPrimaryKeyString(),
            rows: _rows,
            columns: _columns,
            timeInSeconds: _timeInSeconds,
            timeLeftForPlayers: _timeLeftForPlayers,
            moves: _moves,
            playgroundMap: new Dictionary<string, string>(),
            playerIds: _playerIds
        );
    }

    public Task<Game> AddPlayerToGame(string player)
    {
        Debug.Assert(_playerIds.Count < 3, $"Maximum of two players can be added, Added were {_playerIds.Count}");

        if (_playerIds.Contains(player))
        {
            // TODO: this condition should never happen, Check whether i can throw here?? 
            var premature_game = InitializeGame();
            return Task.FromResult(premature_game);
        }

        if (_playerIds.Count == 1)
        {
            Debug.Assert(_initialized, $"Game must be initialized before calling AddPlayerToGame() for second player");
        }

        _playerIds.Add(player);

        if (_playerIds.Count == 2)
        {
            _gameState = GameState.Started;
        }
        else
        {
            _gameState = GameState.WaitingForStart;
        }
        _timeLeftForPlayers[player] = _timeInSeconds;

        var game = InitializeGame();
        return Task.FromResult(game);
    }

    public Task<List<string>> GetPlayers()
    {
        return Task.FromResult(_playerIds);
    }


    public Task<List<GameMove>> GetMoves()
    {
        return Task.FromResult(_moves);
    }

    public Task<GameState> GetState()
    {
        return Task.FromResult(_gameState);
    }

    public Task<GameState> MakeMove(GameMove move)
    {
        _moves.Add(move);
        return Task.FromResult(_gameState);
    }

    public Task<Game> GetGame()
    {
        return Task.FromResult(InitializeGame());
    }
}