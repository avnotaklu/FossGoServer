using BadukServer.Hubs;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using Orleans.Concurrency;

namespace BadukServer.Orleans.Grains;

public class PlayerGrain : Grain, IPlayerGrain
{
    private string _connectionId;
    private bool _isInitialized = false;
    public List<string> games = [];

    public PlayerGrain()
    {
    }

    private string? _activeGameId;

    public async Task InitializePlayer(string connectionId)
    {
        _connectionId = connectionId;
        _isInitialized = true;
        var notifierGrain = GrainFactory.GetGrain<IPushNotifierGrain>(this.GetPrimaryKeyString());
        await notifierGrain.InitializeNotifier(connectionId);
    }

    // public async Task<bool> IsInitializedByOtherDevice(string connectionId)
    // {
    //     return _isInitialized && connectionId != _connectionId;
    // }

    public Task<List<string>> GetCreatedGames()
    {
        return Task.FromResult(games);
    }


    public async Task<string> CreateGame(int rows, int columns, TimeControl timeControl,  StoneSelectionType stone, string time)
    {
        var gameId = Guid.NewGuid().ToString();
        var userId = this.GetPrimaryKeyString(); // our player id

        Console.WriteLine("Creating new game: " + gameId + " By player " + userId);
        var gameGrain = GrainFactory.GetGrain<IGameGrain>(gameId); // create new game

        // add ourselves to the game
        await gameGrain.CreateGame(rows, columns, timeControl, stone, userId);

        // await _hubContext.Groups.AddToGroupAsync(_connectionId, gameId);
        var game = await gameGrain.GetGame();

        _activeGameId = gameId;
        games.Add(gameId);

        // var pairingGrain = GrainFactory.GetGrain<IPairingGrain>(0);
        // await pairingGrain.AddGame(gameId);

        return gameId;
    }

    // async public Task<PairingSummary[]> GetAvailableGames()
    // {
    //     var grain = GrainFactory.GetGrain<IPairingGrain>(0);
    //     return (await grain.GetGames()).Where(x => _activeGameId != x.GameId).ToArray();
    // }

    public async Task<string> JoinGame(string gameId, string time)
    {
        var userId = this.GetPrimaryKeyString(); // our player id

        var gameGrain = GrainFactory.GetGrain<IGameGrain>(gameId);

        Console.WriteLine("Joining game: " + gameId + " By player " + userId);

        var game = await gameGrain.JoinGame(this.GetPrimaryKeyString(), time);

        _activeGameId = gameId;

        return game.GameId;
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