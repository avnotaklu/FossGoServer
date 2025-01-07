using System.Diagnostics;
using GameResultStatList = System.Collections.Generic.List<GameResultStat>;
using BadukServer;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MongoDB.Bson.Serialization.Conventions;

public interface IStatCalculator
{
    public UserStat CalculateUserStat(UserStat oldUserStats, Game game);
}
public class StatCalculator : IStatCalculator
{

    public UserStat CalculateUserStat(UserStat oldUserStats, Game game)
    {
        Debug.Assert(game.DidEnd(), "Can't calculate user stat for ongoing game");
        Debug.Assert(game.Players.Contains(oldUserStats.UserId), "User not in game");

        var key = game.GetTopLevelVariant().ToKey();

        oldUserStats.Stats.TryGetValue(key, out UserStatForVariant? userStat);

        var uid = oldUserStats.UserId;
        var newUserStat = new UserStatForVariant(
            highestRating: GetHighestRating(userStat, game, key, uid),
            lowestRating: GetLowestRating(userStat, game, key, uid),
            resultStreakData: GetResultStreakData(userStat, game, key, uid),
            playTime: GetPlayTime(userStat, game),
            greatestWins: GetGreatestWinningResult(userStat, game, uid),
            statCounts: GetUpdatedTotalStatCounts(userStat, game, uid)
        );

        oldUserStats.Stats[key] = newUserStat;
        // });

        return oldUserStats;
    }

    private GameStatCounts GetUpdatedTotalStatCounts(UserStatForVariant? userStat, Game game, string userId)
    {
        return new GameStatCounts(
            total: (userStat?.StatCounts.Total ?? 0) + 1,
            wins: (userStat?.StatCounts.Wins ?? 0) + (game.DidIWin(userId) ? 1 : 0),
            losses: (userStat?.StatCounts.Losses ?? 0) + (game.DidILose(userId) ? 1 : 0),
            draws: (userStat?.StatCounts.Draws ?? 0) + (game.DidIDraw(userId) ? 1 : 0),
            disconnects: userStat?.StatCounts.Disconnects ?? 0 // IDK how to quantify this
        );
    }

    private GameResultStatList? GetGreatestWinningResult(UserStatForVariant? userStat, Game game, string userId)
    {
        var currentWinningResult = userStat?.GreatestWins;

        if (game.DidIWin(userId))
        {
            var newResultStat = GameResultStatExt.New(game, userId)!;
            if (newResultStat == null) return currentWinningResult;

            return currentWinningResult?.TryAdd(newResultStat) ?? [newResultStat];
        }

        return currentWinningResult;
    }


    // Greatest losses is not implemented in the right now

    // private GameResultStatList? GetGreatestLosingResult(UserStatForVariant? userStat, Game game, string userId)
    // {
    //     var currentLosingResult = userStat?.GreatestWins;

    //     if (game.DidILose(userId))
    //     {
    //         var newResultStat = GameResultStatExt.New(game, userId)!;
    //         return currentLosingResult?.TryAdd(newResultStat) ?? [newResultStat];
    //     }

    //     return currentLosingResult;
    // }

    private double GetPlayTime(UserStatForVariant? userStat, Game game)
    {
        return (userStat?.PlayTimeSeconds ?? 0) + (double)game.GetRunningDurationOfGame()?.TotalSeconds!;
    }

    private double? GetHighestRating(UserStatForVariant? stat, Game game, string key, string userId)
    {
        var variant = VariantTypeExt.FromKey(key); ;
        var rating = MinimalRatingExt.FromString(game.PlayersRatingsAfter[(int)game.Players.GetStoneFromPlayerId(userId)!]);

        if (rating == null || rating.Provisional)
        {
            return stat?.HighestRating;
        }

        if (variant.RatingAllowed())
        {
            return Math.Max(stat?.HighestRating ?? 0, rating.Rating);
        }

        return null;
    }

    private double? GetLowestRating(UserStatForVariant? stat, Game game, string key, string userId)
    {
        var variant = VariantTypeExt.FromKey(key); ;
        var rating = MinimalRatingExt.FromString(game.PlayersRatingsAfter[(int)game.Players.GetStoneFromPlayerId(userId)!]);

        if (rating == null || rating.Provisional)
        {
            return stat?.LowestRating;
        }

        if (variant.RatingAllowed())
        {
            return Math.Min(stat?.LowestRating ?? double.MaxValue, rating.Rating);
        }

        return null;
    }

    private ResultStreakData? GetResultStreakData(UserStatForVariant? stat, Game game, string key, string userId)
    {
        var streakData = stat?.ResultStreakData;

        if (streakData == null)
        {
            return ResultStreakDataExt.New(game, userId);
        }

        return streakData.PutGame(game, userId);
    }

}