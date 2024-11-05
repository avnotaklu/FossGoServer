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
    private Dictionary<Position, StoneType> _board = [];
    private Dictionary<string, int> _prisoners = [];
    private bool _initialized = false;
    private string? _startTime;
    private Position? _koPositionInLastMove;
    private int turn => _moves.Count;

    // Score Calculation Things
    private Dictionary<Position, DeadStoneState> _stoneStates = [];


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
        _prisoners = [];
        _winner = null;
        _loser = null;
        _initialized = true;
        _stoneStates = [];

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
            playgroundMap: _board.ToDictionary(e => e.Key.ToHighLevelRepr(), e => e.Value),
            players: _players,
            prisoners: _prisoners,
            startTime: _startTime,
            gameState: _gameState,
            koPositionInLastMove: _koPositionInLastMove?.ToHighLevelRepr(),
            deadStones: _stoneStates.Where((p) => p.Value == DeadStoneState.Dead).Select((k) => k.Key.ToHighLevelRepr()).ToList()
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
        _prisoners[player] = 0;

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
            _koPositionInLastMove = updateResult.board.koDelete;

            _prisoners[await GetPlayerIdFromStoneType(0)] += updateResult.board.prisoners[0];
            _prisoners[await GetPlayerIdFromStoneType((StoneType)1)] += updateResult.board.prisoners[1];

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

    public async Task<Game> ContinueGame(string playerId)
    {
        _gameState = GameState.Playing;

        var game = await GetGame();
        var pushNotifierGrain = GrainFactory.GetGrain<IPushNotifierGrain>(await GetPlayerIdFromStoneType(await GetOtherStoneFromPlayerId(playerId)));

        await pushNotifierGrain.SendMessage(new SignalRMessage(
            type: SignalRMessageType.continueGame,
            data: new ContinueGameMessage(game)
        ), gameId, toMe: true);

        return game;
    }

    public Task<Game> EditDeadStone(Position position, DeadStoneState state)
    {
        if (_stoneStates.ContainsKey(position) && _stoneStates[position] == state)
        {
            return GetGame();
        }
        else
        {
            var boardState = new BoardStateUtilities(_rows, _columns).BoardStateFromGame(_GetGame());
            var cluster = boardState.playgroundMap[position].cluster;
            foreach (var pos in cluster.data)
            {
                _stoneStates[pos] = state;
            }

            return GetGame();
        }
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


    public Task<StoneType> GetStoneFromPlayerId(string id)
    {
        return Task.FromResult(_players[id]);
    }

    public async Task<StoneType> GetOtherStoneFromPlayerId(string id)
    {
        return await Task.FromResult(1 - await GetStoneFromPlayerId(id));
    }

    public Task<string> GetPlayerIdFromStoneType(StoneType stone)
    {
        Debug.Assert(_players.Count == 2);
        foreach (var item in _players)
        {
            if (item.Value == stone)
            {
                return Task.FromResult(item.Key);
            }
        }
        throw new UnreachableException($"Player: {stone} has not yet joined the game");
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