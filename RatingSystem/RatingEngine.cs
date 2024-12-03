
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
    public (List<int> RatingDiffs, List<PlayerRatingData> PrevPerfs, List<PlayerRatingData> NewPerfs, List<UserRating> UserRatings) CalculateRatingAndPerfsAsync(string winnerId, BoardSize boardSize, TimeStandard timeStandard, Dictionary<string, StoneType> players, List<UserRating> usersRatings, DateTime endTime);
    public double PreviewDeviation(PlayerRatingData data, DateTime ratingPeriodEndDate, bool reverse);
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

    private Rating RatingFromRatingData(PlayerRatingData ratingData)
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
    public (List<int> RatingDiffs, List<PlayerRatingData> PrevPerfs, List<PlayerRatingData> NewPerfs, List<UserRating> UserRatings) CalculateRatingAndPerfsAsync(string winnerId, BoardSize boardSize, TimeStandard timeStandard, Dictionary<string, StoneType> players, List<UserRating> usersRatings, DateTime endTime)
    {
        if (winnerId == null)
        {
            throw new InvalidOperationException("Game is not over yet");
        }
        if (boardSize == BoardSize.Other)
        {
            throw new InvalidOperationException("Can't calculate rating for games with board size other than 9, 13, 19");
        }

        // var usersRatings = await Task.WhenAll(players.Keys.Select(id => _userRepo.GetUserRatings(id)!).ToList());
        var ratingGame = GetPlayerRatingsForGame(boardSize, timeStandard, usersRatings, winnerId, players, endTime);

        var oldRatings = ratingGame.PlayersRatingData;

        // var prevPlayers = prevPerfs.Select(perfs => perfs[perfKey].ToGlickoPlayer()).ToList();
        var result = new RatingPeriodResults();

        var winner = ratingGame.PlayersRatingData[(int)players[winnerId]!];
        var loser = ratingGame.PlayersRatingData[(int)players.GetOtherStoneFromPlayerId(winnerId)!];

        result.AddResult(
            winner: RatingFromRatingData(winner),
            loser: RatingFromRatingData(loser)
        );

        var computedPlayers = ComputeGlickoAsync(result, ratingGame);

        var newRatings = computedPlayers;

        var ratingDiffs = oldRatings.Zip(newRatings, (prev, next) =>
        {
            int ratingOf(PlayerRatingData p) => (int)p.Glicko.Rating;
            return ratingOf(next) - ratingOf(prev);
        }).ToList();

        var newUsers = usersWithNewRatings(usersRatings.ToList(), newRatings, ratingGame);

        return (ratingDiffs, oldRatings, newRatings, usersRatings.ToList());
    }

    public double PreviewDeviation(PlayerRatingData data, DateTime ratingPeriodEndDate, bool reverse)
    {
        return _calculator.PreviewDeviation(RatingFromRatingData(data), ratingPeriodEndDate, reverse);
    }

    private List<PlayerRatingData> ComputeGlickoAsync(RatingPeriodResults results, UncalculatedRatingGame game)
    {
        Debug.Assert(results.GetParticipants().Count() == 2, "Only one result should be added at a time");

        try
        {
            var result = results.GetResults(results.GetParticipants().First()).First();

            _calculator.UpdateRatings(results, skipDeviationIncrease: true);

            List<PlayerRatingData> players = [null, null];

            var winnerGlicko = new GlickoRating(
                    rating: result.GetWinner().GetRating(),
                    deviation: result.GetWinner().GetRatingDeviation(),
                    volatility: result.GetWinner().GetVolatility()
                );
            players[game.Winner] = new PlayerRatingData(
                glicko: winnerGlicko,
                nb: game.PlayersRatingData[game.Winner].NB + 1,
                recent: game.PlayersRatingData[game.Winner].UpdateRecentWith(winnerGlicko),
                latest: DateTime.Now
            );


            var loserGlicko = new GlickoRating(
                    rating: result.GetLoser().GetRating(),
                    deviation: result.GetLoser().GetRatingDeviation(),
                    volatility: result.GetLoser().GetVolatility()
                );
            players[game.Loser] = new PlayerRatingData(
                glicko: loserGlicko,
                nb: game.PlayersRatingData[game.Loser].NB + 1,
                recent: game.PlayersRatingData[game.Loser].UpdateRecentWith(loserGlicko),
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



    private UncalculatedRatingGame GetPlayerRatingsForGame(BoardSize boardSize, TimeStandard timeStandard, List<UserRating> userRating, string winnerId, Dictionary<string, StoneType> players, DateTime endTime)
    {
        var style = RatingKey(boardSize, timeStandard);
        var userRatings = userRating.Select(u => u.Ratings[style]).ToList();
        var winner = players[winnerId];

        return new UncalculatedRatingGame(userRatings, (int)winner, endTime, boardSize, timeStandard);
    }

    private List<UserRating> usersWithNewRatings(List<UserRating> oldUsers, List<PlayerRatingData> newRatings, UncalculatedRatingGame ratingGame)
    {
        var style = ratingGame.GameStyle;

        var result = oldUsers.Zip(newRatings, (oldUser, rating) =>
        {
            oldUser.Ratings[style] = rating;

            var (key, data) = RatingForTimeStandard(ratingGame.Standard, oldUser);
            oldUser.Ratings[key] = data;

            return new UserRating(
                oldUser.UserId,
                oldUser.Ratings
            );
        }).ToList();

        return result;
    }

    private (string standardKey, PlayerRatingData data) RatingForTimeStandard(TimeStandard timeStandard, UserRating p)
    {
        var timeStandardStyle = RatingKey(null, timeStandard);

        // Collecting the sub-performances
        var subs = RateableBoards().Select(boardSize => p.Ratings[RatingKey(boardSize, timeStandard)])
            .Where(perf => !IsRatingProvisional(perf))
            .ToList();

        // Determining the latest date
        var latestStyle = subs.Where(a => a.Latest != null).MaxBy((a) => a.Latest);
        // .MaxByOption(perf => perf.Latest.HasValue ? perf.Latest.Value.Ticks : 0)
        // ?.Latest;

        static int nbSelector(PlayerRatingData p) => p.NB;

        // Updating the standard performance
        var newStandard = (latestStyle?.Latest.HasValue ?? false)
            ? new PlayerRatingData
            (
                glicko: new GlickoRating(
                    rating: subs.Sum(s => s.Glicko.Rating * (s.NB / subs.Sum(nbSelector))),
                    deviation: subs.Sum(s => s.Glicko.Deviation * (s.NB / subs.Sum(nbSelector))),
                    volatility: subs.Sum(s => s.Glicko.Volatility * (s.NB / subs.Sum(nbSelector)))
            ),
                nb: subs.Sum(nbSelector),
                recent: new(),
                latest: latestStyle.Latest
            )
            : p.Ratings[timeStandardStyle];

        // Returning a new UserPerfs instance with the updated Standard
        return (timeStandardStyle, new PlayerRatingData(
            glicko: newStandard.Glicko,
            nb: newStandard.NB,
            recent: newStandard.Recent,
            latest: newStandard.Latest
        ));
    }

    public static IEnumerable<BoardSize> RateableBoards()
    {
        return Enum.GetValues(typeof(BoardSize)).Cast<BoardSize>().Where(a => a != BoardSize.Other);
    }

    public static IEnumerable<TimeStandard> RateableTimeStandards()
    {
        return Enum.GetValues(typeof(TimeStandard)).Cast<TimeStandard>().Where(a => a != TimeStandard.Other);
    }


    public static bool IsRatingProvisional(PlayerRatingData rating)
    {
        return rating.Glicko.Deviation > ProvisionalDeviation;
    }

    public static string RatingKey(BoardSize? size, TimeStandard time)
    {
        if (size == null)
        {
            return $"S{(int)time}";
        }
        return $"B{(int)size}-S{(int)time}";
    }
}

public class UncalculatedRatingGame
{
    public List<PlayerRatingData> PlayersRatingData;
    public string GameStyle => RatingEngine.RatingKey(Size, Standard);
    public BoardSize Size;
    public TimeStandard Standard;
    public int Winner;
    public int Loser => 1 - Winner;
    public DateTime EndTime;

    public UncalculatedRatingGame(List<PlayerRatingData> players, int winner, DateTime endTime, BoardSize size, TimeStandard standard)
    {
        PlayersRatingData = players;
        Winner = winner;
        EndTime = endTime;
        Size = size;
        Standard = standard;
    }

}

// public class UserRatingForGame
// {
//     public string UserId => _user.UserId;
//     public Dictionary<string, PlayerRatingData> _Ratings => ;
//     private UserRating _user;
//     private UserRating _user;

//     public UserRatingForGame(Game game, UserRating rating)
//     {
//         UserId = userId;
//         Ratings = ratings;
//     }
// }


// public class PlayerRatingPerStyle
// {
//     public PlayerRatingData Rating;
//     public BoardSize? BoardSize;
//     public GameSpeed? GameSpeed;

// }

static class PlayerRatingDataExt
{
    public static readonly int RecentMaxSize = 12;

    public static List<int> UpdateRecentWith(this PlayerRatingData data, GlickoRating glicko)
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
