using GameResultStatList = System.Collections.Generic.List<GameResultStat>;
using Glicko2;
using MongoDB.Bson.Serialization.Attributes;
using BadukServer;
using System.Diagnostics;
using System.CodeDom;
using MongoDB.Driver;

public static class UserStatFieldNames
{
    public const string userId = "uid";
    public const string stats = "s";

    public const string HighestRating = "hr";
    public const string LowestRating = "lr";

    public const string ResultStreakData = "res_s";
    public const string PlayTimeSeconds = "pts";
    public const string GreatestWins = "gw";
    public const string GreatestLosses = "gl";
    public const string StatCounts = "c";
    public const string LastTweleveCounts = "c"; // Unused
    public const string LastTweleveRatingDiff = "c";

    public const string WinningStreaks = "ws";
    public const string LosingStreaks = "ls";

    public const string GreatestStreak = "gs";
    public const string CurrentStreak = "cs";

    public const string StreakLength = "sl";
    public const string StreakStartingGameId = "ssgid";
    public const string StreakEndingGameId = "segid";
    public const string StreakFrom = "sf";
    public const string StreakTo = "st";

    public const string Total = "tot";
    public const string Wins = "wn";
    public const string Losses = "lss";
    public const string Disconnects = "dc";
    public const string Draws = "dw";

    public const string OpponentRating = "opR";
    public const string OpponentId = "opId";
    public const string ResultAt = "at";
    public const string GameId = "gid";
}

[Immutable, GenerateSerializer]
[Alias("UserStat")]
public class UserStat
{
    [Id(0)]
    [BsonElement(UserStatFieldNames.userId)]
    public string userId { get; set; }

    [Id(1)]
    [BsonElement(UserStatFieldNames.stats)]
    public Dictionary<string, UserStatForVariant> stats { get; set; }


    public UserStat(string userId, Dictionary<string, UserStatForVariant> stats)
    {
        this.userId = userId;
        this.stats = stats;
    }
}


public static class UserStatForVariantExt
{
}


[Immutable, GenerateSerializer]
[Alias("UserStatData")]
public class UserStatForVariant
{
    [Id(0)]
    [BsonElement(UserStatFieldNames.HighestRating)]
    public double? HighestRating { get; set; }

    [Id(1)]
    [BsonElement(UserStatFieldNames.LowestRating)]
    public double? LowestRating { get; set; }

    [Id(2)]
    [BsonElement(UserStatFieldNames.ResultStreakData)]
    public ResultStreakData? ResultStreakData { get; set; }

    [Id(3)]
    [BsonElement(UserStatFieldNames.PlayTimeSeconds)]
    public double PlayTimeSeconds { get; set; }

    [Id(4)]
    [BsonElement(UserStatFieldNames.GreatestWins)]
    public GameResultStatList? GreatestWins { get; set; }

    [Id(5)]
    [BsonElement(UserStatFieldNames.StatCounts)]
    public GameStatCounts StatCounts { get; set; }



    // Not sure whether i need all this
    // The losses are not really useful, but the wins are
    // [Id(5)]
    // [BsonElement(UserStatFieldNames.GreatestLosses)]
    // public GameResultStatList? GreatestLosses { get; set; }




    // Not sure whether i need all this
    // Another option is i just store two fields for 12 games - 
    //  * non_decisive_count = games without winner in last 12 games,
    //       if tot < 12 we pad this number up to fill 12 games
    //  * decisive_res = number which keeps running score, +1 for win -1 for loss
    //       -2 => you won lost two more games than won,
    //             keeping non_decisive_count into acc. we can calc the win/loss counts

    // [Id(7)]
    // [BsonElement(UserStatFieldNames.LastTweleveCounts)]
    // public GameStatCounts LastTweleveCounts { get; set; }


    // Not sure whether i need this field as well, and also
    // I Should store a list of last 12 results to accurately keep a rating diff

    // [Id(7)]
    // [BsonElement(UserStatFieldNames.LastTweleveRatingDiff)]
    // public double LastTweleveRatingDiff { get; set; }


    public UserStatForVariant(double? highestRating, double? lowestRating, ResultStreakData? resultStreakData, double playTime, GameResultStatList? greatestWins, GameStatCounts statCounts)
    {
        HighestRating = highestRating;
        LowestRating = lowestRating;
        ResultStreakData = resultStreakData;
        PlayTimeSeconds = playTime;
        GreatestWins = greatestWins;
        StatCounts = statCounts;
    }
}

public static class ResultStreakDataExt
{
    public static ResultStreakData? New(Game game, string userId)
    {
        var result = game.MyResult(userId);

        if (result == null)
        {
            return null;
        }

        if (result == 1)
        {
            return new ResultStreakData(StreakDataExt.New(game), null);
        }

        if (result == -1)
        {
            return new ResultStreakData(null, StreakDataExt.New(game));
        }

        return null;
    }


    public static ResultStreakData? PutGame(this ResultStreakData me, Game game, string userId)
    {
        var result = game.MyResult(userId);

        if (result == null)
        {
            return null;
        }

        var winningStreaks = me.WinningStreaks;
        var losingStreaks = me.LosingStreaks;

        if (result == 1)
        {
            return new ResultStreakData(winningStreaks?.Increment(game) ?? StreakDataExt.New(game), losingStreaks?.Break());
        }
        if (result == -1)
        {
            return new ResultStreakData(winningStreaks?.Break(), losingStreaks?.Increment(game) ?? StreakDataExt.New(game));
        }
        if (result == -1 && winningStreaks == null && losingStreaks == null)
        {
            return null;
        }

        return me;
    }
}


[Immutable, GenerateSerializer]
[Alias("ResultStreakData")]
public class ResultStreakData
{
    [Id(0)]
    [BsonElement(UserStatFieldNames.WinningStreaks)]
    public StreakData? WinningStreaks { get; set; }

    [Id(1)]
    [BsonElement(UserStatFieldNames.LosingStreaks)]
    public StreakData? LosingStreaks { get; set; }

    // make constructor

    public ResultStreakData(StreakData? winningStreaks, StreakData? losingStreaks)
    {
        Debug.Assert(winningStreaks != null || losingStreaks != null);
        WinningStreaks = winningStreaks;
        LosingStreaks = losingStreaks;
    }

}

public static class StreakDataExt
{
    public static StreakData? New(Game game)
    {
        if (!game.DidEnd())
        {
            return null;
        }

        var streak = StreakExt.New(game);

        return new StreakData(streak, streak);
    }
    public static StreakData? Increment(this StreakData me, Game game)
    {
        if (!game.DidEnd())
        {
            return null;
        }

        var streak = me.CurrentStreak?.Increment(game);

        if (streak == null)
        {
            return null;
        }

        var greatest = (me.GreatestStreak ?? streak).StreakLength > streak.StreakLength ? me.GreatestStreak : streak!;

        return new StreakData(greatest, streak);
    }

    public static StreakData? Break(this StreakData me)
    {
        return new StreakData(me.GreatestStreak, null);
    }
}

[Immutable, GenerateSerializer]
[Alias("StreakData")]
public class StreakData
{
    [Id(0)]
    [BsonElement(UserStatFieldNames.GreatestStreak)]
    public Streak? GreatestStreak { get; set; }

    [Id(1)]
    [BsonElement(UserStatFieldNames.CurrentStreak)]
    public Streak? CurrentStreak { get; set; }


    public StreakData(Streak? greatestStreak, Streak? currentStreak)
    {
        Debug.Assert(greatestStreak != null || currentStreak != null);

        GreatestStreak = greatestStreak;
        CurrentStreak = currentStreak;
    }
}

public static class StreakExt
{
    public static Streak? New(Game game)
    {
        if (!game.DidEnd())
        {
            return null;
        }

        var streak = new Streak(1, game.EndTime!.DeserializedDate(), game.EndTime!.DeserializedDate(), game.GameId, game.GameId);

        return streak;
    }

    public static Streak? Increment(this Streak me, Game game)
    {
        if (!game.DidEnd())
        {
            return null;
        }

        return new Streak(me.StreakLength + 1, me.StreakFrom, game.EndTime!.DeserializedDate(), me.StartingGameId, game.GameId);
    }
}

[Immutable, GenerateSerializer]
[Alias("Streak")]
public class Streak
{
    [Id(0)]
    [BsonElement(UserStatFieldNames.StreakLength)]
    public int StreakLength { get; set; }

    [Id(1)]
    [BsonElement(UserStatFieldNames.StreakFrom)]
    public DateTime StreakFrom { get; set; }

    [Id(3)]
    [BsonElement(UserStatFieldNames.StreakStartingGameId)]
    public string StartingGameId { get; set; }

    [Id(4)]
    [BsonElement(UserStatFieldNames.StreakEndingGameId)]
    public string EndingGameId { get; set; }


    [Id(5)]
    [BsonElement(UserStatFieldNames.StreakTo)]
    public DateTime StreakTo { get; set; }



    public Streak(int streakLength, DateTime streakFrom, DateTime streakTo, string startingGameId, string endingGameId)
    {
        Debug.Assert(streakLength > 0);

        StreakLength = streakLength;
        StreakFrom = streakFrom;
        StreakTo = streakTo;
        StartingGameId = startingGameId;
        EndingGameId = endingGameId;
    }
}


[Immutable, GenerateSerializer]
[Alias("GameStatCounts")]
public class GameStatCounts
{
    [Id(0)]
    [BsonElement(UserStatFieldNames.Total)]
    public int Total { get; set; }

    [Id(1)]
    [BsonElement(UserStatFieldNames.Wins)]
    public int Wins { get; set; }

    [Id(2)]
    [BsonElement(UserStatFieldNames.Losses)]
    public int Losses { get; set; }

    [Id(3)]
    [BsonElement(UserStatFieldNames.Disconnects)]
    public int Disconnects { get; set; }
    [Id(4)]
    [BsonElement(UserStatFieldNames.Wins)]
    public int Draws { get; set; }

    public GameStatCounts(int total, int wins, int losses, int disconnects, int draws)
    {
        Total = total;
        Wins = wins;
        Losses = losses;
        Disconnects = disconnects;
        Draws = draws;
    }
}


public static class GameResultStatListExt
{
    private static readonly int MaxGameResultStats = 5;


    public static GameResultStatList TryAdd(this GameResultStatList list, GameResultStat item)
    {
        list.Add(item);
        list.Sort((a, b) => b.OpponentRating.CompareTo(a.OpponentRating));

        if (list.Count > MaxGameResultStats)
        {
            list.RemoveAt(MaxGameResultStats);
        }
        return list;
    }


    public static GameResultStat? GetMax(this GameResultStatList list)
    {
        return list.MaxBy((a) => a.OpponentRating);
    }
    public static GameResultStat? GetMin(this GameResultStatList list)
    {
        return list.MinBy((a) => a.OpponentRating);
    }
}


public static class GameResultStatExt
{
    public static GameResultStat? New(Game game, string userId)
    {
        if (!game.DidEnd())
        {
            return null;
        }

        var minRating = MinimalRatingExt.FromString(game.PlayersRatingsDiff[(int)game.Players.GetOtherStoneFromPlayerId(userId)!]);

        if (minRating == null)
        {
            return null;
        }

        if (minRating!.Provisional)
        {
            return null;
        }

        return new GameResultStat(
            gameId: game.GameId,
            opponentRating: minRating.Rating,
            opponentId: game.Players.GetOtherPlayerIdFromPlayerId(userId)!,
            resultAt: game.EndTime!.DeserializedDate()
        );
    }
}

[Immutable, GenerateSerializer]
[Alias("GameResultStat")]

public class GameResultStat // : IComparable<GameResultStat>
{
    [Id(0)]
    [BsonElement(UserStatFieldNames.OpponentRating)]
    public int OpponentRating { get; set; }

    [Id(1)]
    [BsonElement(UserStatFieldNames.OpponentId)]
    public string OpponentId { get; set; }

    [Id(2)]
    [BsonElement(UserStatFieldNames.ResultAt)]
    public DateTime ResultAt { get; set; }

    [Id(3)]
    [BsonElement(UserStatFieldNames.GameId)]
    public string GameId { get; set; }

    public GameResultStat(int opponentRating, string opponentId, DateTime resultAt, string gameId)
    {
        OpponentRating = opponentRating;
        OpponentId = opponentId;
        ResultAt = resultAt;
        GameId = gameId;
    }
}