using BadukServer;


public interface ITimeCalculator
{
    public List<PlayerTimeSnapshot> RecalculateTurnPlayerTimeSnapshots(StoneType curTurnPlayer, List<PlayerTimeSnapshot> playerTimes, TimeControl timeControl, string curTime);
}


public class TimeCalculator : ITimeCalculator
{
    public List<PlayerTimeSnapshot> RecalculateTurnPlayerTimeSnapshots(StoneType curTurnPlayer, List<PlayerTimeSnapshot> playerTimes, TimeControl timeControl, string curTime)
    {
        var curTurn = (int)curTurnPlayer;

        List<PlayerTimeSnapshot> newTimes = [.. playerTimes];

        PlayerTimeSnapshot turnPlayerSnap() => newTimes[curTurn];
        PlayerTimeSnapshot nonTurnPlayerSnap() => newTimes[1 - curTurn];

        var byoYomiMS = timeControl.ByoYomiTime?.ByoYomiSeconds * 1000;

        var activePlayerIdx = newTimes.FindIndex((snap) => snap.TimeActive);
        var activePlayerSnap = newTimes[activePlayerIdx];

        var activePlayerTimeLeft = activePlayerSnap.MainTimeMilliseconds - (int)(DateTime.Parse(curTime) - DateTime.Parse(activePlayerSnap.SnapshotTimestamp)).TotalMilliseconds;

        var newByoYomi = activePlayerSnap.ByoYomisLeft - ((activePlayerSnap.ByoYomiActive && activePlayerTimeLeft <= 0) ? 1 : 0);

        var applicableByoYomiTime = (newByoYomi > 0) ? (byoYomiMS ?? 0) : 0;

        var applicableIncrement = activePlayerIdx != curTurn ? (timeControl.IncrementSeconds ?? 0) * 1000 : 0;

        newTimes[activePlayerIdx] = new PlayerTimeSnapshot(
                    snapshotTimestamp: curTime,
                    mainTimeMilliseconds: activePlayerTimeLeft > 0 ? activePlayerTimeLeft + applicableIncrement : applicableByoYomiTime,
                    byoYomisLeft: newByoYomi,
                    byoYomiActive: activePlayerTimeLeft <= 0,
                    timeActive: newTimes[activePlayerIdx].TimeActive
                );

        newTimes[curTurn] = new PlayerTimeSnapshot(
                            snapshotTimestamp: curTime,
                            mainTimeMilliseconds: turnPlayerSnap().MainTimeMilliseconds,
                            byoYomisLeft: turnPlayerSnap().ByoYomisLeft,
                            byoYomiActive: turnPlayerSnap().ByoYomiActive,
                            timeActive: true
                        );

        newTimes[1 - curTurn] = new PlayerTimeSnapshot(
                    snapshotTimestamp: curTime,
                    mainTimeMilliseconds: nonTurnPlayerSnap().MainTimeMilliseconds,
                    // newTimes[1 - curTurn].ByoYomiActive ? (byoYomiMS ?? 0) : nonTurnPlayerSnap().MainTimeMilliseconds,
                    byoYomisLeft: nonTurnPlayerSnap().ByoYomisLeft,
                    byoYomiActive: false,
                    timeActive: false
                );
        return newTimes.Select((snap) => snap!).ToList();
    }
}