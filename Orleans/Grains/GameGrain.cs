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

    private List<MoveData> _moves = [];
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


    private readonly ILogger<GameGrain> _logger;

    public GameGrain(ILogger<GameGrain> logger)

    {
        _logger = logger;
    }



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

    private string gameId => this.GetPrimaryKeyString();

    private Game _GetGame()
    {
        return new Game(
            gameId: gameId,
            rows: _rows,
            columns: _columns,
            timeInSeconds: _timeInSeconds,
            timeLeftForPlayers: _timeLeftForPlayers,
            moves: _moves,
            playgroundMap: _board,
            players: _players,
            playerScores: _playerScores,
            startTime: _startTime,
            gameState: _gameState,
            koPositionInLastMove: koPositionInLastMove
        );
    }

    public Task<Game> AddPlayerToGame(string player, StoneType stone, string time)
    {
        Debug.Assert(_players.Keys.Count < 3, $"Maximum of two players can be added, Added were {_players.Keys.Count}");

        if (_players.Keys.Contains(player))
        {
            // TODO: this condition should never happen, Check whether i can throw here?? 
            var premature_game = _GetGame();
            return Task.FromResult(premature_game);
        }

        if (_players.Keys.Count == 1)
        {
            Debug.Assert(_initialized, $"Game must be initialized before calling AddPlayerToGame() for second player");
        }

        _players[player] = stone;

        if (_players.Keys.Count == 2)
        {
            _gameState = GameState.Playing;
            _startTime = time;
        }
        else
        {
            _gameState = GameState.WaitingForStart;
        }
        _timeLeftForPlayers[player] = _timeInSeconds;

        var game = _GetGame();
        return Task.FromResult(game);
    }

    public Task<Dictionary<string, StoneType>> GetPlayers()
    {
        return Task.FromResult(_players);
    }


    public Task<List<MoveData>> GetMoves()
    {
        return Task.FromResult(_moves);
    }

    public Task<GameState> GetState()
    {
        return Task.FromResult(_gameState);
    }

    public async Task<(bool moveSuccess, Game game)> MakeMove(MovePosition move, string playerId)
    {
        Debug.Assert(_players.ContainsKey(playerId));
        var player = _players[playerId];
        Debug.Assert((turn % 2) == (int)player);

        // if (move.X == null || move.Y == null)
        // {
        //     // Move was passed
        //     if(_moves.Last().IsPass()) {
        //         // Two passes
        //         _gameState = GameState.ScoreCalculation;
        //     }
        //     return await GetGame();
        // }
        if (!move.IsPass())
        {
            var x = (int)move.X!;
            var y = (int)move.Y!;

            var position = new Position(x, y);
            var utils = new BoardStateUtilities(_rows, _columns);
            var board = utils.BoardStateFromGame(_GetGame());

            var updateResult = new StoneLogic(board).HandleStoneUpdate(position, (int)player);
            koPositionInLastMove = updateResult.board.koDelete?.ToHighLevelRepr();

            if (updateResult.result)
            {
                var map = utils.MakeHighLevelBoardRepresentationFromBoardState(updateResult.board);
                _board = map;
            }
            else
            {
                return (false, await GetGame());
            }
        }

        var lastMove = new MoveData(
            move.X,
            move.Y,
            DateTime.Now.ToString("o")
        );

        _logger.LogInformation("Move played <id>{gameId}<id>, <move>{move}<move>", gameId, lastMove);

        _moves.Add(lastMove);

        if (HasPassedTwice())
        {
            _logger.LogInformation("Game reached score calculation {gameId}", gameId);
            SetScoreCalculationState(lastMove);
        }

        var game = await GetGame();

        {
            // Send message to player with current turn
            var currentTurnPlayer = GetPlayerIdWithTurn();
            var pushNotifier = GrainFactory.GetGrain<IPushNotifierGrain>(currentTurnPlayer);
            await pushNotifier.SendMessage(
                new SignalRMessage(
                    type: SignalRMessageType.newMove,
                    data: new NewMoveMessage(game: game)
                ), game.GameId, toMe: true
            );

        }

        return (true, game);
    }

    private void SetScoreCalculationState(MoveData lastMove)
    {
        Debug.Assert(HasPassedTwice());
        Debug.Assert(lastMove.IsPass());

        var playerWithTurn = GetPlayerIdWithTurn();

        _gameState = GameState.ScoreCalculation;
        var pushNotifier = GrainFactory.GetGrain<IPushNotifierGrain>(playerWithTurn);
        pushNotifier.SendMessage(new SignalRMessage(
            SignalRMessageType.scoreCaculationStarted,
            null
        ), gameId);
    }

    private string GetPlayerIdWithTurn()
    {
        Debug.Assert(_players.Count == 2);
        foreach (var item in _players)
        {
            if ((int)item.Value == (turn % 2))
            {
                return item.Key;
            }
        }
        throw new UnreachableException("This path shouldn't be reachable, as there always exists one user with supposed next turn");
    }

    private bool HasPassedTwice()
    {
        MoveData? prev = null;
        bool hasPassedTwice = false;
        var reversedMoves = _moves.AsEnumerable().Reverse();

        foreach (var i in reversedMoves)
        {
            if (prev == null)
            {
                prev = i;
                continue;
            }

            if (i.IsPass() && prev.IsPass())
            {
                hasPassedTwice = !hasPassedTwice;
            }
            else
            {
                break;
            }
        }
        return hasPassedTwice;
    }

    public Task<Game> GetGame()
    {
        return Task.FromResult(_GetGame());
    }
}