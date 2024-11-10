using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using MongoDB.Bson.Serialization.Serializers;
using Orleans.Concurrency;

namespace BadukServer.Orleans.Grains;

public class GameGrain : Grain, IGameGrain
{
    // Player id with player's stone; 0 for black, 1 for white
    private Dictionary<string, StoneType> _players = [];
    private GameState _gameState;
    private string? _winnerId;
    private List<MoveData> _moves = [];
    private int _rows;
    private int _columns;
    private int _timeInSeconds;
    private Dictionary<string, int> _timeLeftForPlayers { get; set; } = [];
    private Dictionary<Position, StoneType> _board = [];
    private Dictionary<string, int> _prisoners = [];
    private ReadOnlyCollection<int> _finalTerritoryScores = new ReadOnlyCollection<int>([]);
    private bool _initialized = false;
    private string? _startTime;
    private Position? _koPositionInLastMove;
    private int turn => _moves.Count;
    private HashSet<string> _scoresAcceptedBy = [];

    // Score Calculation Things
    private Dictionary<Position, DeadStoneState> _stoneStates = [];
    private GameOverMethod _gameOverMethod;
    private readonly ILogger<GameGrain> _logger;
    private BoardStateUtilities _boardStateUtilities;

    private string? _endTime;

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
        _finalTerritoryScores = new ReadOnlyCollection<int>([]);
        _winnerId = null;
        _initialized = true;
        _stoneStates = [];
        _scoresAcceptedBy = [];
        _boardStateUtilities = new BoardStateUtilities(_rows, _columns);
        _endTime = null;

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
            deadStones: _stoneStates.Where((p) => p.Value == DeadStoneState.Dead).Select((k) => k.Key.ToHighLevelRepr()).ToList(),
            winnerId: _winnerId,
            finalTerritoryScores: _finalTerritoryScores.ToList(),
            komi: 6.5f,
            gameOverMethod: _gameOverMethod,
            endTime: _endTime
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
            StartGame(time);
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

    private void StartGame(string time)
    {
        _gameState = GameState.Playing;
        _startTime = time;
        var gameTimer = GrainFactory.GetGrain<IGameTimerGrain>(gameId);
        gameTimer.StartTurnTimer(_timeInSeconds * 1000);
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
        Debug.Assert(_gameState == GameState.Playing);

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

        var gameTimer = GrainFactory.GetGrain<IGameTimerGrain>(gameId);
        await gameTimer.StopTurnTimer();
        var totalTimes = await CalculatePlayerTimesOfDiscreteSections(game);
        var curPlayerRemainingTime = (int)totalTimes[(int)await GetStoneFromPlayerId(GetPlayerIdWithTurn())].TotalMilliseconds;

        await gameTimer.StartTurnTimer(curPlayerRemainingTime);

        return (true, game);
    }

    public async Task<List<TimeSpan>> CalculatePlayerTimesOfDiscreteSections(Game game)
    {
        var raw_times = new List<string> { game.StartTime! };
        raw_times.AddRange(game.Moves.Select(move => move.Time));

        var times = raw_times.Select(time => DateTime.Parse(time)).ToList();

        // Calculate first player's duration
        // var firstPlayerDuration = times
        //     .Select((time, index) => (time, index))
        //     .Where(pair => pair.index % 2 == 1)
        //     .Aggregate(TimeSpan.Zero, (duration, pair) => duration + (pair.time - times[pair.index - 1]));

        var firstPlayerDuration = TimeSpan.Zero;

        var firstPlayerArrangedTimes = times.SkipLast(times.Count % 2);
        var firstPlayerTimesBeforeCorrespondingMoveMade = firstPlayerArrangedTimes.Where((time, index) => index % 2 == 0).ToList();
        var firstPlayerMoveMadeTimes = firstPlayerArrangedTimes.Where((time, index) => index % 2 == 1).ToList();

        for (int i = 0; i < MathF.Floor(firstPlayerArrangedTimes.Count() / 2); i++)
        {
            firstPlayerDuration += firstPlayerMoveMadeTimes[i] - firstPlayerTimesBeforeCorrespondingMoveMade[i];
        }





        var player0Time = TimeSpan.FromSeconds(game.TimeInSeconds) - firstPlayerDuration;

        // Calculate second player's duration
        // var secondPlayerDuration = times
        //     .Select((time, index) => (time, index))
        //     .Skip(1)
        //     .Where(pair => pair.index % 2 == 1)
        //     .Aggregate(TimeSpan.Zero, (duration, pair) => duration + (pair.time - times[pair.index - 1]));



        var secondPlayerDuration = TimeSpan.Zero;

        var secondPlayerArrangedTimes = times.Skip(1).SkipLast(times.Count % 2);
        var secondPlayerTimesBeforeCorrespondingMoveMade = secondPlayerArrangedTimes.Where((time, index) => index % 2 == 0).ToList();
        var secondPlayerMoveMadeTimes = secondPlayerArrangedTimes.Where((time, index) => index % 2 == 1).ToList();

        for (int i = 0; i < MathF.Floor(secondPlayerArrangedTimes.Count() / 2); i++)
        {
            secondPlayerDuration += secondPlayerMoveMadeTimes[i] - secondPlayerTimesBeforeCorrespondingMoveMade[i];
        }


        var player1Time = TimeSpan.FromSeconds(game.TimeInSeconds) - secondPlayerDuration;

        var playerTimes = new List<TimeSpan> { player0Time, player1Time };

        // If the game has ended, apply the end time to the player with the turn
        if (game.GameState == GameState.Ended)
        {
            playerTimes[(int)await GetStoneFromPlayerId(GetPlayerIdWithTurn())] -= DateTime.Parse(game.EndTime!) - times.Last();
        }

        return playerTimes;
    }
    public async Task<Game> ContinueGame(string playerId)
    {
        Debug.Assert(_gameState == GameState.ScoreCalculation);
        _gameState = GameState.Playing;
        _stoneStates.Clear();
        _scoresAcceptedBy.Clear();

        var game = await GetGame();
        var pushNotifierGrain = GrainFactory.GetGrain<IPushNotifierGrain>(await GetPlayerIdFromStoneType(await GetOtherStoneFromPlayerId(playerId)));

        await pushNotifierGrain.SendMessage(new SignalRMessage(
            type: SignalRMessageType.continueGame,
            data: new ContinueGameMessage(game)
        ), gameId, toMe: true);

        return game;
    }

    public async Task<Game> AcceptScores(string playerId)
    {
        Debug.Assert(_gameState == GameState.ScoreCalculation);
        _scoresAcceptedBy.Add(playerId);

        var game = await GetGame();

        var pushNotifierGrain = GrainFactory.GetGrain<IPushNotifierGrain>(await GetPlayerIdFromStoneType(await GetOtherStoneFromPlayerId(playerId)));

        if (_scoresAcceptedBy.Count == 2)
        {
            // Game is over
            EndGame(GameOverMethod.Score);

            await pushNotifierGrain.SendMessage(new SignalRMessage(
                type: SignalRMessageType.gameOver,
                data: new GameOverMessage(
                    GameOverMethod.Score,
                    _GetGame()
                )
            ), gameId, toMe: true);
        }
        else
        {
            await pushNotifierGrain.SendMessage(new SignalRMessage(
                type: SignalRMessageType.acceptedScores,
                data: null
            ), gameId, toMe: true);
        }

        return game;
    }

    public async Task<Game> ResignGame(string playerId)
    {
        var otherPlayerId = await GetPlayerIdFromStoneType(await GetOtherStoneFromPlayerId(playerId));

        EndGame(GameOverMethod.Resign, otherPlayerId);

        var game = await GetGame();
        var pushNotifierGrain = GrainFactory.GetGrain<IPushNotifierGrain>(otherPlayerId);

        await pushNotifierGrain.SendMessage(new SignalRMessage(
            type: SignalRMessageType.gameOver,
            data: new GameOverMessage(
                GameOverMethod.Resign,
                game
            )
        ), gameId, toMe: true);

        return game;
    }

    public async Task<Game> TimeoutCurrentPlayer()
    {
        var playerWithTurn = GetPlayerIdWithTurn();
        var otherPlayerId = await GetPlayerIdFromStoneType(await GetOtherStoneFromPlayerId(playerWithTurn));

        EndGame(GameOverMethod.Timeout, otherPlayerId);

        return _GetGame();
    }

    private void EndGame(GameOverMethod method, string? winnerId = null)
    {
        _gameState = GameState.Ended;

        if (winnerId != null)
        {
            _winnerId = winnerId;
        }

        if (method == GameOverMethod.Score)
        {
            var scoreCalculator = _GetScoreCalculator();
            _winnerId = scoreCalculator.GetWinner();
            _finalTerritoryScores = scoreCalculator.TerritoryScores;
        }

        _endTime = DateTime.Now.ToString("o");
        _gameOverMethod = method;
    }

    public Task<Game> EditDeadStone(Position position, DeadStoneState state)
    {
        Debug.Assert(_gameState == GameState.ScoreCalculation);
        _scoresAcceptedBy.Clear();
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


    public string _GetPlayerIdFromStoneType(StoneType stone)
    {
        Debug.Assert(_players.Count == 2);
        foreach (var item in _players)
        {
            if (item.Value == stone)
            {
                return item.Key;
            }
        }
        throw new UnreachableException($"Player: {stone} has not yet joined the game");
    }

    public Task<string> GetPlayerIdFromStoneType(StoneType stone)
    {
        return Task.FromResult(_GetPlayerIdFromStoneType(stone));
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

    private ScoreCalculation _GetScoreCalculator()
    {
        var playground = _boardStateUtilities.BoardStateFromGame(_GetGame()).playgroundMap;
        return new ScoreCalculation(
            _GetGame(),
            _GetPlayerIdFromStoneType(StoneType.Black),
            _GetPlayerIdFromStoneType(StoneType.White),
            playground
        );
    }

    public Task<Game> GetGame()
    {
        return Task.FromResult(_GetGame());
    }
}