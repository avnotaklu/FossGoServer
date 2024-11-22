
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
        var res = await gameGrain.TimeoutCurrentPlayer();
        await StopTurnTimer();

        if (res != null && res.MainTimeMilliseconds > 0)
        {
            await StartTurnTimer(res.MainTimeMilliseconds);
        }

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