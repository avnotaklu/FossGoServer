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

        if (activePlayerIdx == -1)
        {
            return newTimes;
        }

        var activePlayerSnap = newTimes[activePlayerIdx];

        var activePlayerTimeLeft = activePlayerSnap.MainTimeMilliseconds - (int)(DateTime.Parse(curTime) - DateTime.Parse(activePlayerSnap.SnapshotTimestamp)).TotalMilliseconds;

        var newByoYomi = (activePlayerSnap.ByoYomisLeft ?? 0) - ((activePlayerSnap.ByoYomiActive && activePlayerTimeLeft <= 0) ? 1 : 0);

        var applicableByoYomiTime = (newByoYomi > 0) ? (byoYomiMS ?? 0) : 0;

        var applicableIncrement = activePlayerIdx != curTurn ? (timeControl.IncrementSeconds ?? 0) * 1000 : 0;

        newTimes[activePlayerIdx] = new PlayerTimeSnapshot(
                    snapshotTimestamp: curTime,
                    mainTimeMilliseconds: activePlayerTimeLeft > 0 ? activePlayerTimeLeft + applicableIncrement : applicableByoYomiTime,
                    byoYomisLeft: (int)MathF.Max(newByoYomi, 0),
                    byoYomiActive: newTimes[activePlayerIdx].ByoYomiActive || activePlayerTimeLeft <= 0,
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
                    mainTimeMilliseconds: nonTurnPlayerSnap().ByoYomiActive ? (int)byoYomiMS! : nonTurnPlayerSnap().MainTimeMilliseconds,
                    byoYomisLeft: nonTurnPlayerSnap().ByoYomisLeft,
                    byoYomiActive: nonTurnPlayerSnap().ByoYomiActive,
                    timeActive: false
                );
        return newTimes.Select((snap) => snap!).ToList();
    }



    // I just like this code, so I'm keeping it here for now

    // public async Task<List<TimeSpan>> CalculatePlayerTimesOfDiscreteSections(Game game)
    // {
    //     Debug.Assert(DidStart());
    //     var mainTimeSpan = TimeSpan.FromSeconds(game.TimeControl.MainTimeSeconds);
    //     // var mainTimeSpan =TimeSpan.FromSeconds(game.TimeControl.MainTimeSeconds);
    //     var raw_times = new List<string> { game.StartTime! };
    //     raw_times.AddRange(game.Moves.Select(move => move.Time));

    //     var times = raw_times.Select(time => DateTime.Parse(time)).ToList();

    //     // Calculate first player's duration
    //     // var firstPlayerDuration = times
    //     //     .Select((time, index) => (time, index))
    //     //     .Where(pair => pair.index % 2 == 1)
    //     //     .Aggregate(TimeSpan.Zero, (duration, pair) => duration + (pair.time - times[pair.index - 1]));

    //     var firstPlayerDuration = TimeSpan.Zero;

    //     var firstPlayerArrangedTimes = times.SkipLast(times.Count % 2);
    //     var firstPlayerTimesBeforeCorrespondingMoveMade = firstPlayerArrangedTimes.Where((time, index) => index % 2 == 0).ToList();
    //     var firstPlayerMoveMadeTimes = firstPlayerArrangedTimes.Where((time, index) => index % 2 == 1).ToList();

    //     for (int i = 0; i < MathF.Floor(firstPlayerArrangedTimes.Count() / 2); i++)
    //     {
    //         firstPlayerDuration += firstPlayerMoveMadeTimes[i] - firstPlayerTimesBeforeCorrespondingMoveMade[i];
    //     }

    //     var player0Time = mainTimeSpan - firstPlayerDuration;


    //     var secondPlayerDuration = TimeSpan.Zero;
    //     var secondPlayerArrangedTimes = times.Skip(1).SkipLast(times.Count % 2);
    //     var secondPlayerTimesBeforeCorrespondingMoveMade = secondPlayerArrangedTimes.Where((time, index) => index % 2 == 0).ToList();
    //     var secondPlayerMoveMadeTimes = secondPlayerArrangedTimes.Where((time, index) => index % 2 == 1).ToList();

    //     for (int i = 0; i < MathF.Floor(secondPlayerArrangedTimes.Count() / 2); i++)
    //     {
    //         secondPlayerDuration += secondPlayerMoveMadeTimes[i] - secondPlayerTimesBeforeCorrespondingMoveMade[i];
    //     }
    //     var player1Time = mainTimeSpan - secondPlayerDuration;


    //     var playerTimes = new List<TimeSpan> { player0Time, player1Time };

    //     // If the game has ended, apply the end time to the player with the turn
    //     if (game.GameState == GameState.Ended)
    //     {
    //         playerTimes[(int)GetStoneFromPlayerId(GetPlayerIdWithTurn()!)] -= DateTime.Parse(game.EndTime!) - times.Last();
    //     }

    //     return playerTimes;
    // }
}