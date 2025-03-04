using System.Diagnostics;
using BadukServer.Hubs;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using Orleans.Concurrency;
using Orleans.Streams;

namespace BadukServer.Orleans.Grains;


[Reentrant]
public class PlayerGrain : Grain, IPlayerGrain
{
    // Injected
    private readonly IPlayerInfoService _publicUserInfoService;
    private readonly IGameService _gameService;
    private readonly ISignalRHubService _hubService;

    private string? _connectionId;
    private bool _isInitialized = false;
    private string PlayerId => this.GetPrimaryKeyString();
    private PlayerType PlayerType;
    private ILogger<PlayerGrain> _logger;
    public HashSet<string> activeGames = [];

    public PlayerGrain(IPlayerInfoService publicUserInfoService, IGameService gameService, ISignalRHubService hubService, ILogger<PlayerGrain> logger)
    {
        _publicUserInfoService = publicUserInfoService;
        _gameService = gameService;
        _logger = logger;
        _hubService = hubService;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var games = await _gameService.GetActiveGamesForPlayer(PlayerId);

        foreach (var game in games)
        {
            activeGames.Add(game.GameId);
        }

        await base.OnActivateAsync(cancellationToken);
    }

    public async Task ConnectPlayer(string connectionId, PlayerType playerType)
    {
        PlayerType = playerType;

        if (_connectionId != null)
        {
            var grain = GrainFactory.GetGrain<IPushNotifierGrain>(_connectionId);

            await grain.PlayerConnectionChanged();
        }

        _connectionId = connectionId;
        _isInitialized = true;
        var notifierGrain = GrainFactory.GetGrain<IPushNotifierGrain>(connectionId);

        await notifierGrain.InitializeNotifier(PlayerId, playerType);

        foreach (var gameId in activeGames)
        {
            var gameGrain = GrainFactory.GetGrain<IGameGrain>(gameId);

            var game = await gameGrain.GetGame();

            if (!game.DidEnd())
            {
                await gameGrain.PlayerRejoin(PlayerId, connectionId);
            }
        }
    }

    // public async Task<bool> IsInitializedByOtherDevice(string connectionId)
    // {
    //     return _isInitialized && connectionId != _connectionId;
    // }

    public Task<HashSet<string>> GetActiveGames()
    {
        return Task.FromResult(activeGames);
    }


    public async Task<string> CreateGame(GameCreationDto creationData, DateTime time)
    {
        Debug.Assert(_isInitialized, "Can't create game if not initialized");
        var gameId = ObjectId.GenerateNewId().ToString();
        var userId = this.GetPrimaryKeyString(); // our player id

        Console.WriteLine("Creating new game: " + gameId + " By player " + userId);
        var gameGrain = GrainFactory.GetGrain<IGameGrain>(gameId); // create new game


        var publicUserInfo = await _publicUserInfoService.GetPublicUserInfoForPlayer(userId, PlayerType);

        if (publicUserInfo == null)
        {
            throw new Exception("Player not found");
        }

        var gameType = PlayerType.GetGameType(creationData.RankedOrCasual);

        // add ourselves to the game
        await gameGrain.CreateGame(creationData.ToData(), publicUserInfo, gameType, connectionId: _connectionId!);

        activeGames.Add(gameId);

        return gameId;
    }

    // async public Task<PairingSummary[]> GetAvailableGames()
    // {
    //     var grain = GrainFactory.GetGrain<IPairingGrain>(0);
    //     return (await grain.GetGames()).Where(x => _activeGameId != x.GameId).ToArray();
    // }

    public async Task<(Game game, DateTime? joinTime, PlayerInfo? otherPlayerData)> JoinGame(string gameId)
    {
        Debug.Assert(_isInitialized, "Can't create game if not initialized");
        _logger.LogInformation("Trying to join game");
        var userId = this.GetPrimaryKeyString(); // our player id

        _logger.LogInformation("Joining game " + gameId + " By player " + userId);

        var gameGrain = GrainFactory.GetGrain<IGameGrain>(gameId);

        var publicUserInfo = await _publicUserInfoService.GetPublicUserInfoForPlayer(userId, PlayerType);

        if (publicUserInfo == null)
        {
            throw new Exception("Player not found");
        }

        var game = await gameGrain.GetGame();

        try
        {
            var (gameAfterJoin, joinTime, justJoined) = await gameGrain.JoinGame(publicUserInfo, connectionId: _connectionId!);

            var otherPlayerData = await _publicUserInfoService.GetPublicUserInfoForPlayer(gameAfterJoin.Players.GetOtherPlayerIdFromPlayerId(userId)!, PlayerType);

            // REVIEW: assuming other player same type as me

            if (otherPlayerData == null)
            {
                throw new Exception("Other player not found");
            }
            if (justJoined)
            {
                await InformMyJoin(gameAfterJoin, new List<PlayerInfo> { publicUserInfo, otherPlayerData }, joinTime, PlayerJoinMethod.Join);
                activeGames.Add(gameId);
            }

            return (gameAfterJoin, joinTime, otherPlayerData);
        }
        catch (InvalidOperationException)
        {
            if (!game.DidEnd() && game.GameCreator == userId)
            {
                return (game, null, null);
            }
            else
            {
                throw;
            }
        }
    }

    public async Task InformMyJoin(Game game, List<PlayerInfo> players, DateTime time, PlayerJoinMethod joinMethod)
    {
        var userId = this.GetPrimaryKeyString(); // our player id

        Debug.Assert(players.Count == 2);
        Debug.Assert(players.Any(a => a.Id == userId));

        var otherPlayerData = players.FirstOrDefault(p => p.Id != userId);
        var publicUserInfo = players.FirstOrDefault(p => p.Id == userId);

        if (otherPlayerData != null)
        {
            var otherPlayerGrain = GrainFactory.GetGrain<IPlayerGrain>(otherPlayerData.Id);
            var otherConId = await otherPlayerGrain.GetConnectionId();

            var pushG = GrainFactory.GetGrain<IPushNotifierGrain>(otherConId);
            await pushG.SendMessageToMe(
                new SignalRMessage(
                    joinMethod.SignalRMethod(),
                    new GameJoinMessage(
                        game,
                        publicUserInfo,
                        time
                    )
                )
            );
        }
    }

    public Task AddActiveGame(string gameId)
    {
        activeGames.Add(gameId);
        return Task.CompletedTask;
    }

    public Task<string?> GetConnectionId()
    {
        if (!_isInitialized)
        {
            return Task.FromResult<string?>(null);
        }
        else
        {
            return Task.FromResult(_connectionId);
        }
    }

    public Task LeaveGame(string gameId)
    {
        activeGames.Remove(gameId);
        return Task.CompletedTask;
    }

    public Task<bool> IsActive()
    {
        return Task.FromResult(_isInitialized);
    }
}

public static class PlayerJoinMethodExt
{
    public static string SignalRMethod(this PlayerJoinMethod method)
    {
        return method switch
        {
            PlayerJoinMethod.Join => SignalRMessageType.gameJoin,
            PlayerJoinMethod.Match => SignalRMessageType.matchFound,
            _ => throw new Exception("Invalid join method")
        };
    }
}

[GenerateSerializer]
public enum PlayerJoinMethod
{
    Join,
    Match
}