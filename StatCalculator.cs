using System.Diagnostics;
using GameResultStatList = System.Collections.Generic.List<GameResultStat>;
using BadukServer;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MongoDB.Bson.Serialization.Conventions;

public interface IStatCalculator
{

    public UserStat CalculateUserStat(UserStat oldUserStats, PlayerRatings newRatings, Game game);
}
public class StatCalculator : IStatCalculator
{

    public UserStat CalculateUserStat(UserStat oldUserStats, PlayerRatings newRatings, Game game)
    {
        Debug.Assert(game.DidEnd(), "Can't calculate user stat for ongoing game");
        Debug.Assert(game.Players.Keys.Contains(oldUserStats.userId), "User not in game");

        var statKeys = game.GetRelevantVariants().Where(a => a.StatAllowed()).Select(a => a.ToKey()).ToList();

        statKeys.ForEach(key =>
        {
            UserStatForVariant? userStat;
            if (oldUserStats.stats.ContainsKey(key))
            {
                userStat = null;
            }
            else
            {
                userStat = oldUserStats.stats[key];
            }

            var newUserStat = new UserStatForVariant(
                highestRating: GetHighestRating(userStat, newRatings, game, key),
                lowestRating: GetLowestRating(userStat, newRatings, game, key),
                resultStreakData: GetResultStreakData(userStat, newRatings, game, key, oldUserStats.userId),
                playTime: GetPlayTime(userStat, game),
                greatestWins: GetGreatestWinningResult(userStat, game, oldUserStats.userId),
                greatestLosses: GetGreatestLosingResult(userStat, game, oldUserStats.userId),
                statCounts: GetUpdatedTotalStatCounts(userStat, game, oldUserStats.userId)
            );

            oldUserStats.stats[key] = newUserStat;
        });

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
            return currentWinningResult?.TryAdd(newResultStat) ?? [newResultStat];
        }

        return currentWinningResult;
    }



    private GameResultStatList? GetGreatestLosingResult(UserStatForVariant? userStat, Game game, string userId)
    {
        var currentLosingResult = userStat?.GreatestWins;

        if (game.DidILose(userId))
        {
            var newResultStat = GameResultStatExt.New(game, userId)!;
            return currentLosingResult?.TryAdd(newResultStat) ?? [newResultStat];
        }

        return currentLosingResult;
    }

    private double GetPlayTime(UserStatForVariant? userStat, Game game)
    {
        return (userStat?.PlayTimeSeconds ?? 0) + (double)game.GetRunningDurationOfGame()?.TotalSeconds!;
    }

    private double? GetHighestRating(UserStatForVariant? stat, PlayerRatings rats, Game game, string key)
    {
        var variant = VariantTypeExt.FromKey(key); ;

        if (variant.RatingAllowed())
        {
            return Math.Max(stat?.HighestRating ?? 0, rats.Ratings[key].Glicko.Rating);
        }

        return null;
    }

    private double? GetLowestRating(UserStatForVariant? stat, PlayerRatings rats, Game game, string key)
    {
        var variant = VariantTypeExt.FromKey(key); ;

        if (variant.RatingAllowed())
        {
            return Math.Min(stat?.LowestRating ?? double.MaxValue, rats.Ratings[key].Glicko.Rating);
        }

        return null;
    }

    private ResultStreakData? GetResultStreakData(UserStatForVariant? stat, PlayerRatings rats, Game game, string key, string userId)
    {
        var variant = VariantTypeExt.FromKey(key); ;
        var streakData = stat?.ResultStreakData;

        if (streakData == null)
        {
            return ResultStreakDataExt.New(game, userId);
        }

        return streakData.PutGame(game, userId);
    }

}