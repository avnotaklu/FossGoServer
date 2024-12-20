
using System.CodeDom;
using System.Diagnostics;
using BadukServer;
using Glicko2;
using Google.Apis.Util;
using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
using Moq;

// public class RatingPlayer
// {

// }


// public class GlickoSettings
// {
//     public Rating StartRating;
//     public double VolatilityChange;
//     public double ConvergenceTolerance;
//     public TimeSpan RatingPeriodDuration;

//     public GlickoSettings(Rating startRating, double volatilityChange, double convergenceTolerance, TimeSpan ratingPeriodDuration)
//     {
//         StartRating = startRating;
//         VolatilityChange = volatilityChange;
//         ConvergenceTolerance = convergenceTolerance;
//         RatingPeriodDuration = ratingPeriodDuration;
//     }
// }

public interface IRatingEngine
{
    public (List<int> RatingDiffs, List<PlayerRatingsData> PrevPerfs, List<PlayerRatingsData> NewPerfs, List<PlayerRatings> UserRatings) CalculateRatingAndPerfsAsync(GameResult gameResult, ConcreteGameVariant gameVariant, List<PlayerRatings> usersRatings, DateTime endTime);

    public double PreviewDeviation(PlayerRatingsData data, DateTime ratingPeriodEndDate, bool reverse);

    public bool IsRatingProvisional(PlayerRatingsData rating, DateTime ratingPeriodEndDate);
}

public class RatingEngine : IRatingEngine
{
    private const double ProvisionalDeviation = 110;
    // private readonly List<Rating> _players = [];
    // private readonly List<Result> _results = [];
    private readonly RatingPeriodResults _result;
    // private DateTime _lastRatingPeriodStart;
    // private GlickoSettings _settings;
    private RatingCalculator _calculator;
    private ILogger<RatingEngine> _logger;

    public RatingEngine(ILogger<RatingEngine> logger)
    {
        // _lastRatingPeriodStart = DateTime.Now;
        // _settings = settings;
        _result = new();
        _calculator = new();
        _logger = logger;
    }


    //     public RatingEngine(
    // // DateTime startAt, 
    //         ILogger<RatingEngine> logger)
    //     {
    //         // _lastRatingPeriodStart = startAt;
    //         _result = new();
    //         _calculator = new();
    //         _logger = logger;
    //     }

    private Rating RatingFromRatingData(PlayerRatingsData ratingData)
    {
        var glicko = ratingData.Glicko;
        return new Rating(
            ratingSystem: _calculator,
            initRating: glicko.Rating,
            initRatingDeviation: glicko.Deviation,
            initVolatility: glicko.Volatility,
            numberOfResults: ratingData.NB,
            lastRatingPeriodEnd: ratingData.Latest
             );
    }

    // 
    public (List<int> RatingDiffs, List<PlayerRatingsData> PrevPerfs, List<PlayerRatingsData> NewPerfs, List<PlayerRatings> UserRatings) CalculateRatingAndPerfsAsync(GameResult gameResult, ConcreteGameVariant variantType, List<PlayerRatings> usersRatings, DateTime endTime)
    {
        if (!variantType.RatingAllowed())
        {
            throw new InvalidOperationException($"Can't calculate rating for variants other than {RateableVariants().Aggregate("", (p, n) => p + ", " + n.ToString())}");
        }

        foreach (var userR in usersRatings)
        {
            if (userR.GetRatingData(variantType) == null)
            {
                userR.Ratings[variantType.ToKey()] = GetInitialRatingData();
            }
        }

        // var usersRatings = await Task.WhenAll(players.Keys.Select(id => _userRepo.GetUserRatings(id)!).ToList());
        var ratingGame = GetPlayerRatingsForGame(variantType, usersRatings, gameResult, endTime);

        var oldRatings = ratingGame.PlayersRatingData;

        // var prevPlayers = prevPerfs.Select(perfs => perfs[perfKey].ToGlickoPlayer()).ToList();
        var result = new RatingPeriodResults();

        var p1Stone = gameResult.GetWinnerStone() ?? StoneType.Black;
        var p1 = ratingGame.PlayersRatingData[(int)p1Stone];
        var p2Stone = gameResult.GetLoserStone() ?? StoneType.White;
        var p2 = ratingGame.PlayersRatingData[(int)p2Stone];

        if (gameResult == GameResult.Draw)
        {
            result.AddDraw(
                player1: RatingFromRatingData(p1),
                player2: RatingFromRatingData(p2)
            );
        }
        else
        {
            result.AddResult(
                winner: RatingFromRatingData(p1),
                loser: RatingFromRatingData(p2)
            );
        }

        var computedPlayers = ComputeGlickoAsync(result, ratingGame, p1Stone, p2Stone);

        var newRatings = computedPlayers;

        var ratingDiffs = oldRatings.Zip(newRatings, (prev, next) =>
        {
            int ratingOf(PlayerRatingsData p) => (int)p.Glicko.Rating;
            return ratingOf(next) - ratingOf(prev);
        }).ToList();

        var newUsers = usersWithNewRatings(usersRatings.ToList(), newRatings, ratingGame);

        return (ratingDiffs, oldRatings, newRatings, usersRatings.ToList());
    }

    public double PreviewDeviation(PlayerRatingsData data, DateTime ratingPeriodEndDate, bool reverse)
    {
        return _calculator.PreviewDeviation(RatingFromRatingData(data), ratingPeriodEndDate, reverse);
    }

    private List<PlayerRatingsData> ComputeGlickoAsync(RatingPeriodResults results, UncalculatedRatingGame game, StoneType p1, StoneType p2)
    {
        Debug.Assert(results.GetParticipants().Count() == 2, "Only one result should be added at a time");

        try
        {
            var result = results.GetResults(results.GetParticipants().First()).First();

            _calculator.UpdateRatings(results, skipDeviationIncrease: true);

            List<PlayerRatingsData> players = [null, null];

            var p1Glicko = new GlickoRating(
                    rating: result.GetPlayer1().GetRating(),
                    deviation: result.GetPlayer1().GetRatingDeviation(),
                    volatility: result.GetPlayer1().GetVolatility()
                );

            var p1Id = (int)p1;

            players[p1Id] = new PlayerRatingsData(
                glicko: p1Glicko,
                nb: game.PlayersRatingData[p1Id].NB + 1,
                recent: game.PlayersRatingData[p1Id].UpdateRecentWith(p1Glicko),
                latest: DateTime.Now
            );


            var p2Glicko = new GlickoRating(
                    rating: result.GetPlayer2().GetRating(),
                    deviation: result.GetPlayer2().GetRatingDeviation(),
                    volatility: result.GetPlayer2().GetVolatility()
                );

            var p2Id = (int)p2;
            players[p2Id] = new PlayerRatingsData(
                glicko: p2Glicko,
                nb: game.PlayersRatingData[p2Id].NB + 1,
                recent: game.PlayersRatingData[p2Id].UpdateRecentWith(p2Glicko),
                latest: DateTime.Now
            );

            var firstPlayer = players[0];
            var secondPlayer = players[1];

            return [firstPlayer, secondPlayer];
        }
        catch (Exception ex)
        {
            _logger.LogInformation($"Error computing Glicko2 for game: {ex.Message}");
            throw new Exception("Error computing Glicko2 for game", ex);
        }
    }


    private UncalculatedRatingGame GetPlayerRatingsForGame(ConcreteGameVariant variantType, List<PlayerRatings> userRating, GameResult res, DateTime endTime)
    {
        var key = variantType.ToKey();
        var userRatings = userRating.Select(u => u.Ratings[key]).ToList();

        return new UncalculatedRatingGame(userRatings, res, endTime, variantType);
    }

    private List<PlayerRatings> usersWithNewRatings(List<PlayerRatings> oldUsers, List<PlayerRatingsData> newRatings, UncalculatedRatingGame ratingGame)
    {
        var style = ratingGame.Variant.ToKey();

        var result = oldUsers.Zip(newRatings, (oldUser, rating) =>
        {
            oldUser.Ratings[style] = rating;

            // var (key, data) = RatingForTimeStandard(new VariantType(null, ratingGame.Variant.TimeStandard), oldUser);

            // oldUser.Ratings[key] = data;

            return new PlayerRatings(
                oldUser.PlayerId,
                oldUser.Ratings
            );
        }).ToList();

        return result;
    }

    // private (string standardKey, PlayerRatingsData data) RatingForTimeStandard(ConcreteGameVariant variant, PlayerRatings p)
    // {
    //     var timeStandardStyle = variant.ToKey();

    //     // Collecting the sub-performances
    //     var subs = RateableBoards().Select(boardSize => p.Ratings[new ConcreteGameVariant(boardSize, variant.TimeStandard).ToKey()])
    //         .Where(perf => perf.Latest == null ? false : !IsRatingProvisional(perf, (DateTime)perf.Latest))
    //         .ToList();

    //     // Determining the latest date
    //     var latestStyle = subs.Where(a => a.Latest != null).MaxBy((a) => a.Latest);
    //     // .MaxByOption(perf => perf.Latest.HasValue ? perf.Latest.Value.Ticks : 0)
    //     // ?.Latest;

    //     static int nbSelector(PlayerRatingsData p) => p.NB;

    //     // Updating the standard performance
    //     var newStandard = (latestStyle?.Latest.HasValue ?? false)
    //         ? new PlayerRatingsData
    //         (
    //             glicko: new GlickoRating(
    //                 rating: subs.Sum(s => s.Glicko.Rating * (s.NB / subs.Sum(nbSelector))),
    //                 deviation: subs.Sum(s => s.Glicko.Deviation * (s.NB / subs.Sum(nbSelector))),
    //                 volatility: subs.Sum(s => s.Glicko.Volatility * (s.NB / subs.Sum(nbSelector)))
    //         ),
    //             nb: subs.Sum(nbSelector),
    //             recent: new(),
    //             latest: latestStyle.Latest
    //         )
    //         : p.Ratings[timeStandardStyle];

    //     // Returning a new UserPerfs instance with the updated Standard
    //     return (timeStandardStyle, new PlayerRatingsData(
    //         glicko: newStandard.Glicko,
    //         nb: newStandard.NB,
    //         recent: newStandard.Recent,
    //         latest: newStandard.Latest
    //     ));
    // }

    public static IEnumerable<BoardSize> RateableBoards()
    {
        return Enum.GetValues(typeof(BoardSize)).Cast<BoardSize>().Where(a => a.RatingAllowed());
    }

    public static IEnumerable<TimeStandard> RateableTimeStandards()
    {
        return Enum.GetValues(typeof(TimeStandard)).Cast<TimeStandard>().Where(a => a.RatingAllowed());
    }


    public static IEnumerable<ConcreteGameVariant> RateableVariants()
    {
        return RateableBoards().SelectMany(a => RateableTimeStandards().Select(t => new ConcreteGameVariant(a, t)));
    }


    public bool IsRatingProvisional(PlayerRatingsData rating, DateTime periodEnd)
    {
        return PreviewDeviation(rating, periodEnd, false) > ProvisionalDeviation;
    }


    public static PlayerRatingsData GetInitialRatingData()
    {
        return new PlayerRatingsData(new GlickoRating(1500, 110, 0.06), nb: 0, recent: [], latest: null);
    }
}

public class UncalculatedRatingGame
{
    public List<PlayerRatingsData> PlayersRatingData;
    public ConcreteGameVariant Variant;
    public GameResult Result;
    public DateTime EndTime;

    public UncalculatedRatingGame(List<PlayerRatingsData> players, GameResult res, DateTime endTime, ConcreteGameVariant variant)
    {
        PlayersRatingData = players;
        Result = res;
        EndTime = endTime;
        Variant = variant;
    }

}


static class PlayerRatingDataExt
{
    public static readonly int RecentMaxSize = 12;

    public static List<int> UpdateRecentWith(this PlayerRatingsData data, GlickoRating glicko)
    {
        var p = data;
        if (p.NB < 10)
        {
            return p.Recent;
        }

        // Prepend glicko.IntRating to p.Recent and limit to Perf.RecentMaxSize
        var updatedRecent = new List<int> { (int)glicko.Rating };
        updatedRecent.AddRange(p.Recent);

        // Truncate the list to Perf.RecentMaxSize
        return updatedRecent.Take(RecentMaxSize).ToList();
    }
}
