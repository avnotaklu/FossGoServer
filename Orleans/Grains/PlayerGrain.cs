
namespace BadukServer.Orleans.Grains;

public class PlayerGrain : Grain, IPlayerGrain
{
    private string? _activeGameId;
    async public Task<string> CreateGame(int rows, int columns, int timeInSeconds)
    {
        var gameId = Guid.NewGuid().ToString();
        var userId = this.GetPrimaryKeyString();  // our player id
        
        Console.WriteLine("Creating new game: " + gameId + " By player " + userId);
        var gameGrain = GrainFactory.GetGrain<IGameGrain>(gameId);  // create new game
        
        var game = await gameGrain.AddPlayerToGame(userId);

        // add ourselves to the game
        await gameGrain.CreateGame(rows, columns, timeInSeconds);
        await gameGrain.AddPlayerToGame(userId);
        _activeGameId = gameId;

        // var pairingGrain = GrainFactory.GetGrain<IPairingGrain>(0);
        // await pairingGrain.AddGame(gameId);

        return gameId;
    }

    // async public Task<PairingSummary[]> GetAvailableGames()
    // {
    //     var grain = GrainFactory.GetGrain<IPairingGrain>(0);
    //     return (await grain.GetGames()).Where(x => _activeGameId != x.GameId).ToArray();
    // }

    async public Task<string> JoinGame(string gameId)
    {
        var userId = this.GetPrimaryKeyString();  // our player id
        
        var gameGrain = GrainFactory.GetGrain<IGameGrain>(gameId);
        Console.WriteLine("Joining game: " + gameId + " By player " + userId);

        var game = await gameGrain.AddPlayerToGame(this.GetPrimaryKeyString());
        _activeGameId = gameId;

        // var pairingGrain = GrainFactory.GetGrain<IPairingGrain>(0);
        // await pairingGrain.RemoveGame(gameId);

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