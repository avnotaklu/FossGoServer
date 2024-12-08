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


    public async Task<string> CreateGame(GameCreationDto creationData, string time)
    {
        Debug.Assert(_isInitialized, "Can't create game if not initialized");
        var gameId = ObjectId.GenerateNewId().ToString();
        var userId = this.GetPrimaryKeyString(); // our player id

        Console.WriteLine("Creating new game: " + gameId + " By player " + userId);
        var gameGrain = GrainFactory.GetGrain<IGameGrain>(gameId); // create new game


        var publicUserInfo = PlayerType == PlayerType.Guest ? await _publicUserInfoService.GetPublicUserInfoForGuest(userId) : await _publicUserInfoService.GetPublicUserInfoForNormalUser(userId);

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

    public async Task<(Game game, PlayerInfo? otherPlayerData)> JoinGame(string gameId, string time)
    {
        Debug.Assert(_isInitialized, "Can't create game if not initialized");
        _logger.LogInformation("Trying to join game");
        var userId = this.GetPrimaryKeyString(); // our player id

        _logger.LogInformation("Joining game " + gameId + " By player " + userId);

        var gameGrain = GrainFactory.GetGrain<IGameGrain>(gameId);

        var publicUserInfo = PlayerType == PlayerType.Guest ? await _publicUserInfoService.GetPublicUserInfoForGuest(userId) : await _publicUserInfoService.GetPublicUserInfoForNormalUser(userId);

        var (game, otherPlayerData, justJoined) = await gameGrain.JoinGame(publicUserInfo, time);

        await _hubService.AddToGroup(_connectionId!, gameId, CancellationToken.None);

        if (otherPlayerData != null)
        {
            var otherPlayerGrain = GrainFactory.GetGrain<IPlayerGrain>(otherPlayerData.Id);
            var otherConId = await otherPlayerGrain.GetConnectionId();

            var pushG = GrainFactory.GetGrain<IPushNotifierGrain>(otherConId);
            await pushG.SendMessageToMe(
                new SignalRMessage(
                    SignalRMessageType.gameJoin,
                    new GameJoinResult(
                        game,
                        publicUserInfo,
                        time
                    )
                )
            );
        }
        // }

        _activeGameId = gameId;

        return (game, otherPlayerData);
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