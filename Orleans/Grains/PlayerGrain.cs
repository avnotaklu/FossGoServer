
namespace BadukServer.Orleans.Grains;

public class PlayerGrain : Grain, IPlayerGrain
{
    private string? _activeGameId;
    async public Task<string> CreateGame(int rows, int columns, int timeInSeconds, string userId)
    {
        var gameId = Guid.NewGuid().ToString();
        var gameGrain = GrainFactory.GetGrain<IGameGrain>(gameId);  // create new game
        
        var game = await gameGrain.AddPlayerToGame(userId);

        // add ourselves to the game
        var playerId = this.GetPrimaryKeyString();  // our player id
        await gameGrain.CreateGame(rows, columns, timeInSeconds);
        await gameGrain.AddPlayerToGame(playerId);
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
        var gameGrain = GrainFactory.GetGrain<IGameGrain>(gameId);

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