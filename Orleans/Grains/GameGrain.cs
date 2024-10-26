using System.Diagnostics;
using Orleans.Concurrency;

namespace BadukServer.Orleans.Grains;

public class GameGrain : Grain, IGameGrain
{
    // Player id with player's stone; 0 for black, 1 for white
    private Dictionary<string, Stone> _players = [];
    private int _currentMove;
    private GameState _gameState;
    private string? _winner;
    private string? _loser;

    private List<GameMove> _moves = [];
    private int _rows;
    private int _columns;
    private int _timeInSeconds;
    private Dictionary<string, int> _timeLeftForPlayers { get; set; } = [];
    private Dictionary<string, string> _board = [];
    private Dictionary<string, int> _playerScores = [];
    private bool _initialized = false;


    public Task CreateGame(int rows, int columns, int timeInSeconds)
    {
        _players = [];
        _timeLeftForPlayers = [];
        _rows = rows;
        _columns = columns;
        _timeInSeconds = timeInSeconds;
        _board = [];
        _gameState = GameState.WaitingForStart;
        _moves = [];
        _playerScores = [];
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
            playgroundMap: _board,
            players: _players,
            playerScores: _playerScores
        );
    }

    public Task<Game> AddPlayerToGame(string player, Stone stone)
    {
        Debug.Assert(_players.Keys.Count < 3, $"Maximum of two players can be added, Added were {_players.Keys.Count}");

        if (_players.Keys.Contains(player))
        {
            // TODO: this condition should never happen, Check whether i can throw here?? 
            var premature_game = InitializeGame();
            return Task.FromResult(premature_game);
        }

        if (_players.Keys.Count == 1)
        {
            Debug.Assert(_initialized, $"Game must be initialized before calling AddPlayerToGame() for second player");
        }

        _players[player] = stone;

        if (_players.Keys.Count == 2)
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

    public Task<Dictionary<string, Stone>> GetPlayers()
    {
        return Task.FromResult(_players);
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