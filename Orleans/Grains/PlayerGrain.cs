using System.Diagnostics;
using BadukServer.Hubs;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using Orleans.Concurrency;

namespace BadukServer.Orleans.Grains;

public class PlayerGrain : Grain, IPlayerGrain
{
    // Injected
    private readonly IPlayerInfoService _publicUserInfoService;
    private readonly ISignalRHubService _hubService;

    private string? _connectionId;
    private bool _isInitialized = false;
    private string PlayerId => this.GetPrimaryKeyString();
    private PlayerType PlayerType;
    private ILogger<PlayerGrain> _logger;
    public List<string> games = [];

    public PlayerGrain(IPlayerInfoService publicUserInfoService, ISignalRHubService hubService, ILogger<PlayerGrain> logger)
    {
        _publicUserInfoService = publicUserInfoService;
        _logger = logger;
        _hubService = hubService;
    }


    private string? _activeGameId;

    public async Task InitializePlayer(string connectionId, PlayerType playerType)
    {
        PlayerType = playerType;
        _connectionId = connectionId;
        _isInitialized = true;
        var notifierGrain = GrainFactory.GetGrain<IPushNotifierGrain>(connectionId);

        await notifierGrain.InitializeNotifier(PlayerId, playerType);
    }

    // public async Task<bool> IsInitializedByOtherDevice(string connectionId)
    // {
    //     return _isInitialized && connectionId != _connectionId;
    // }

    public Task<List<string>> GetCreatedGames()
    {
        return Task.FromResult(games);
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
        await gameGrain.CreateGame(creationData.ToData(), publicUserInfo, gameType);

        await _hubService.AddToGroup(_connectionId!, gameId, CancellationToken.None);

        _activeGameId = gameId;
        games.Add(gameId);

        return gameId;
    }

    // async public Task<PairingSummary[]> GetAvailableGames()
    // {
    //     var grain = GrainFactory.GetGrain<IPairingGrain>(0);
    //     return (await grain.GetGames()).Where(x => _activeGameId != x.GameId).ToArray();
    // }

    public async Task<(Game game, PlayerInfo? otherPlayerData)> JoinGame(string gameId, DateTime time)
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

        var (game, otherPlayerData, justJoined) = await gameGrain.JoinGame(publicUserInfo, time);

        if (justJoined && otherPlayerData != null)
        {
            await InformMyJoin(game, new List<PlayerInfo> { publicUserInfo, otherPlayerData }, time, PlayerJoinMethod.Join);
        }

        _activeGameId = gameId;

        return (game, otherPlayerData);
    }

    public async Task InformMyJoin(Game game, List<PlayerInfo> players, DateTime time, PlayerJoinMethod joinMethod)
    {
        var userId = this.GetPrimaryKeyString(); // our player id

        Debug.Assert(players.Count == 2);
        Debug.Assert(players.Any(a => a.Id == userId));

        var otherPlayerData = players.FirstOrDefault(p => p.Id != userId);
        var publicUserInfo = players.FirstOrDefault(p => p.Id == userId);

        await _hubService.AddToGroup(_connectionId!, game.GameId, CancellationToken.None);

        if (otherPlayerData != null)
        {
            var otherPlayerGrain = GrainFactory.GetGrain<IPlayerGrain>(otherPlayerData.Id);
            var otherConId = await otherPlayerGrain.GetConnectionId();

            var pushG = GrainFactory.GetGrain<IPushNotifierGrain>(otherConId);
            await pushG.SendMessageToMe(
                new SignalRMessage(
                    joinMethod.SignalRMethod(),
                    new GameJoinResult(
                        game,
                        publicUserInfo,
                        time
                    )
                )
            );
        }
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
        _activeGameId = null;

        return Task.CompletedTask;
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