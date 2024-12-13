using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection.Metadata;
using BadukServer.Services;
using Microsoft.CodeAnalysis;

namespace BadukServer.Orleans.Grains;

public class GameGrain : Grain, IGameGrain
{
    // Player id with player's stone; 0 for black, 1 for white
    private Dictionary<string, StoneType> _players = [];
    private GameState _gameState;
    private List<GameMove> _moves = [];
    private int _rows;
    private int _columns;
    private TimeControl _timeControl = null!;
    private StoneSelectionType _stoneSelectionType;
    private List<PlayerTimeSnapshot> _playerTimeSnapshots { get; set; } = [];
    private Dictionary<Position, StoneType> _board = [];
    private List<int> _prisoners = [];
    private ReadOnlyCollection<int> _finalTerritoryScores = new ReadOnlyCollection<int>([]);
    private DateTime? _startTime;
    private Position? _koPositionInLastMove;
    private int turn => _moves.Count;
    private StoneType playerTurn => (StoneType)(turn % 2);
    private HashSet<string> _scoresAcceptedBy = [];

    // Score Calculation Things
    private Dictionary<Position, DeadStoneState> _stoneStates = [];
    private GameOverMethod? _gameOverMethod;

    private List<int> _playersRatingsDiff = [];
    private List<string> _playersRatingsAfter = [];

    private GameResult? _gameResult;
    private DateTime? _endTime;
    private string? _gameCreator;
    public GameType _gameType;
    private float _komi = 6.5f;

    private string now => _dateTimeService.NowFormatted();

    // External stuff
    public Dictionary<string, PlayerInfo> PlayerInfos = [];

    // Injected
    private readonly ILogger<GameGrain> _logger;
    private readonly IDateTimeService _dateTimeService;
    private readonly IUserRatingService _userRatingService;
    private readonly IUserStatService _userStatService;
    private readonly IGameService _gameService;
    private readonly ISignalRHubService _hubService;
    private readonly ITimeCalculator _timeCalculator;
    private BoardStateUtilities _boardStateUtilities;
    private IRatingEngine _ratingEngine;
    private IStatCalculator _statCalculator;


    public GameGrain(ILogger<GameGrain> logger, IDateTimeService dateTimeService, IUsersService usersService, IUserRatingService userRatingService, IUserStatService userStatService, IGameService gameService, ISignalRHubService hubService, IRatingEngine ratingEngine, IPlayerInfoService publicUserInfo, ITimeCalculator timeCalculator, IStatCalculator statCalculator)
    {
        _logger = logger;
        _dateTimeService = dateTimeService;
        _userRatingService = userRatingService;
        _gameService = gameService;
        _boardStateUtilities = new BoardStateUtilities();
        _ratingEngine = ratingEngine;
        _hubService = hubService;
        _userStatService = userStatService;
        _timeCalculator = timeCalculator;
        _statCalculator = statCalculator;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var gameId = this.GetPrimaryKeyString();

        _logger.LogInformation("Game grain activated: {GameId}", gameId);

        var game = await _gameService.GetGame(gameId);

        if (game != null)
        {
            await ResetGame(game);
        }

        await base.OnActivateAsync(cancellationToken);
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
        GameResult? gameResult,
        List<int> finalTerritoryScores,
        float komi,
        GameOverMethod? gameOverMethod,
        string? endTime,
        StoneSelectionType stoneSelectionType,
        string? gameCreator,
        List<int> playersRatingsDiff,
        List<string> playersRatingsAfter
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
        _gameResult = gameResult;
        _finalTerritoryScores = new ReadOnlyCollection<int>(finalTerritoryScores);
        _komi = komi;
        _gameOverMethod = gameOverMethod;
        _endTime = endTime?.DeserializedDate();
        _stoneSelectionType = stoneSelectionType;
        _gameCreator = gameCreator;
        _playersRatingsDiff = playersRatingsDiff;
        _playersRatingsAfter = playersRatingsAfter;
    }

    // Grain overrides

    public async Task CreateGame(GameCreationData creationData, PlayerInfo? gameCreatorData, GameType gameType)
    {
        var gameCreator = gameCreatorData?.Id;
        _gameType = gameType;

        if (gameCreator != null)
        {
            PlayerInfos[gameCreator] = gameCreatorData!;
        }
        SetState(
            rows: creationData.Rows,
            columns: creationData.Columns,
            timeControl: new TimeControl(creationData.TimeControl),
            playerTimeSnapshots: [],
            moves: [],
            playgroundMap: [],
            players: [],
            prisoners: [0, 0],
            startTime: null,
            gameState: GameState.WaitingForStart,
            koPositionInLastMove: null,
            deadStones: [],
            gameResult: null,
            finalTerritoryScores: [],
            komi: 6.5f,
            gameOverMethod: null,
            endTime: null,
            stoneSelectionType: creationData.FirstPlayerStone,
            gameCreator: gameCreator,
            playersRatingsDiff: [],
            playersRatingsAfter: []
        );

        await TrySaveGame();
    }

    public async Task<(Game game, PlayerInfo? otherPlayerInfo, bool justJoined)> JoinGame(PlayerInfo playerData, string time)
    {
        var player = playerData.Id;

        if (_gameCreator == player)
        {
            return (
                _GetGame(), DidStart() ? PlayerInfos[GetOtherPlayerIdFromPlayerId(player)] : null, false
            );
        }

        if (_players.Keys.Contains(player))
        {
            return (
                _GetGame(), PlayerInfos[GetOtherPlayerIdFromPlayerId(player)], false
            );
        }

        PlayerInfos[player] = playerData;

        await StartGame(time, [_gameCreator, player]);

        var otherPlayerInfo = PlayerInfos[GetOtherPlayerIdFromPlayerId(player)]; ;

        var game = _GetGame();

        // await JoinPlayersToPushGroup();
        // await SendJoinMessage(otherPlayerInfo.Id, time.DeserializedDate(), otherPlayerInfo);

        await TrySaveGame();
        return (game, otherPlayerInfo, true);
    }

    // public Task<Dictionary<string, StoneType>> GetPlayers()
    // {
    //     return Task.FromResult(_players);
    // }

    // public Task<List<GameMove>> GetMoves()
    // {
    //     return Task.FromResult(_moves);
    // }

    // public Task<GameState> GetState()
    // {
    //     return Task.FromResult(_gameState);
    // }

    public async Task<(bool moveSuccess, Game game)> MakeMove(MovePosition move, string playerId)
    {
        Debug.Assert(_players.ContainsKey(playerId));

        if (_gameState != GameState.Playing)
        {
            throw new InvalidOperationException("Game is not in playing state");
        }
        var player = _players[playerId];

        if ((turn % 2) != (int)player)
        {
            throw new InvalidOperationException("Not player's turn");
        }

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
            await SendNewMoveMessage(currentTurnPlayer);
        }


        await TrySaveGame();
        return (true, game);
    }

    public async Task<Game> ContinueGame(string playerId)
    {
        Debug.Assert(_players.ContainsKey(playerId));
        if (_gameState != GameState.ScoreCalculation)
        {
            throw new InvalidOperationException("Game is not in score calculation state");
        }

        _gameState = GameState.Playing;
        _stoneStates.Clear();
        _scoresAcceptedBy.Clear();

        var game = await GetGame();
        var otherPlayer = GetOtherPlayerIdFromPlayerId(playerId);

        ResumeActivePlayerTimer();

        await SendContinueGameMessage(otherPlayer);

        await TrySaveGame();
        return game;
    }

    public async Task<Game> AcceptScores(string playerId)
    {
        Debug.Assert(_players.ContainsKey(playerId));
        if (_gameState != GameState.ScoreCalculation)
        {
            throw new InvalidOperationException("Game is not in score calculation state");
        }

        _scoresAcceptedBy.Add(playerId);

        var otherPlayer = GetOtherPlayerIdFromPlayerId(playerId);
        if (_scoresAcceptedBy.Count == 2)
        {
            // Game is over
            await EndGame(GameOverMethod.Score);
            await SendGameOverMessage(GameOverMethod.Score);
        }
        else
        {
            await SendAcceptedScoresMessage(otherPlayer);
        }

        var game = await GetGame();

        await TrySaveGame();
        return game;
    }

    public async Task<Game> EditDeadStone(RawPosition rawPosition, DeadStoneState state, string editorPlayer)
    {
        Debug.Assert(_players.ContainsKey(editorPlayer));

        if (_gameState != GameState.ScoreCalculation)
        {
            throw new InvalidOperationException("Game is not in score calculation state");
        }
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

        await SendEditDeadStoneMessage(GetOtherPlayerIdFromPlayerId(editorPlayer), rawPosition, state);

        await TrySaveGame();
        return _GetGame();
    }

    public async Task<Game> ResignGame(string playerId)
    {

        if (!DidStart())
        {
            throw new InvalidOperationException("Game hasn't started yet");
        }

        Debug.Assert(_players.ContainsKey(playerId));

        var myStone = GetStoneFromPlayerId(playerId);

        await EndGame(GameOverMethod.Resign, myStone.ResultForOtherWon());

        var game = await GetGame();

        await SendGameOverMessage(GameOverMethod.Resign);


        await TrySaveGame();
        return game;
    }

    public async Task<PlayerTimeSnapshot> TimeoutCurrentPlayer()
    {
        var playerWithTurn = GetPlayerIdWithTurn();
        var myStone = GetStoneFromPlayerId(playerWithTurn);

        SetRecalculatedTurnPlayerTimeSnapshots(_moves, _playerTimeSnapshots, _timeControl);

        var curPlayerTime = _playerTimeSnapshots[(int)playerTurn];

        if (curPlayerTime.MainTimeMilliseconds <= 0)
        {
            await EndGame(GameOverMethod.Timeout, myStone.ResultForOtherWon(), true);
            await SendGameOverMessage(GameOverMethod.Timeout);
            Console.WriteLine("Game timeout");
        }
        else
        {
            foreach (var playerId in _players.Keys)
            {
                await SendGameTimerUpdateMessage(playerId, curPlayerTime);
            }
        }
        return curPlayerTime;
    }

    private async Task EndGame(GameOverMethod method, GameResult? _result = null, bool timeStopped = false)
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


        if (_result != null)
        {
            _gameResult = _result;
        }

        if (method == GameOverMethod.Score)
        {
            var scoreCalculator = _GetScoreCalculator();
            _gameResult = scoreCalculator.GetResult();
            _finalTerritoryScores = scoreCalculator.TerritoryScores;
        }

        _endTime = now.DeserializedDate();
        _gameOverMethod = method;

        await TryUpdatePlayerData();

        if (!timeStopped)
        {
            var gameTimerGrain = GrainFactory.GetGrain<IGameTimerGrain>(gameId);
            await gameTimerGrain.StopTurnTimer();
        }
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
            gameResult: game.Result,
            finalTerritoryScores: game.FinalTerritoryScores.ToList(),
            komi: game.Komi,
            gameOverMethod: game.GameOverMethod,
            endTime: game.EndTime,
            stoneSelectionType: game.StoneSelectionType,
            gameCreator: game.GameCreator,
            playersRatingsDiff: game.PlayersRatingsDiff,
            playersRatingsAfter: game.PlayersRatingsAfter
        );

        return GetGame();
    }

    public async Task<Game> StartMatch(Match match, List<PlayerInfo> playerInfos)
    {
        await CreateGame(
                new GameCreationData(
                rows: match.BoardSize.ToBoardSizeData().Rows,
                columns: match.BoardSize.ToBoardSizeData().Columns,
                timeControl: match.TimeControl,
                firstPlayerStone: match.StoneType
            ),
            gameCreatorData: null,
            match.GameType
        );

        List<string> players = playerInfos.Select(a => a.Id).ToList();
        PlayerInfos = players.Zip(playerInfos).ToDictionary(a => a.First, a => a.Second);

        await StartGame(now, players);

        // await JoinPlayersToPushGroup();

        return _GetGame();
    }

    private async Task SendMessageToClient(SignalRMessage message, string player)
    {
        try
        {
            var connectionId = await GrainFactory.GetGrain<IPlayerGrain>(player).GetConnectionId();
            if (connectionId != null)
            {
                _logger.LogInformation("Notification sent to <player>{player}<player>, <message>{message}<message>", player, message);
                await _hubService.SendToClient(connectionId, "gameUpdate", message, CancellationToken.None);
            }
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
            result: _gameResult,
            finalTerritoryScores: _finalTerritoryScores.ToList(),
            komi: _komi,
            gameOverMethod: _gameOverMethod,
            endTime: _endTime?.SerializedDate(),
            gameCreator: _gameCreator,
            playersRatingsDiff: _playersRatingsDiff,
            playersRatingsAfter: _playersRatingsAfter,
            gameType: _gameType
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
        _logger.LogInformation("Game started <gameId>{gameId}<gameId>", gameId);
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

        var gameTimer = GrainFactory.GetGrain<IGameTimerGrain>(gameId);
        await gameTimer.StartTurnTimer(_timeControl.MainTimeSeconds * 1000);
    }

    private string MinifiedRating(DateTime time, PlayerRatingsData? ratD)
    {
        if (ratD == null)
        {
            return "?";
        }

        return new MinimalRating((int)ratD.Glicko.Rating, _ratingEngine.IsRatingProvisional(ratD, time)).Stringify();
    }


    private async Task TryUpdatePlayerData()
    {
        await TrySaveGame();
        if (_gameType == GameType.Rated)
        {
            var (_, _, newPerfs, _) = await UpdateRatingsOnResult();
            var stat = await UpdateStatsOnResult();

            StoneTypeExt.GetValuesSafe().Zip(newPerfs.Zip(stat)).ToList().ForEach(async (a) =>
            {
                var (stone, (perf, stat)) = a;
                var playerGrain = GrainFactory.GetGrain<IPlayerGrain>(_players.GetPlayerIdFromStoneType(stone));
                var pushGrain = GrainFactory.GetGrain<IPushNotifierGrain>(await playerGrain.GetConnectionId());
                await pushGrain.SendMessageToMe(new SignalRMessage(
                    type: SignalRMessageType.statUpdate,
                    data: new StatUpdateMessage(stat, perf, _GetGame().GetTopLevelVariant().ToKey())
                ));
            });
        }
    }


    private async Task TrySaveGame()
    {
        if (_gameType != GameType.Anonymous)
        {
            await SaveGame(gameId);
        }
    }

    private async Task<Game> SaveGame(string gameId)
    {
        Debug.Assert(_gameType != GameType.Anonymous, "Game must not be anonymous to be updated");
        var curGame = _GetGame();
        var res = await _gameService.SaveGame(curGame);

        if (res == null)
        {
            var oldGame = await _gameService.GetGame(gameId);

            if (oldGame != null)
            {
                await ResetGame(oldGame);
            }

            throw new Exception("Failed to save game");
        }
        else
        {
            return res;
        }
    }

    // private async Task TryUpdateRatings()
    // {
    //     if (_gameType == GameType.Rated)
    //     {
    //         var res = await UpdateRatingsOnResult();
    //         return player
    //     }
    // }

    private async Task<(List<int> RatingDiffs, List<PlayerRatingsData> PrevPerfs, List<PlayerRatingsData> NewPerfs, List<PlayerRatings> UserRatings)> UpdateRatingsOnResult()
    {
        Debug.Assert(_gameState == GameState.Ended, "Game must be ended to update ratings");
        Debug.Assert(_gameType == GameType.Rated, "Game must be rated to update ratings");

        var game = _GetGame();

        var res = _ratingEngine.CalculateRatingAndPerfsAsync(
            gameResult: (GameResult)_gameResult!,
            gameVariant: game.GetTopLevelVariant(),
            players: game.Players,
            usersRatings: [.. (await Task.WhenAll(GetPlayerIdSortedByColor().Select(a => _userRatingService.GetUserRatings(a))))],
            endTime: (DateTime)_endTime!
        );

        _playersRatingsAfter = res.NewPerfs.Select(a => MinifiedRating((DateTime)_endTime!, a)).ToList();
        _playersRatingsDiff = res.RatingDiffs;

        foreach (var rating in res.UserRatings)
        {
            await _userRatingService.SaveUserRatings(rating);
        }

        return res;
    }


    // private async Task TryUpdateStats()
    // {
    //     if (_gameType == GameType.Rated)
    //     {
    //         await UpdateStatsOnResult();
    //     }
    // }

    private async Task<List<UserStatForVariant>> UpdateStatsOnResult()
    {
        Debug.Assert(_gameState == GameState.Ended, "Game must be ended to update stats");
        Debug.Assert(_gameType != GameType.Anonymous, "Game must not be anonymous to update stats");

        var game = _GetGame();

        var players = GetPlayerIdSortedByColor();

        var userStat = new List<UserStatForVariant>();
        foreach (var player in players)
        {
            var oldStats = (await _userStatService.GetUserStat(player))!;

            var res = _statCalculator.CalculateUserStat(
                oldStats, game
            );

            var stat = await _userStatService.SaveUserStat(res);
            userStat.Add(stat.Stats[game.GetTopLevelVariant().ToKey()]);
        }
        return userStat;
    }


    private async void SetScoreCalculationState(GameMove lastMove)
    {
        Debug.Assert(HasPassedTwice());
        Debug.Assert(lastMove.IsPass());

        var playerWithTurn = GetPlayerIdWithTurn();

        SetRecalculatedTurnPlayerTimeSnapshots(
            _moves, _playerTimeSnapshots, _timeControl
        );

        PauseActivePlayerTimer();

        _gameState = GameState.ScoreCalculation;

        await SendScoreCalculationStartedMessage(playerWithTurn);
    }

    private async void PauseActivePlayerTimer()
    {
        var timerGrain = GrainFactory.GetGrain<IGameTimerGrain>(gameId);
        await timerGrain.StopTurnTimer();

        var oldData = _playerTimeSnapshots[(int)playerTurn];

        _playerTimeSnapshots[(int)playerTurn] = new PlayerTimeSnapshot(
            snapshotTimestamp: oldData.SnapshotTimestamp,
            mainTimeMilliseconds: oldData.MainTimeMilliseconds,
            byoYomisLeft: oldData.ByoYomisLeft,
            byoYomiActive: oldData.ByoYomiActive,
            timeActive: false
        );
    }

    private async void ResumeActivePlayerTimer()
    {
        var timerGrain = GrainFactory.GetGrain<IGameTimerGrain>(gameId);

        var oldData = _playerTimeSnapshots[(int)playerTurn];

        _playerTimeSnapshots[(int)playerTurn] = new PlayerTimeSnapshot(
            snapshotTimestamp: _dateTimeService.Now().SerializedDate(),
            mainTimeMilliseconds: oldData.MainTimeMilliseconds,
            byoYomisLeft: oldData.ByoYomisLeft,
            byoYomiActive: oldData.ByoYomiActive,
            timeActive: true
        );

        var newData = _playerTimeSnapshots[(int)playerTurn];

        await timerGrain.StartTurnTimer(newData.MainTimeMilliseconds);
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
    // private async Task JoinPlayersToPushGroup()
    // {
    //     foreach (var player in _players.Keys)
    //     {
    //         _logger.LogInformation("Player <player>{player}<player> joined push group <gameId>{gameId}<gameId>", player, gameId);
    //         var playerGrain = GrainFactory.GetGrain<IPlayerGrain>(player);
    //         var conId = await playerGrain.GetConnectionId();
    //     }
    // }

    // private async Task SendJoinMessage(string toPlayer, DateTime time, PlayerInfo otherPlayerData)
    // {
    //     var game = _GetGame();
    //     await SendMessageToClient(new SignalRMessage(
    //         SignalRMessageType.gameJoin,
    //         new GameJoinResult(
    //             game,
    //             otherPlayerData,
    //             time.SerializedDate()
    //         )
    //     ), toPlayer);
    // }

    private async Task SendNewMoveMessage(string toPlayer)
    {
        var game = _GetGame();
        await SendMessageToClient(new SignalRMessage(
            SignalRMessageType.newMove,
            new NewMoveMessage(game)
        ), toPlayer);
    }

    private async Task SendScoreCalculationStartedMessage(string toPlayer)
    {
        await SendMessageToClient(new SignalRMessage(
            SignalRMessageType.scoreCaculationStarted,
            null
        ), toPlayer);
    }

    private async Task SendEditDeadStoneMessage(string toPlayer, RawPosition position, DeadStoneState state)
    {
        await SendMessageToClient(new SignalRMessage(
            SignalRMessageType.editDeadStone,
            new EditDeadStoneMessage(position, state, _GetGame())
        ), toPlayer);
    }

    private async Task SendContinueGameMessage(string toPlayer)
    {
        var game = _GetGame();
        await SendMessageToClient(new SignalRMessage(
            SignalRMessageType.continueGame,
            new ContinueGameMessage(game)
        ), toPlayer);
    }

    private async Task SendAcceptedScoresMessage(string toPlayer)
    {
        await SendMessageToClient(new SignalRMessage(
            SignalRMessageType.acceptedScores,
            null
        ), toPlayer);
    }

    private async Task SendGameOverMessage(GameOverMethod method)
    {
        var game = _GetGame();
        await SendMessageToAll(new SignalRMessage(
            SignalRMessageType.gameOver,
            new GameOverMessage(method, game)
        ));
    }

    private async Task SendGameTimerUpdateMessage(string toPlayer, PlayerTimeSnapshot playerTime)
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