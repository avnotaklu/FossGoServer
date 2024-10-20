
public class PlayerGrain : Grain, IPlayerGrain
{
    private Guid? _activeGameId;
    async public Task<Guid> CreateGame()
    {
        var gameId = Guid.NewGuid();
        var gameGrain = GrainFactory.GetGrain<IGameGrain>(gameId);  // create new game

        // add ourselves to the game
        var playerId = this.GetPrimaryKey();  // our player id
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

    async public Task<GameState> JoinGame(Guid gameId)
    {
        var gameGrain = GrainFactory.GetGrain<IGameGrain>(gameId);

        var state = await gameGrain.AddPlayerToGame(this.GetPrimaryKey());
        _activeGameId = gameId;

        // var pairingGrain = GrainFactory.GetGrain<IPairingGrain>(0);
        // await pairingGrain.RemoveGame(gameId);

        return state;

    }

    public Task LeaveGame(Guid gameId)
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