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
    private TimeControl _timeControl;
    private StoneSelectionType _stoneSelectionType;
    private List<PlayerTimeSnapshot> _playerTimeSnapshots { get; set; } = [];
    private Dictionary<Position, StoneType> _board = [];
    private List<int> _prisoners = [];
    private ReadOnlyCollection<int> _finalTerritoryScores = new ReadOnlyCollection<int>([]);
    private bool _initialized = false;
    private string? _startTime;
    private Position? _koPositionInLastMove;
    private int turn => _moves.Count;
    private HashSet<string> _scoresAcceptedBy = [];

    // Score Calculation Things
    private Dictionary<Position, DeadStoneState> _stoneStates = [];
    private GameOverMethod? _gameOverMethod;
    private readonly ILogger<GameGrain> _logger;
    private readonly IDateTimeService _dateTimeService;
    private BoardStateUtilities _boardStateUtilities;

    private string? _endTime;
    private string? _gameCreator;
    private float _komi = 6.5f;
    private string now => _dateTimeService.NowFormatted();

    public GameGrain(ILogger<GameGrain> logger, IDateTimeService dateTimeService)
    {
        _logger = logger;
        _dateTimeService = dateTimeService;
    }

    public Task CreateGame(int rows, int columns, TimeControl timeControl, StoneSelectionType stoneSelectionType, string gameCreator)
    {
        _players = [];
        _rows = rows;
        _columns = columns;
        _timeControl = timeControl;
        _stoneSelectionType = stoneSelectionType;
        _playerTimeSnapshots = [];
        _board = [];
        _gameState = GameState.WaitingForStart;
        _gameOverMethod = null;
        _moves = [];
        _prisoners = [];
        _finalTerritoryScores = new ReadOnlyCollection<int>([]);
        _winnerId = null;
        _initialized = true;
        _stoneStates = [];
        _scoresAcceptedBy = [];
        _boardStateUtilities = new BoardStateUtilities(_rows, _columns);
        _endTime = null;
        _gameCreator = gameCreator;

        return Task.CompletedTask;
    }

    private string gameId => this.GetPrimaryKeyString();

    private Game _GetGame()
    {
        return new Game(
            gameId: gameId,
            rows: _rows,
            columns: _columns,
            timeControl: _timeControl,
            stoneSelectionType: _stoneSelectionType,
            playerTimeSnapshots: _playerTimeSnapshots,
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
            komi: _komi,
            gameOverMethod: _gameOverMethod,
            endTime: _endTime,
            gameCreator: _gameCreator
        );
    }


    public Task<Game> JoinGame(string player, string time)
    {
        if (_players.Keys.Contains(player))
        {
            // TODO: this condition should never happen, Check whether i can throw here?? 
            var premature_game = _GetGame();
            return Task.FromResult(premature_game);
        }

        StartGame(time, [_gameCreator, player]);

        var game = _GetGame();
        return Task.FromResult(game);
    }

    private PlayerTimeSnapshot RecalculateTurnPlayerTimeSnapshots(List<MoveData> moves, List<PlayerTimeSnapshot> playerTimes, TimeControl timeControl)
    {
        var curTime = now;
        var turn = moves.Count;
        var curTurn = turn % 2;

        PlayerTimeSnapshot turnPlayerSnap() => playerTimes[curTurn];
        PlayerTimeSnapshot nonTurnPlayerSnap() => playerTimes[1 - curTurn];

        var byoYomiMS = timeControl.ByoYomiTime?.ByoYomiSeconds * 1000;

        var activePlayerIdx = playerTimes.FindIndex((snap) => snap.TimeActive);
        var activePlayerSnap = playerTimes[activePlayerIdx];

        var activePlayerTimeLeft = activePlayerSnap.MainTimeMilliseconds - (int)(DateTime.Parse(curTime) - DateTime.Parse(activePlayerSnap.SnapshotTimestamp)).TotalMilliseconds;

        var newByoYomi = activePlayerSnap.ByoYomisLeft - ((activePlayerSnap.ByoYomiActive && activePlayerTimeLeft <= 0) ? 1 : 0);

        var applicableByoYomiTime = (newByoYomi > 0) ? (byoYomiMS ?? 0) : 0;

        var applicableIncrement = (timeControl.IncrementSeconds ?? 0) * 1000;


        Console.WriteLine("Calculated Player Times");
        Debug.Write("Calculated Player Times");
        _logger.LogInformation("Calculated Player Times");

        playerTimes[activePlayerIdx] = new PlayerTimeSnapshot(
                    snapshotTimestamp: curTime,
                    mainTimeMilliseconds: activePlayerTimeLeft > 0 ? activePlayerTimeLeft + applicableIncrement : applicableByoYomiTime,
                    byoYomisLeft: newByoYomi,
                    byoYomiActive: activePlayerTimeLeft <= 0,
                    timeActive: playerTimes[activePlayerIdx].TimeActive
                );

        playerTimes[curTurn] = new PlayerTimeSnapshot(
                            snapshotTimestamp: curTime,
                            mainTimeMilliseconds: turnPlayerSnap().MainTimeMilliseconds,
                            byoYomisLeft: turnPlayerSnap().ByoYomisLeft,
                            byoYomiActive: turnPlayerSnap().ByoYomiActive,
                            timeActive: true
                        );

        playerTimes[1 - curTurn] = new PlayerTimeSnapshot(
                    snapshotTimestamp: curTime,
                    mainTimeMilliseconds: playerTimes[1 - curTurn].ByoYomiActive ? (byoYomiMS ?? 0) : nonTurnPlayerSnap().MainTimeMilliseconds,
                    byoYomisLeft: nonTurnPlayerSnap().ByoYomisLeft,
                    byoYomiActive: false,
                    timeActive: false
                );
        // var nonTurnPlayerSnap = playerTimes[1 - curTurn];

        // playerTimes[1 - curTurn] = new PlayerTimeSnapshot(
        //             snapshotTimestamp: curTime,
        //             mainTimeMilliseconds: nonTurnPlayerSnap.MainTimeMilliseconds,
        //              byoYomisLeft: nonTurnPlayerSnap.ByoYomisLeft,
        //             byoYomiActive: false,
        //             timeActive: false
        //         );

        return playerTimes[curTurn];
    }

    private void StartGame(string time, List<string> players)
    {
        _gameState = GameState.Playing;
        _startTime = time;
        _playerTimeSnapshots = [
                    new PlayerTimeSnapshot(
                snapshotTimestamp: time,
                mainTimeMilliseconds: _timeControl.MainTimeSeconds * 1000,
                byoYomisLeft: _timeControl.ByoYomiTime?.ByoYomis,
                byoYomiActive: false,
                timeActive: true
            ),
            new PlayerTimeSnapshot(
                snapshotTimestamp: time,
                mainTimeMilliseconds: _timeControl.MainTimeSeconds * 1000,
                byoYomisLeft: _timeControl.ByoYomiTime?.ByoYomis,
                byoYomiActive: false,
                timeActive: false
            )
                ];

        var stoneSelectionType = _stoneSelectionType;
        var firstPlayerToAssignStone = _gameCreator ?? players.First();

        foreach (var player in players)
        {
            var firstPlayerStone = stoneSelectionType == StoneSelectionType.Auto ? (StoneType)new Random().Next(2) : (StoneType)stoneSelectionType;
            var secondPlayerStone = 1 - firstPlayerStone;

            if (player == firstPlayerToAssignStone)
            {
                _players[player] = firstPlayerStone;
            }
            else
            {
                _players[player] = secondPlayerStone;
            }
        }
        _prisoners = [0, 0];

        var gameTimer = GrainFactory.GetGrain<IGameTimerGrain>(gameId);
        gameTimer.StartTurnTimer(_timeControl.MainTimeSeconds * 1000);
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

        if (!move.IsPass())
        {
            var x = (int)move.X!;
            var y = (int)move.Y!;

            var position = new Position(x, y);
            var utils = new BoardStateUtilities(_rows, _columns);
            var board = utils.BoardStateFromGame(_GetGame());

            var updateResult = new StoneLogic(board).HandleStoneUpdate(position, (int)player);
            _koPositionInLastMove = updateResult.board.koDelete;

            _prisoners[0] += updateResult.board.prisoners[0];
            _prisoners[1] += updateResult.board.prisoners[1];

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
            now
        );

        _logger.LogInformation("Move played <id>{gameId}<id>, <move>{move}<move>", gameId, lastMove);

        _moves.Add(lastMove);

        if (HasPassedTwice())
        {
            _logger.LogInformation("Game reached score calculation {gameId}", gameId);
            SetScoreCalculationState(lastMove);
        }
        else
        {
            var gameTimer = GrainFactory.GetGrain<IGameTimerGrain>(gameId);
            await gameTimer.StopTurnTimer();
            var turnPlayerTimeSnapshot = RecalculateTurnPlayerTimeSnapshots(_moves, _playerTimeSnapshots, _timeControl);
            await gameTimer.StartTurnTimer(turnPlayerTimeSnapshot.MainTimeMilliseconds);
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

    public async Task<List<TimeSpan>> CalculatePlayerTimesOfDiscreteSections(Game game)
    {
        var mainTimeSpan = TimeSpan.FromSeconds(game.TimeControl.MainTimeSeconds);
        // var mainTimeSpan =TimeSpan.FromSeconds(game.TimeControl.MainTimeSeconds);
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

        var player0Time = mainTimeSpan - firstPlayerDuration;


        var secondPlayerDuration = TimeSpan.Zero;
        var secondPlayerArrangedTimes = times.Skip(1).SkipLast(times.Count % 2);
        var secondPlayerTimesBeforeCorrespondingMoveMade = secondPlayerArrangedTimes.Where((time, index) => index % 2 == 0).ToList();
        var secondPlayerMoveMadeTimes = secondPlayerArrangedTimes.Where((time, index) => index % 2 == 1).ToList();

        for (int i = 0; i < MathF.Floor(secondPlayerArrangedTimes.Count() / 2); i++)
        {
            secondPlayerDuration += secondPlayerMoveMadeTimes[i] - secondPlayerTimesBeforeCorrespondingMoveMade[i];
        }
        var player1Time = mainTimeSpan - secondPlayerDuration;


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
            ), gameId, toMe: false);
        }
        else
        {
            await pushNotifierGrain.SendMessage(new SignalRMessage(
                type: SignalRMessageType.acceptedScores,
                data: null
            ), gameId, toMe: true);
        }

        var game = await GetGame();
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

    public async Task<PlayerTimeSnapshot?> TimeoutCurrentPlayer()
    {
        var playerWithTurn = GetPlayerIdWithTurn();
        var otherPlayerId = await GetPlayerIdFromStoneType(await GetOtherStoneFromPlayerId(playerWithTurn));

        var gameTimer = GrainFactory.GetGrain<IGameTimerGrain>(gameId);

        var curPlayerTime = RecalculateTurnPlayerTimeSnapshots(_moves, _playerTimeSnapshots, _timeControl);


        if (curPlayerTime.MainTimeMilliseconds <= 0)
        {
            EndGame(GameOverMethod.Timeout, otherPlayerId);

            var game = _GetGame();

            foreach (var playerId in _players.Keys)
            {
                var pushGrain = GrainFactory.GetGrain<IPushNotifierGrain>(playerId);

                await pushGrain.SendMessage(
                    new SignalRMessage(
                        SignalRMessageType.gameOver,
                        new GameOverMessage(game: game, method: GameOverMethod.Timeout)
                    ),
                    gameId,
                    toMe: true
                );
            }

            Console.WriteLine("Game timeout");
        }
        else
        {
            foreach (var playerId in _players.Keys)
            {
                var pushGrain = GrainFactory.GetGrain<IPushNotifierGrain>(playerId);

                await pushGrain.SendMessage(
                    new SignalRMessage(
                        SignalRMessageType.gameTimerUpdate,
                        new GameTimerUpdateMessage(
                            currentPlayerTime: curPlayerTime,
                            player: await GetStoneFromPlayerId(playerWithTurn)
                        )
                    ),
                    gameId,
                    toMe: true
                );
            }
        }
        return curPlayerTime;
    }

    private void EndGame(GameOverMethod method, string? winnerId = null)
    {
        _gameState = GameState.Ended;

        _playerTimeSnapshots = [
            new PlayerTimeSnapshot(
                snapshotTimestamp: _playerTimeSnapshots[0].SnapshotTimestamp,
                mainTimeMilliseconds: _playerTimeSnapshots[0].MainTimeMilliseconds,
                byoYomisLeft: _playerTimeSnapshots[0].ByoYomisLeft,
                byoYomiActive: _playerTimeSnapshots[0].ByoYomiActive,
                timeActive: false
            ),
            new PlayerTimeSnapshot(
                snapshotTimestamp: _playerTimeSnapshots[1].SnapshotTimestamp,
                mainTimeMilliseconds: _playerTimeSnapshots[1].MainTimeMilliseconds,
                byoYomisLeft: _playerTimeSnapshots[1].ByoYomisLeft,
                byoYomiActive: _playerTimeSnapshots[1].ByoYomiActive,
                timeActive: false
            )
        ];

        var gameTimerGrain = GrainFactory.GetGrain<IGameTimerGrain>(gameId);
        gameTimerGrain.StopTurnTimer();

        if (winnerId != null)
        {
            _winnerId = winnerId;
        }

        if (method == GameOverMethod.Score)
        {
            var scoreCalculator = _GetScoreCalculator();
            _winnerId = scoreCalculator.GetWinner() == 0 ? _GetPlayerIdFromStoneType(StoneType.Black) : _GetPlayerIdFromStoneType(StoneType.White);
            _finalTerritoryScores = scoreCalculator.TerritoryScores;
        }

        _endTime = now;
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
            // _GetGame(),
            rows: _rows,
            cols: _columns,
            komi: _komi,
            prisoners: _prisoners,
            deadStones: _stoneStates.Where((p) => p.Value == DeadStoneState.Dead).Select(a => a.Key).ToList(),
            playground
        );
    }

    public Task<Game> GetGame()
    {
        var game = _GetGame();
        return Task.FromResult(game);
    }
}