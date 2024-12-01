public interface IGameTimerGrain : IGrainWithStringKey
{
    Task StartTurnTimer(int durationInMilliseconds);
    ValueTask StopTurnTimer();
    Task<bool> IsTimerActive();
}