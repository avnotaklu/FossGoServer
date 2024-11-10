
using BadukServer.Orleans.Grains;

public class GameTimerGrain : Grain, IGameTimerGrain
{
    private IDisposable _timerHandle;
    private bool _isTurnActive;
    private string gameId => this.GetPrimaryKeyString();

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _isTurnActive = false;
        return base.OnActivateAsync(cancellationToken);
    }

    public Task StartTurnTimer(int durationInMilliseconds)
    {
        if (_isTurnActive)
        {
            return Task.CompletedTask; // Timer already active
        }

        _isTurnActive = true;
        _timerHandle = this.RegisterGrainTimer(
            Timeout,
            this,
            TimeSpan.FromMilliseconds(durationInMilliseconds),
            TimeSpan.FromMilliseconds(-1)); // Single-use timer
        //     TurnExpired,
        //     null,
        //     TimeSpan.FromSeconds(durationInSeconds),
        //     TimeSpan.FromMilliseconds(-1)); // Single-use timer

        return Task.CompletedTask;
    }

    private async Task Timeout(object state)
    {
        _isTurnActive = false;

        var gameGrain = GrainFactory.GetGrain<IGameGrain>(gameId);
        var game = await gameGrain.TimeoutCurrentPlayer();

        foreach (var playerId in game.Players.Keys)
        {
            var pushGrain = GrainFactory.GetGrain<IPushNotifierGrain>(playerId);

            await pushGrain.SendMessage(
                new SignalRMessage(
                    SignalRMessageType.gameOver,
                    new GameOverMessage(game: game, method: GameOverMethod.Timeout)
                ),
                gameId,
                toMe: true
            );
        }

        Console.WriteLine("Game timeout");

        return;
    }

    public Task StopTurnTimer()
    {
        _timerHandle?.Dispose();
        _isTurnActive = false;
        return Task.CompletedTask;
    }

    public Task<bool> IsTurnActive() => Task.FromResult(_isTurnActive);
}