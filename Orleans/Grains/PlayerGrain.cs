using BadukServer.Hubs;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using Orleans.Concurrency;

namespace BadukServer.Orleans.Grains;

public static class PlayerTypeExt
{
    public static PlayerType FromString(string type)
    {
        return type switch
        {
            "normal_user" => PlayerType.Normal,
            "guest_user" => PlayerType.Guest,
            _ => throw new Exception("Invalid player type")
        };
    }
    public static string ToTypeString(this PlayerType type)
    {
        return type switch
        {
            PlayerType.Normal => "normal_user",
            PlayerType.Guest => "guest_user",
            _ => throw new Exception("Invalid player type")
        };
    }

    public static GameType GetGameType(this PlayerType type, RankedOrCasual rankedOrCasual)
    {
        return type switch
        {
            PlayerType.Normal => rankedOrCasual switch
            {
                RankedOrCasual.Rated => GameType.Rated,
                RankedOrCasual.Casual => GameType.Casual,
                _ => throw new Exception("Invalid game type")
            },
            PlayerType.Guest => GameType.Anonymous,
            _ => throw new Exception("Invalid player type")
        };
    }
}

public class PlayerGrain : Grain, IPlayerGrain
{
    // Injected
    private readonly IPlayerInfoService _publicUserInfoService;
    private readonly ISignalRHubService _hubService;

    private string _connectionId = null!;
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
        var gameId = ObjectId.GenerateNewId().ToString();
        var userId = this.GetPrimaryKeyString(); // our player id

        Console.WriteLine("Creating new game: " + gameId + " By player " + userId);
        var gameGrain = GrainFactory.GetGrain<IGameGrain>(gameId); // create new game


        var publicUserInfo = PlayerType == PlayerType.Guest ? await _publicUserInfoService.GetPublicUserInfoForGuest(userId) : await _publicUserInfoService.GetPublicUserInfoForNormalUser(userId);

        var gameType = PlayerType.GetGameType(creationData.RankedOrCasual);

        // add ourselves to the game
        await gameGrain.CreateGame(creationData.ToData(), publicUserInfo, gameType);

        await _hubService.AddToGroup(_connectionId, gameId, CancellationToken.None);

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
        var userId = this.GetPrimaryKeyString(); // our player id

        _logger.LogInformation("Joining game " + gameId + " By player " + userId);

        var gameGrain = GrainFactory.GetGrain<IGameGrain>(gameId);

        var publicUserInfo = PlayerType == PlayerType.Guest ? await _publicUserInfoService.GetPublicUserInfoForGuest(userId) : await _publicUserInfoService.GetPublicUserInfoForNormalUser(userId);

        var (game, otherPlayerData, justJoined) = await gameGrain.JoinGame(publicUserInfo, time);

        await _hubService.AddToGroup(_connectionId, gameId, CancellationToken.None);

        if (otherPlayerData != null)
        {
            var otherPlayerGrain = GrainFactory.GetGrain<IPlayerGrain>(otherPlayerData.Id);
            var otherConId = await otherPlayerGrain.GetConnectionId();

            var pushG = GrainFactory.GetGrain<IPushNotifierGrain>(otherConId);
            pushG.SendMessageToMe(
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

    public Task<string> GetConnectionId()
    {
        return Task.FromResult(_connectionId);
    }

    public Task LeaveGame(string gameId)
    {
        // manage game list
        _activeGameId = null;

        // manage running total
        // _ = outcome switch {
        //     GameOutcome.Win => _wins++,
        //     GameOutcome.Lose => _loses++,
        //     _ => 0
        // };

        return Task.CompletedTask;
    }
}