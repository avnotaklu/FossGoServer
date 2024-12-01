using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using BadukServer.Services;
using Microsoft.CodeAnalysis;
using MongoDB.Bson.Serialization.Serializers;
using Moq;
using Orleans.Concurrency;

namespace BadukServer.Orleans.Grains;

public class GameGrain : Grain, IGameGrain
{
    // Player id with player's stone; 0 for black, 1 for white
    private Dictionary<string, StoneType> _players = [];
    private GameState _gameState;
    private string? _winnerId;
    private List<GameMove> _moves = [];
    private int _rows;
    private int _columns;
    private TimeControl _timeControl = null!;
    private StoneSelectionType _stoneSelectionType;
    private List<PlayerTimeSnapshot> _playerTimeSnapshots { get; set; } = [];
    private Dictionary<Position, StoneType> _board = [];
    private List<int> _prisoners = [];
    private ReadOnlyCollection<int> _finalTerritoryScores = new ReadOnlyCollection<int>([]);
    private bool _initialized = false;
    private DateTime? _startTime;
    private Position? _koPositionInLastMove;
    private int turn => _moves.Count;
    private StoneType playerTurn => (StoneType)(turn % 2);
    private HashSet<string> _scoresAcceptedBy = [];

    // Score Calculation Things
    private Dictionary<Position, DeadStoneState> _stoneStates = [];
    private GameOverMethod? _gameOverMethod;

    private List<int>? _playersRatings = [];
    private List<int>? _playersRatingsDiff = [];

    private DateTime? _endTime;
    private string? _gameCreator;
    private float _komi = 6.5f;
    private string now => _dateTimeService.NowFormatted();


    // Injected
    private readonly ILogger<GameGrain> _logger;
    private readonly IDateTimeService _dateTimeService;
    private readonly IUserRatingService _userRatingService;
    private readonly IUsersService _userService;
    private readonly IGameService _gameService;
    private readonly IPublicUserInfoService _publicUserInfo;
    private readonly ISignalRHubService _hubService;
    private readonly ITimeCalculator _timeCalculator;


    private BoardStateUtilities _boardStateUtilities;
    private IRatingEngine _ratingEngine;

    public GameGrain(ILogger<GameGrain> logger, IDateTimeService dateTimeService, IUsersService usersService, IUserRatingService userRatingService, IGameService gameService, ISignalRHubService hubService, IRatingEngine ratingEngine, IPublicUserInfoService publicUserInfo, ITimeCalculator timeCalculator)
    {
        _logger = logger;
        _dateTimeService = dateTimeService;
        _userRatingService = userRatingService;
        _gameService = gameService;
        _boardStateUtilities = new BoardStateUtilities();
        _ratingEngine = ratingEngine;
        _hubService = hubService;
        _userService = usersService;
        _publicUserInfo = publicUserInfo;
        _timeCalculator = timeCalculator;
    }

    public void SetState(
        int rows,
        int columns,
        TimeControl timeControl,
        List<PlayerTimeSnapshot> playerTimeSnapshots,
        List<GameMove> moves,
        Dictionary<string, StoneType> playgroundMap,
        Dictionary<string, StoneType> players,
        List<int> prisoners,
        string? startTime,
        GameState gameState,
        string? koPositionInLastMove,
        List<string> deadStones,
        string? winnerId,
        List<int> finalTerritoryScores,
        float komi,
        GameOverMethod? gameOverMethod,
        string? endTime,
        StoneSelectionType stoneSelectionType,
        string? gameCreator,
        List<int>? playersRatings,
        List<int>? playersRatingsDiff
    )
    {
        _rows = rows;
        _columns = columns;
        _timeControl = timeControl;
        _playerTimeSnapshots = playerTimeSnapshots;
        _moves = moves;
        _board = playgroundMap.ToDictionary(a => new Position(a.Key), a => a.Value);
        _players = players;
        _prisoners = prisoners;
        _startTime = startTime?.DeserializedDate();
        _gameState = gameState;
        _koPositionInLastMove = koPositionInLastMove == null ? null : new Position(koPositionInLastMove);
        _stoneStates = deadStones.Select(a => new Position(a)).ToDictionary(a => a, a => DeadStoneState.Dead);
        _winnerId = winnerId;
        _finalTerritoryScores = new ReadOnlyCollection<int>(finalTerritoryScores);
        _komi = komi;
        _gameOverMethod = gameOverMethod;
        _endTime = endTime?.DeserializedDate();
        _stoneSelectionType = stoneSelectionType;
        _gameCreator = gameCreator;
        _playersRatings = playersRatings;
        _playersRatingsDiff = playersRatingsDiff;
        _initialized = true;
    }

    // Grain overrides

    public Task CreateGame(int rows, int columns, TimeControlDto timeControl, StoneSelectionType stoneSelectionType, string? gameCreator)
    {
        SetState(
            rows: rows,
            columns: columns,
            timeControl: new TimeControl(timeControl),
            playerTimeSnapshots: [],
            moves: [],
            playgroundMap: [],
            players: [],
            prisoners: [0, 0],
            startTime: null,
            gameState: GameState.WaitingForStart,
            koPositionInLastMove: null,
            deadStones: [],
            winnerId: null,
            finalTerritoryScores: [],
            komi: 6.5f,
            gameOverMethod: null,
            endTime: null,
            stoneSelectionType: stoneSelectionType,
            gameCreator: gameCreator,
            playersRatings: [],
            playersRatingsDiff: []
        );

        return Task.CompletedTask;
    }


    public async Task<(Game game, PublicUserInfo otherPlayerInfo)> JoinGame(string player, string time)
    {

        if (_players.Keys.Contains(player))
        {
            var otherPlayerDataAlreadyStart = (await _userService.GetByIds([GetOtherPlayerIdFromPlayerId(player)])).First();
            var oldGame = _GetGame();
            return (oldGame, await _publicUserInfo.GetPublicUserInfo(otherPlayerDataAlreadyStart.GetUserId()));
        }

        await StartGame(time, [_gameCreator, player]);

        var otherPlayerData = (await _userService.GetByIds([GetOtherPlayerIdFromPlayerId(player)])).First();

        var game = _GetGame();

        var otherPlayerInfo = await _publicUserInfo.GetPublicUserInfo(otherPlayerData.GetUserId());

        await JoinPlayersToPushGroup();
        await SendJoinMessage(otherPlayerInfo.Id, time.DeserializedDate(), otherPlayerInfo);

        return (game, otherPlayerInfo);
    }

    public Task<Dictionary<string, StoneType>> GetPlayers()
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
            var board = _boardStateUtilities.BoardStateFromGame(_GetGame());

            var updateResult = new StoneLogic(board).HandleStoneUpdate(position, (int)player);
            _koPositionInLastMove = updateResult.board.koDelete;

            _prisoners[0] += updateResult.board.prisoners[0];
            _prisoners[1] += updateResult.board.prisoners[1];

            if (updateResult.result)
            {
                var map = _boardStateUtilities.MakeHighLevelBoardRepresentationFromBoardState(updateResult.board);
                _board = map;
            }
            else
            {
                return (false, _GetGame());
            }
        }

        var lastMove = new GameMove(
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

            SetRecalculatedTurnPlayerTimeSnapshots(_moves, _playerTimeSnapshots, _timeControl);

            var curPlayerTime = _playerTimeSnapshots[(int)playerTurn];

            await gameTimer.StartTurnTimer(curPlayerTime.MainTimeMilliseconds);
        }


        var game = _GetGame();

        {
            // Send message to player with current turn
            var currentTurnPlayer = GetPlayerIdWithTurn();
            SendNewMoveMessage(currentTurnPlayer);
        }

        return (true, game);
    }

    public async Task<Game> ContinueGame(string playerId)
    {
        Debug.Assert(_gameState == GameState.ScoreCalculation);
        _gameState = GameState.Playing;
        _stoneStates.Clear();
        _scoresAcceptedBy.Clear();

        var game = await GetGame();
        var otherPlayer = GetOtherPlayerIdFromPlayerId(playerId);

        SendContinueGameMessage(otherPlayer);

        return game;
    }

    public async Task<Game> AcceptScores(string playerId)
    {
        Debug.Assert(_gameState == GameState.ScoreCalculation);
        _scoresAcceptedBy.Add(playerId);

        var otherPlayer = GetOtherPlayerIdFromPlayerId(playerId);
        if (_scoresAcceptedBy.Count == 2)
        {
            // Game is over
            await EndGame(GameOverMethod.Score);
            SendGameOverMessage(GameOverMethod.Score);
        }
        else
        {
            SendAcceptedScoresMessage(otherPlayer);
        }

        var game = await GetGame();
        return game;
    }

    public async Task<Game> ResignGame(string playerId)
    {
        var otherPlayerId = GetOtherPlayerIdFromPlayerId(playerId);

        await EndGame(GameOverMethod.Resign, otherPlayerId);

        var game = await GetGame();

        SendGameOverMessage(GameOverMethod.Resign);

        return game;
    }

    public async Task<PlayerTimeSnapshot> TimeoutCurrentPlayer()
    {
        var playerWithTurn = GetPlayerIdWithTurn();
        var otherPlayerId = GetPlayerIdFromStoneType(GetOtherStoneFromPlayerId(playerWithTurn));

        SetRecalculatedTurnPlayerTimeSnapshots(_moves, _playerTimeSnapshots, _timeControl);

        var curPlayerTime = _playerTimeSnapshots[(int)playerTurn];

        if (curPlayerTime.MainTimeMilliseconds <= 0)
        {
            await EndGame(GameOverMethod.Timeout, otherPlayerId, true);
            SendGameOverMessage(GameOverMethod.Timeout);
            Console.WriteLine("Game timeout");
        }
        else
        {
            foreach (var playerId in _players.Keys)
            {
                SendGameTimerUpdateMessage(playerId, curPlayerTime);
            }
        }
        return curPlayerTime;
    }

    private async Task EndGame(GameOverMethod method, string? winnerId = null, bool timeStopped = false)
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


        if (winnerId != null)
        {
            _winnerId = winnerId;
        }

        if (method == GameOverMethod.Score)
        {
            var scoreCalculator = _GetScoreCalculator();
            _winnerId = scoreCalculator.GetWinner() == 0 ? GetPlayerIdFromStoneType(StoneType.Black) : GetPlayerIdFromStoneType(StoneType.White);
            _finalTerritoryScores = scoreCalculator.TerritoryScores;
        }

        _endTime = now.DeserializedDate();
        _gameOverMethod = method;

        var res = await UpdateRatingsOnResult();

        foreach (var rating in res.UserRatings)
        {
            await _userRatingService.SaveUserRatings(rating);
        }

        if (!timeStopped)
        {
            var gameTimerGrain = GrainFactory.GetGrain<IGameTimerGrain>(gameId);
            await gameTimerGrain.StopTurnTimer();
        }
    }

    public Task<Game> EditDeadStone(RawPosition rawPosition, DeadStoneState state, string editorPlayer)
    {
        Debug.Assert(_gameState == GameState.ScoreCalculation);
        _scoresAcceptedBy.Clear();

        var position = rawPosition.ToGamePosition();
        if (!(_stoneStates.ContainsKey(position) && _stoneStates[position] == state))
        {
            var boardState = _boardStateUtilities.BoardStateFromGame(_GetGame());
            var cluster = boardState.playgroundMap[position].cluster;
            foreach (var pos in cluster.data)
            {
                _stoneStates[pos] = state;
            }

        }

        SendEditDeadStoneMessage(GetOtherPlayerIdFromPlayerId(editorPlayer), rawPosition, state);

        return GetGame();
    }

    public Task<Game> GetGame()
    {
        var game = _GetGame();
        return Task.FromResult(game);
    }

    /// <summary>
    /// In case of an external error use this function
    /// Set grain state to a game supplied from database
    /// </summary>
    /// <param name="playerId"></param>
    /// <returns></returns>
    public Task<Game> ResetGame(Game game)
    {
        SetState(
            rows: game.Rows,
            columns: game.Columns,
            timeControl: game.TimeControl,
            playerTimeSnapshots: game.PlayerTimeSnapshots,
            moves: game.Moves,
            playgroundMap: game.PlaygroundMap,
            players: game.Players,
            prisoners: game.Prisoners,
            startTime: game.StartTime,
            gameState: game.GameState,
            koPositionInLastMove: game.KoPositionInLastMove,
            deadStones: game.DeadStones,
            winnerId: game.WinnerId,
            finalTerritoryScores: game.FinalTerritoryScores.ToList(),
            komi: game.Komi,
            gameOverMethod: game.GameOverMethod,
            endTime: game.EndTime,
            stoneSelectionType: game.StoneSelectionType,
            gameCreator: game.GameCreator,
            playersRatings: game.PlayersRatings,
            playersRatingsDiff: game.PlayersRatingsDiff
        );

        return GetGame();
    }

    public async Task<Game> StartMatch(Match match, string matchedPlayerId)
    {
        await CreateGame(
            rows: match.BoardSize.ToBoardSizeData().Rows,
            columns: match.BoardSize.ToBoardSizeData().Columns,
            timeControl: match.TimeControl,
            stoneSelectionType: match.StoneType,
            gameCreator: null
        );

        List<string> players = [match.CreatorId, matchedPlayerId];

        await StartGame(now, players);

        await JoinPlayersToPushGroup();

        return _GetGame();
    }

    private async Task SendMessageToClient(SignalRMessage message, string player)
    {
        try
        {
            _logger.LogInformation("Notification sent to <player>{player}<player>, <message>{message}<message>", player, message);
            var connectionId = await GrainFactory.GetGrain<IPlayerGrain>(player).GetConnectionId();
            await _hubService.SendToClient(connectionId, "gameUpdate", message, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting to host");
        }
    }


    private async Task SendMessageToAll(SignalRMessage message)
    {
        try
        {
            _logger.LogInformation("Notification sent to <group>{gameGroup}<group>, <message>{message}<message>", gameId, message);
            await _hubService.SendToGroup("gameUpdate", gameId, message, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting to host");
        }
    }

    // Grain Overrides //


    private string gameId => this.GetPrimaryKeyString();

    // private async Task<Game?> SaveGame()
    // {
    //     var game = _GetGame();
    //     return await _gameService.SaveGame(game);
    // }

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
            startTime: _startTime?.SerializedDate(),
            gameState: _gameState,
            koPositionInLastMove: _koPositionInLastMove?.ToHighLevelRepr(),
            deadStones: _stoneStates.Where((p) => p.Value == DeadStoneState.Dead).Select((k) => k.Key.ToHighLevelRepr()).ToList(),
            winnerId: _winnerId,
            finalTerritoryScores: _finalTerritoryScores.ToList(),
            komi: _komi,
            gameOverMethod: _gameOverMethod,
            endTime: _endTime?.SerializedDate(),
            gameCreator: _gameCreator,
            playersRatings: _playersRatings,
            playersRatingsDiff: _playersRatingsDiff
        );
    }

    private void SetRecalculatedTurnPlayerTimeSnapshots(List<GameMove> moves, List<PlayerTimeSnapshot> playerTimes, TimeControl timeControl)
    {


        var _now = now;

        // var lastPlayerPreInc = _playerTimeSnapshots[1 - (int)playerTurn].MainTimeMilliseconds - (int)(DateTime.Parse(_now) - DateTime.Parse(_playerTimeSnapshots[1 - (int)playerTurn].SnapshotTimestamp)).TotalMilliseconds;

        var times = _timeCalculator.RecalculateTurnPlayerTimeSnapshots(playerTurn, playerTimes, timeControl, _now);

        _logger.LogInformation("Recalculated Player Times");
        _playerTimeSnapshots = times;

        // if (_timeControl.IncrementSeconds != null)
        // {
        //     var lastPlayerNewTime = _playerTimeSnapshots[1 - (int)playerTurn].MainTimeMilliseconds;
        //     var inc = _timeControl.IncrementSeconds * 1000;
        //     Debug.Assert(lastPlayerNewTime - lastPlayerPreInc == inc, $"Increment not added correctly {lastPlayerNewTime} != {lastPlayerPreInc} + {inc}");
        // }

    }

    private async Task StartGame(string time, List<string> players)
    {
        _gameState = GameState.Playing;
        _startTime = time.DeserializedDate();
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

        var firstPlayerStone = stoneSelectionType == StoneSelectionType.Auto ? (StoneType)new Random().Next(2) : (StoneType)(int)stoneSelectionType;
        var secondPlayerStone = 1 - firstPlayerStone;

        foreach (var player in players)
        {
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

        _playersRatings = [
            (int)MathF.Floor((float)(await _userRatingService.GetUserRatings(GetPlayerIdFromStoneType(StoneType.Black))).Ratings[RatingEngine.RatingKey(_GetGame().GetBoardSize(), _timeControl.TimeStandard)].Glicko.Rating),
            (int)MathF.Floor((float)(await _userRatingService.GetUserRatings(GetPlayerIdFromStoneType(StoneType.White))).Ratings[RatingEngine.RatingKey(_GetGame().GetBoardSize(), _timeControl.TimeStandard)].Glicko.Rating),
        ];

        var gameTimer = GrainFactory.GetGrain<IGameTimerGrain>(gameId);
        await gameTimer.StartTurnTimer(_timeControl.MainTimeSeconds * 1000);
    }


    // public async Task<List<TimeSpan>> CalculatePlayerTimesOfDiscreteSections(Game game)
    // {
    //     Debug.Assert(DidStart());
    //     var mainTimeSpan = TimeSpan.FromSeconds(game.TimeControl.MainTimeSeconds);
    //     // var mainTimeSpan =TimeSpan.FromSeconds(game.TimeControl.MainTimeSeconds);
    //     var raw_times = new List<string> { game.StartTime! };
    //     raw_times.AddRange(game.Moves.Select(move => move.Time));

    //     var times = raw_times.Select(time => DateTime.Parse(time)).ToList();

    //     // Calculate first player's duration
    //     // var firstPlayerDuration = times
    //     //     .Select((time, index) => (time, index))
    //     //     .Where(pair => pair.index % 2 == 1)
    //     //     .Aggregate(TimeSpan.Zero, (duration, pair) => duration + (pair.time - times[pair.index - 1]));

    //     var firstPlayerDuration = TimeSpan.Zero;

    //     var firstPlayerArrangedTimes = times.SkipLast(times.Count % 2);
    //     var firstPlayerTimesBeforeCorrespondingMoveMade = firstPlayerArrangedTimes.Where((time, index) => index % 2 == 0).ToList();
    //     var firstPlayerMoveMadeTimes = firstPlayerArrangedTimes.Where((time, index) => index % 2 == 1).ToList();

    //     for (int i = 0; i < MathF.Floor(firstPlayerArrangedTimes.Count() / 2); i++)
    //     {
    //         firstPlayerDuration += firstPlayerMoveMadeTimes[i] - firstPlayerTimesBeforeCorrespondingMoveMade[i];
    //     }

    //     var player0Time = mainTimeSpan - firstPlayerDuration;


    //     var secondPlayerDuration = TimeSpan.Zero;
    //     var secondPlayerArrangedTimes = times.Skip(1).SkipLast(times.Count % 2);
    //     var secondPlayerTimesBeforeCorrespondingMoveMade = secondPlayerArrangedTimes.Where((time, index) => index % 2 == 0).ToList();
    //     var secondPlayerMoveMadeTimes = secondPlayerArrangedTimes.Where((time, index) => index % 2 == 1).ToList();

    //     for (int i = 0; i < MathF.Floor(secondPlayerArrangedTimes.Count() / 2); i++)
    //     {
    //         secondPlayerDuration += secondPlayerMoveMadeTimes[i] - secondPlayerTimesBeforeCorrespondingMoveMade[i];
    //     }
    //     var player1Time = mainTimeSpan - secondPlayerDuration;


    //     var playerTimes = new List<TimeSpan> { player0Time, player1Time };

    //     // If the game has ended, apply the end time to the player with the turn
    //     if (game.GameState == GameState.Ended)
    //     {
    //         playerTimes[(int)GetStoneFromPlayerId(GetPlayerIdWithTurn()!)] -= DateTime.Parse(game.EndTime!) - times.Last();
    //     }

    //     return playerTimes;
    // }

    private async Task<(List<int> RatingDiffs, List<PlayerRatingData> PrevPerfs, List<PlayerRatingData> NewPerfs, List<UserRating> UserRatings)> UpdateRatingsOnResult()
    {
        Debug.Assert(_gameState == GameState.Ended, "Game must be ended to update ratings");

        var game = _GetGame();

        var res = _ratingEngine.CalculateRatingAndPerfsAsync(
            winnerId: _winnerId!,
            boardSize: game.GetBoardSize(),
            timeStandard: game.TimeControl.TimeStandard,
            players: game.Players,
            usersRatings: [.. (await Task.WhenAll(GetPlayerIdSortedByColor().Select(a => _userRatingService.GetUserRatings(a))))],
            endTime: (DateTime)_endTime!
        );

        _playersRatingsDiff = res.RatingDiffs;

        return res;
    }

    private void SetScoreCalculationState(GameMove lastMove)
    {
        Debug.Assert(HasPassedTwice());
        Debug.Assert(lastMove.IsPass());

        var playerWithTurn = GetPlayerIdWithTurn();

        _gameState = GameState.ScoreCalculation;

        SendScoreCalculationStartedMessage(playerWithTurn);
    }

    private bool HasPassedTwice()
    {
        GameMove? prev = null;
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

    // Push Messages
    private async Task JoinPlayersToPushGroup()
    {
        foreach (var player in _players.Keys)
        {
            var playerGrain = GrainFactory.GetGrain<IPlayerGrain>(player);
            var conId = await playerGrain.GetConnectionId();
            await _hubService.AddToGroup(conId, gameId, CancellationToken.None);
        }
    }

    private async Task SendJoinMessage(string toPlayer, DateTime time, PublicUserInfo otherPlayerData)
    {
        var game = _GetGame();
        await SendMessageToClient(new SignalRMessage(
            SignalRMessageType.gameJoin,
            new GameJoinResult(
                game,
                otherPlayerData,
                time.SerializedDate()
            )
        ), toPlayer);
    }

    private async void SendNewMoveMessage(string toPlayer)
    {
        var game = _GetGame();
        await SendMessageToClient(new SignalRMessage(
            SignalRMessageType.newMove,
            new NewMoveMessage(game)
        ), toPlayer);
    }

    private async void SendScoreCalculationStartedMessage(string toPlayer)
    {
        await SendMessageToClient(new SignalRMessage(
            SignalRMessageType.scoreCaculationStarted,
            null
        ), toPlayer);
    }

    private async void SendEditDeadStoneMessage(string toPlayer, RawPosition position, DeadStoneState state)
    {
        await SendMessageToClient(new SignalRMessage(
            SignalRMessageType.editDeadStone,
            new EditDeadStoneMessage(position, state, _GetGame())
        ), toPlayer);
    }

    private async void SendContinueGameMessage(string toPlayer)
    {
        var game = _GetGame();
        await SendMessageToClient(new SignalRMessage(
            SignalRMessageType.continueGame,
            new ContinueGameMessage(game)
        ), toPlayer);
    }

    private async void SendAcceptedScoresMessage(string toPlayer)
    {
        await SendMessageToClient(new SignalRMessage(
            SignalRMessageType.acceptedScores,
            null
        ), toPlayer);
    }

    private async void SendGameOverMessage(GameOverMethod method)
    {
        var game = _GetGame();
        await SendMessageToAll(new SignalRMessage(
            SignalRMessageType.gameOver,
            new GameOverMessage(method, game)
        ));
    }

    private async void SendGameTimerUpdateMessage(string toPlayer, PlayerTimeSnapshot playerTime)
    {
        var playerWithTurn = GetPlayerIdWithTurn();
        await SendMessageToClient(new SignalRMessage(
            SignalRMessageType.gameTimerUpdate,
            new GameTimerUpdateMessage(
                playerTime,
                GetStoneFromPlayerId(playerWithTurn)
            )
        ), toPlayer);
    }

    // Helpers

    private bool DidStart()
    {
        return _GetGame().DidStart();
    }


    private string GetPlayerIdWithTurn()
    {
        Debug.Assert(DidStart());
        return _GetGame().GetPlayerIdWithTurn()!;
    }


    private StoneType GetStoneFromPlayerId(string id)
    {
        Debug.Assert(DidStart());
        return (StoneType)_GetGame().Players.GetStoneFromPlayerId(id)!;
    }


    private StoneType GetOtherStoneFromPlayerId(string id)
    {
        Debug.Assert(DidStart());
        return (StoneType)_GetGame().Players.GetOtherStoneFromPlayerId(id)!;
    }


    private string GetPlayerIdFromStoneType(StoneType stone)
    {
        Debug.Assert(DidStart());
        return _GetGame().Players.GetPlayerIdFromStoneType(stone)!;
    }

    public string GetOtherPlayerIdFromPlayerId(string id)
    {
        Debug.Assert(DidStart());
        return _GetGame().Players.GetOtherPlayerIdFromPlayerId(id)!;
    }


    public List<string> GetPlayerIdSortedByColor()
    {
        Debug.Assert(DidStart());
        return _GetGame().Players.GetPlayerIdSortedByColor();
    }
}