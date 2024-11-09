public interface IGameTimerGrain : IGrainWithStringKey
{
    Task StartTurnTimer(int durationInMilliseconds);
    Task StopTurnTimer();
    Task<bool> IsTurnActive();
}