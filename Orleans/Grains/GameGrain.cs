using System.Diagnostics;
using Orleans.Concurrency;

namespace BadukServer.Orleans.Grains;

public class GameGrain : Grain, IGameGrain
{
    // Player id with player's stone; 0 for black, 1 for white
    private Dictionary<string, StoneType> _players = [];
    private int _currentMove;
    private GameState _gameState;
    private string? _winner;
    private string? _loser;

    private List<MovePosition?> _moves = [];
    private int _rows;
    private int _columns;
    private int _timeInSeconds;
    private Dictionary<string, int> _timeLeftForPlayers { get; set; } = [];
    private Dictionary<string, StoneType> _board = [];
    private Dictionary<string, int> _playerScores = [];
    private bool _initialized = false;
    private string? _startTime;
    private string? koPositionInLastMove;
    private int turn => _moves.Count;


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
            playerScores: _playerScores,
            startTime: _startTime,
            koPositionInLastMove: koPositionInLastMove
        );
    }

    public Task<Game> AddPlayerToGame(string player, StoneType stone, string time)
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
            _startTime = time;
        }
        else
        {
            _gameState = GameState.WaitingForStart;
        }
        _timeLeftForPlayers[player] = _timeInSeconds;

        var game = InitializeGame();
        return Task.FromResult(game);
    }

    public Task<Dictionary<string, StoneType>> GetPlayers()
    {
        return Task.FromResult(_players);
    }


    public Task<List<MovePosition?>> GetMoves()
    {
        return Task.FromResult(_moves);
    }

    public Task<GameState> GetState()
    {
        return Task.FromResult(_gameState);
    }

    public async Task<Game> MakeMove(MovePosition? move, string playerId)
    {
        Debug.Assert(_players.ContainsKey(playerId));
        var player = _players[playerId];
        Debug.Assert((turn % 2) == (int)player);

        _moves.Add(move);
        if (move == null)
        {
            // Move was passed
            return await GetGame();
        }
        var movePos = move.GetValueOrDefault();
        var position = new Position(movePos.X, movePos.Y);
        var utils = new BoardStateUtilities(_rows, _columns);
        var board = utils.BoardStateFromHighLevelBoardRepresentation(_board);
        var updateResult = new StoneLogic(board).HandleStoneUpdate(position, (int)player);
        var map = utils.MakeHighLevelBoardRepresentationFromBoardState(updateResult.board);
        _board = map;
        return await GetGame();
    }

    public Task<Game> GetGame()
    {
        return Task.FromResult(InitializeGame());
    }
}