using Orleans.Concurrency;

namespace BadukServer.Orleans.Grains;

[Reentrant]
public class GameTimerGrain : Grain, IGameTimerGrain
{
    private IDisposable _timerHandle = null!;
    private bool _isTimerActive;
    private string gameId => this.GetPrimaryKeyString();

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _isTimerActive = false;
        return base.OnActivateAsync(cancellationToken);
    }

    public Task StartTurnTimer(int durationInMilliseconds)
    {
        if (_isTimerActive)
        {
            return Task.CompletedTask; // Timer already active
        }

        _isTimerActive = true;
        _timerHandle = this.RegisterGrainTimer(
            Timeout,
            this,
            TimeSpan.FromMilliseconds(durationInMilliseconds),
            TimeSpan.FromMilliseconds(-1));

        return Task.CompletedTask;
    }

    private async Task Timeout(object state)
    {
        _isTimerActive = false;

        var gameGrain = GrainFactory.GetGrain<IGameGrain>(gameId);
        await StopTurnTimer();
        var res = await gameGrain.TimeoutCurrentPlayer();

        if (res != null && res.MainTimeMilliseconds > 0 && res.TimeActive)
        {
            await StartTurnTimer(res.MainTimeMilliseconds);
        }
        // return;
    }

    public ValueTask StopTurnTimer()
    {
        if (_isTimerActive)
        {
            _timerHandle?.Dispose();
            _isTimerActive = false;
        }
        return new();
    }

    public Task<bool> IsTimerActive() => Task.FromResult(_isTimerActive);
}