using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection.Metadata;
using BadukServer.Services;
using Google.Apis.Util;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Payloads;

namespace BadukServer.Orleans.Grains;

public class GameGrain : Grain, IGameGrain
{
    // Player id with player's stone; 0 for black, 1 for white
    private List<string> _players = [];
    private List<string> _usernames { get => _players.Select(a => _playerInfos[a].GetUsername()).ToList(); }
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
    private DateTime _creationTime;
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
    private DateTime now => _dateTimeService.Now();

    // External stuff
    // Only used for getting usernames
    private Dictionary<string, PlayerInfo> _playerInfos = [];
    // Time when both players have entered and game is ready to start
    private DateTime _joinTime;


    // Own Timer stuff (Used for acknowledging game starts)
    private IDisposable _timerHandle = null!;
    private bool _isTimerActive;

    // Injected
    private readonly ILogger<GameGrain> _logger;
    private readonly IDateTimeService _dateTimeService;
    private readonly IUserRatingService _userRatingService;
    private readonly IUserStatService _userStatService;
    private readonly IGameService _gameService;
    private readonly ISignalRHubService _hubService;
    private readonly ITimeCalculator _timeCalculator;
    private readonly IPlayerInfoService _playerInfoService;

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
        _playerInfoService = publicUserInfo;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var gameId = this.GetPrimaryKeyString();

        _logger.LogInformation("Game grain activated: {GameId}", gameId);

        var game = await _gameService.GetGame(gameId);

        if (game != null)
        {
            await ResetGame(game);

            if (DidStart() && !DidEnd())
            {
                await EndGame(GameOverMethod.Abandon, GameResult.NoResult, true);
                await TrySaveGame();
            }
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
        List<string> players,
        List<int> prisoners,
        DateTime? startTime,
        GameState gameState,
        string? koPositionInLastMove,
        List<string> deadStones,
        GameResult? gameResult,
        List<int> finalTerritoryScores,
        float komi,
        GameOverMethod? gameOverMethod,
        DateTime? endTime,
        StoneSelectionType stoneSelectionType,
        string? gameCreator,
        List<int> playersRatingsDiff,
        List<string> playersRatingsAfter,
        DateTime creationTime,
        GameType gameType
    )
    {
        _rows = rows;
        _columns = columns;
        _timeControl = timeControl;
        _playerTimeSnapshots = playerTimeSnapshots;
        _moves = moves;
        _board = playgroundMap.ToDictionary(a => new Position(a.Key), a => a.Value);
        Debug.Assert(players.Count == 0 || players.Count == 2, "Players must be 0 or 2");
        _players = players;
        _prisoners = prisoners;
        _startTime = startTime;
        _gameState = gameState;
        _koPositionInLastMove = koPositionInLastMove == null ? null : new Position(koPositionInLastMove);
        _stoneStates = deadStones.Select(a => new Position(a)).ToDictionary(a => a, a => DeadStoneState.Dead);
        _gameResult = gameResult;
        _finalTerritoryScores = new ReadOnlyCollection<int>(finalTerritoryScores);
        _komi = komi;
        _gameOverMethod = gameOverMethod;
        _endTime = endTime;
        _stoneSelectionType = stoneSelectionType;
        _gameCreator = gameCreator;
        _playersRatingsDiff = playersRatingsDiff;
        _playersRatingsAfter = playersRatingsAfter;
        _creationTime = creationTime;
        _gameType = gameType;
    }

    // Grain overrides

    public async Task CreateGame(GameCreationData creationData, PlayerInfo? gameCreatorData, GameType gameType)
    {
        var gameCreator = gameCreatorData?.Id;

        if (gameCreator != null)
        {
            _playerInfos[gameCreator] = gameCreatorData!;
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
            playersRatingsAfter: [],
            creationTime: now,
            gameType
        );

        await TrySaveGame();
    }

    public async Task<(Game game, DateTime joinTime, bool justJoined)> JoinGame(PlayerInfo playerData)
    {
        Debug.Assert(_gameCreator != null);

        var player = playerData.Id;

        if (DidEnd())
        {
            throw new InvalidOperationException("Game has ended");
        }

        if (BothPlayersIn())
        {
            return (_GetGame(), _joinTime, false);
        }

        if (_gameCreator == player)
        {
            throw new InvalidOperationException("Creator can't join their own game");
        }


        _playerInfos[player] = playerData;

        var joinTime = await EnterGameByPlayers(_gameCreator, player);

        await StartGameStartDelayTimer(joinTime);

        var game = _GetGame();

        await TrySaveGame();
        return (game, joinTime, true);
    }

    public async Task<(bool moveSuccess, Game game)> MakeMove(MovePosition move, string playerId)
    {
        if (!DidStart() && _players.Count == 2)
        {
            _logger.LogInformation("Starting game by making move");
            await StartDelayTimeout(0);
        }

        if (!_players.Contains(playerId))
        {
            throw new InvalidOperationException("Player not in game");
        }

        if (_gameState != GameState.Playing)
        {
            throw new InvalidOperationException("Game is not in playing state");
        }
        var player = _players.GetStoneFromPlayerId(playerId)!;

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
            (int)now.Subtract(_startTime!.Value).TotalSeconds
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
        if (!_players.Contains(playerId))
        {
            throw new InvalidOperationException("Player not in game");
        }

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
        if (!_players.Contains(playerId))
        {
            throw new InvalidOperationException("Player not in game");
        }

        if (_gameState != GameState.ScoreCalculation)
        {
            throw new InvalidOperationException("Game is not in score calculation state");
        }

        _scoresAcceptedBy.Add(playerId);

        var otherPlayer = GetOtherPlayerIdFromPlayerId(playerId);
        if (_scoresAcceptedBy.Count == 2)
        {
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
        if (!_players.Contains(editorPlayer))
        {
            throw new InvalidOperationException("Player not in game");
        }

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

        if (!_players.Contains(playerId))
        {
            throw new InvalidOperationException("Player not in game");
        }

        Debug.Assert(_players.Contains(playerId));

        var myStone = GetStoneFromPlayerId(playerId);

        SetRecalculatedTurnPlayerTimeSnapshots(_moves, _playerTimeSnapshots, _timeControl);

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
            foreach (var playerId in _players)
            {
                await SendGameTimerUpdateMessage(playerId, curPlayerTime);
            }
        }
        return curPlayerTime;
    }

    // Grain methods ---/

    private int startDelay = 10;

    private Task StartGameStartDelayTimer(DateTime activationTime)
    {
        if (_isTimerActive)
        {
            return Task.CompletedTask; // Timer already active
        }

        _isTimerActive = true;
        _timerHandle = this.RegisterGrainTimer(
            StartDelayTimeout,
            this,
            TimeSpan.FromSeconds(startDelay) - (now - activationTime),
            TimeSpan.FromMilliseconds(-1));

        return Task.CompletedTask;
    }


    private Task OpponentConnectionUpdateTimer()
    {
        if (_isTimerActive)
        {
            return Task.CompletedTask; // Timer already active
        }

        _isTimerActive = true;
        _timerHandle = this.RegisterGrainTimer(
            OpponentConnectionTimeout,
            this,
            TimeSpan.FromSeconds(10),
            TimeSpan.FromMilliseconds(-1));

        return Task.CompletedTask;
    }

    private async Task OpponentConnectionTimeout(object state)
    {
        await StopActiveTimer();
        await SendOpponentConnectionUpdates();
        await OpponentConnectionUpdateTimer();
    }

    private async Task StartDelayTimeout(object state)
    {
        await StopActiveTimer();
        await StartGame(now);
        await OpponentConnectionUpdateTimer();
    }

    public ValueTask StopActiveTimer()
    {
        if (_isTimerActive)
        {
            _timerHandle?.Dispose();
            _isTimerActive = false;
        }
        return new();
    }


    private async Task EndGame(GameOverMethod method, GameResult? _result = null, bool timeStopped = false)
    {
        _logger.LogInformation("Game ended <gameId>{gameId}<gameId> via {method} with {result}", gameId, method, _result);

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

        _endTime = now;
        _gameOverMethod = method;

        await TryUpdatePlayerData();

        if (!timeStopped)
        {
            var gameTimerGrain = GrainFactory.GetGrain<IGameTimerGrain>(gameId);
            await gameTimerGrain.StopTurnTimer();
        }

        await StopActiveTimer();
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
    public async Task<Game> ResetGame(Game game)
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
            playersRatingsAfter: game.PlayersRatingsAfter,
            creationTime: game.CreationTime,
            gameType: game.GameType
        );

        if (BothPlayersIn())
        {
            foreach (var player in _players)
            {
                if (!_playerInfos.ContainsKey(player))
                {
                    _playerInfos[player] = await _playerInfoService.GetPublicUserInfoForPlayer(player, _gameType.AllowedPlayerType());
                }
            }
        }
        else if (_gameCreator != null)
        {
            if (!_playerInfos.ContainsKey(_gameCreator))
            {
                _playerInfos[_gameCreator] = (await _playerInfoService.GetPublicUserInfoForPlayer(_gameCreator, _gameType.AllowedPlayerType()))!;
            }
        }

        return _GetGame();
    }

    public async Task<(Game, DateTime)> StartMatch(Match match, List<PlayerInfo> playerInfos)
    {
        await CreateGame(
                new GameCreationData(
                rows: match.BoardSize.ToBoardSizeData().Rows,
                columns: match.BoardSize.ToBoardSizeData().Columns,
                timeControl: match.TimeControl,
                firstPlayerStone: StoneSelectionType.Auto
            ),
            gameCreatorData: null,
            match.GameType
        );

        List<string> players = playerInfos.Select(a => a.Id).ToList();
        _playerInfos = players.Zip(playerInfos).ToDictionary(a => a.First, a => a.Second);

        var time = await EnterGameByPlayers(players[0], players[1]);
        await StartGameStartDelayTimer(time);

        return (_GetGame(), time);
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

    // private async Task<GamePlayersAggregate> _GetGameAggr()
    // {
    //     return new GamePlayersAggregate(
    //         game: _GetGame(),
    //         players: (await Task.WhenAll(_players.Select(async a => await _playerInfoService.GetPublicUserInfoForPlayer(a, _playerInfos[a].PlayerType)))).Select(a => a!).ToList()
    //     );
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
            startTime: _startTime,
            gameState: _gameState,
            koPositionInLastMove: _koPositionInLastMove?.ToHighLevelRepr(),
            deadStones: _stoneStates.Where((p) => p.Value == DeadStoneState.Dead).Select((k) => k.Key.ToHighLevelRepr()).ToList(),
            result: _gameResult,
            finalTerritoryScores: _finalTerritoryScores.ToList(),
            komi: _komi,
            gameOverMethod: _gameOverMethod,
            endTime: _endTime,
            gameCreator: _gameCreator,
            playersRatingsDiff: _playersRatingsDiff,
            playersRatingsAfter: _playersRatingsAfter,
            gameType: _gameType,
            usernames: _usernames,
            creationTime: _creationTime
        );
    }

    private void SetRecalculatedTurnPlayerTimeSnapshots(List<GameMove> moves, List<PlayerTimeSnapshot> playerTimes, TimeControl timeControl)
    {
        var _now = now;
        var times = _timeCalculator.RecalculateTurnPlayerTimeSnapshots(playerTurn, playerTimes, timeControl, _now.SerializedDate());

        _logger.LogInformation("Recalculated Player Times");
        _playerTimeSnapshots = times;
    }

    /// <summary>
    /// Enter both players into game
    /// </summary>
    /// <param name="firstPlayer">The first player will have the preferred stone</param>
    /// <param name="secondPlayer"></param>
    /// <returns></returns>
    private Task<DateTime> EnterGameByPlayers(string firstPlayer, string secondPlayer)
    {
        Debug.Assert(firstPlayer != secondPlayer);

        var stoneSelectionType = _stoneSelectionType;

        var firstPlayerStone = stoneSelectionType == StoneSelectionType.Auto ? (StoneType)new Random().Next(2) : (StoneType)(int)stoneSelectionType;
        var secondPlayerStone = 1 - firstPlayerStone;

        Dictionary<StoneType, string> firstAndSecondPlayer = new Dictionary<StoneType, string>
        {
            [firstPlayerStone] = firstPlayer,
            [secondPlayerStone] = secondPlayer
        };

        List<StoneType> firstAndSecondPlayerStones = [firstPlayerStone, secondPlayerStone];

        firstAndSecondPlayerStones.Sort((a, b) => a.CompareTo(b));

        _players = firstAndSecondPlayerStones.Select(a => firstAndSecondPlayer[a]).ToList();
        _prisoners = [0, 0];

        _joinTime = now;

        return Task.FromResult(_joinTime);
    }

    private async Task StartGame(DateTime time)
    {
        Debug.Assert(_playerInfos.Count == 2);

        _logger.LogInformation("Game started <gameId>{gameId}<gameId>", gameId);
        _gameState = GameState.Playing;
        _startTime = time;

        _playerTimeSnapshots = [
                    new PlayerTimeSnapshot(
                snapshotTimestamp: time.SerializedDate(),
                mainTimeMilliseconds: _timeControl.MainTimeSeconds * 1000,
                byoYomisLeft: _timeControl.ByoYomiTime?.ByoYomis,
                byoYomiActive: false,
                timeActive: true
            ),
            new PlayerTimeSnapshot(
                snapshotTimestamp: time.SerializedDate(),
                mainTimeMilliseconds: _timeControl.MainTimeSeconds * 1000,
                byoYomisLeft: _timeControl.ByoYomiTime?.ByoYomis,
                byoYomiActive: false,
                timeActive: false
            )];


        var gameTimer = GrainFactory.GetGrain<IGameTimerGrain>(gameId);
        await gameTimer.StartTurnTimer(_timeControl.MainTimeSeconds * 1000);

        await SendGameStartMessageToPlayers();
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
        if (_gameType == GameType.Rated)
        {
            List<PlayerRatingsData>? uptoDatePerfs = null;
            if (_gameResult != GameResult.NoResult)
            {
                var (_, _, newPerfs, _) = await UpdateRatingsOnResult();
                uptoDatePerfs = newPerfs;
            }
            else
            {
                _playersRatingsAfter = _players.Select(a => _playerInfos[a].Rating.GetRatingDataOrInitial(_GetGame().GetTopLevelVariant())).ToList().Select(a => MinifiedRating((DateTime)_endTime!, a)).ToList();
                _playersRatingsDiff = [0, 0];
            }

            var stat = await UpdateStatsOnResult();

            if (uptoDatePerfs != null)
            {
                StoneTypeExt.GetValuesSafe().Zip(uptoDatePerfs.Zip(stat)).ToList().ForEach(async (a) =>
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

        await TrySaveGame();
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
            usersRatings: [.. (await Task.WhenAll(_players.Select(a => _userRatingService.GetUserRatings(a))))],
            endTime: (DateTime)_endTime!
        );

        _playersRatingsAfter = res.NewPerfs.Select(a => MinifiedRating((DateTime)_endTime!, a)).ToList();
        _playersRatingsDiff = res.RatingDiffs;

        foreach (var rating in res.UserRatings)
        {
            var newRating = await _userRatingService.SaveUserRatings(rating);
        }

        _playerInfos = _playerInfos.ToDictionary(a => a.Key, a => new PlayerInfo(
            a.Value.Id,
            a.Value.Username,
            res.UserRatings[(int)GetStoneFromPlayerId(a.Key)],
            a.Value.PlayerType
        ));

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

        // var players = GetPlayerIdSortedByColor();

        var userStat = new List<UserStatForVariant>();
        foreach (var player in _players)
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

    private async Task SendGameStartMessageToPlayers()
    {
        var game = _GetGame();
        await SendMessageToAll(new SignalRMessage(
            SignalRMessageType.gameStart,
            new GameStartMessage(game)
        ));
    }

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

    private async Task SendOpponentConnectionUpdates()
    {
        foreach (var player in _players)
        {
            var op = _players.GetOtherPlayerIdFromPlayerId(player);

            var opG = GrainFactory.GetGrain<IPlayerGrain>(op);

            var opPushG = GrainFactory.GetGrain<IPushNotifierGrain>(await opG.GetConnectionId());

            // var myPushG = GrainFactory.GetGrain<IPushNotifierGrain>(await plG.GetConnectionId());

            await SendMessageToClient(new SignalRMessage(
                SignalRMessageType.opponentConnection,
                await opPushG.GetConnectionStrength()
            ), player);
        }
    }

    // Helpers

    private bool DidStart()
    {
        return _GetGame().DidStart();
    }

    private bool DidEnd()
    {
        return _GetGame().DidEnd();
    }

    private bool BothPlayersIn()
    {
        return _players.Count == 2;
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

}