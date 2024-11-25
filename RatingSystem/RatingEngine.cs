
using System.Diagnostics;
using BadukServer;
using Glicko2;
using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;

// public class RatingPlayer
// {

// }


public class GlickoSettings
{
    public Rating start_rating;
    public double volatility_change;
    public double convergence_tolerance;
    public TimeSpan RatingPeriodDuration;
}


public class RatingEngine
{
    // private readonly List<Rating> _players = [];
    // private readonly List<Result> _results = [];
    private readonly RatingPeriodResults _result;
    private DateTime _lastRatingPeriodStart;
    private GlickoSettings _settings;
    private RatingCalculator _calculator;
    private Logger<RatingEngine> _logger;

    public RatingEngine(GlickoSettings settings, Logger<RatingEngine> logger)
    {
        _lastRatingPeriodStart = DateTime.Now;
        _settings = settings;
        _result = new();
        _calculator = new();
        _logger = logger;
    }

    public RatingEngine(DateTime startAt, GlickoSettings settings)
    {
        _lastRatingPeriodStart = startAt;
        _settings = settings;
        _result = new();
        _calculator = new();
    }


    // /// <summary>
    // /// Add a result to the result.
    // /// </summary>
    // /// <param name="winner"></param>
    // /// <param name="loser"></param>
    // public void AddResult(Rating winner, Rating loser)
    // {
    //     _result.AddResult(winner, loser);
    // }

    // /// <summary>
    // /// Record a draw between two players and add to the result.
    // /// </summary>
    // /// <param name="player1"></param>
    // /// <param name="player2"></param>
    // public void AddDraw(Rating player1, Rating player2)
    // {
    //     _result.AddDraw(player1, player2);
    // }


    // public Rating GetRating(Rating player)
    // {
    //     throw new NotImplementedException();
    // }

    // /// <summary>
    // /// Calculates a player's rating at the current point in time.
    // /// This calculation is based on the registered results for this player.
    // /// </summary>
    // /// <param name="player">The player handle.</param>
    // /// <returns>
    // /// A tuple containing the player's current rating and the number of rating periods
    // /// that were closed during this operation.
    // /// </returns>
    // /// <exception cref="InvalidOperationException">
    // /// Thrown if the player was not sourced from this <see cref="RatingEngine"/>.
    // /// </exception>
    // public (Rating, uint) GetPlayerRating<TScale>(PlayerHandle player)
    // {
    //     return GetPlayerRatingAt(player, DateTime.UtcNow);
    // }

    // /// <summary>
    // /// Calculates a player's rating at a given point in time.
    // /// This calculation is based on the registered results for this player.
    // /// </summary>
    // /// <param name="player">The player handle.</param>
    // /// <param name="time">The point in time for which to calculate the rating.</param>
    // /// <returns>
    // /// A tuple containing the player's current rating and the number of rating periods
    // /// that were closed during this operation.
    // /// </returns>
    // /// <exception cref="InvalidOperationException">
    // /// Thrown if the player was not sourced from this <see cref="RatingEngine"/>.
    // /// </exception>
    // public (Rating, uint) GetPlayerRatingAt(PlayerHandle player, DateTime time)
    // {
    //     var (elapsedPeriods, closedPeriods) = MaybeCloseRatingPeriodsAt(time);

    //     if (player.Index < 0 || player.Index >= _managedPlayers.Count)
    //     {
    //         throw new InvalidOperationException("Player did not belong to this RatingEngine.");
    //     }

    //     var managedPlayer = _managedPlayers[player.Index];

    //     var rating = Algorithm.RateGamesUntimed(
    //         managedPlayer.Rating,
    //         managedPlayer.CurrentRatingPeriodResults,
    //         elapsedPeriods,
    //         _settings
    //     ).WithSettings(_settings);

    //     return (rating, closedPeriods);
    // }

    // /// <summary>
    // /// Closes all open rating periods that have elapsed by now.
    // /// This doesn't need to be called manually.
    // /// When a rating period is closed, the stored results are cleared, and the players' ratings
    // /// at the end of the period are stored as their ratings at the beginning of the next one.
    // /// </summary>
    // /// <returns>
    // /// A tuple containing the elapsed periods in the current rating period <i>after</i> all previous periods 
    // /// have been closed as a fraction, as well as the amount of rating periods that have been closed.
    // /// The elapsed periods will always be smaller than 1.
    // /// </returns>
    // public (double elapsedPeriods, uint closedPeriods) MaybeCloseRatingPeriods()
    // {
    //     return MaybeCloseRatingPeriodsAt(DateTime.UtcNow);
    // }

    // /// <summary>
    // /// Closes all open rating periods that have elapsed by a given point in time.
    // /// This doesn't need to be called manually.
    // /// When a rating period is closed, the stored results are cleared, and the players' ratings
    // /// at the end of the period are stored as their ratings at the beginning of the next one.
    // /// </summary>
    // /// <param name="time">The point in time to calculate elapsed periods.</param>
    // /// <returns>
    // /// A tuple containing the elapsed periods in the current rating period <i>after</i> all previous periods 
    // /// have been closed as a fraction, as well as the amount of rating periods that have been closed.
    // /// The elapsed periods will always be smaller than 1.0.
    // /// </returns>
    // public (double elapsedPeriods, uint closedPeriods) MaybeCloseRatingPeriodsAt(DateTime time)
    // {
    //     double elapsedPeriods = ElapsedPeriodsAt(time);

    //     // Truncate elapsedPeriods to get the number of periods to close.
    //     uint periodsToClose = (uint)Math.Floor(elapsedPeriods);

    //     // Close the rating periods.
    //     for (int i = 0; i < periodsToClose; i++)
    //     {
    //         foreach (var player in _players)
    //         {
    //             // Update the player's rating with the current rating period results.
    //             // player.Rating = 

    //             // Algorithm.RateGamesUntimed(
    //             //     player.Rating,
    //             //     player.CurrentRatingPeriodResults,
    //             //     1.0,
    //             //     settings
    //             // );

    //             _calculator.CalculateNewRating(player, )

    //             // Clear the results for the next period.
    //             player.CurrentRatingPeriodResults.Clear();
    //         }
    //     }

    //     // Update the start of the last rating period.
    //     _lastRatingPeriodStart = _lastRatingPeriodStart.AddSeconds(periodsToClose * ratingPeriodDuration.TotalSeconds);

    //     // Return the fractional part of the elapsed periods and the number of periods closed.
    //     return (elapsedPeriods % 1.0, periodsToClose);
    // }

    // /// <summary>
    // /// The amount of rating periods that have elapsed since the last one was closed as a fraction.
    // /// </summary>
    // /// <returns>The elapsed periods as a fraction.</returns>
    // public double ElapsedPeriods()
    // {
    //     return ElapsedPeriodsAt(DateTime.UtcNow);
    // }

    // /// <summary>
    // /// The amount of rating periods that have elapsed at the given point in time since the last one was closed as a fraction.
    // /// If the given time is earlier than the start of the last rating period, this function returns 0.0.
    // /// </summary>
    // /// <param name="time">The point in time to calculate elapsed periods.</param>
    // /// <returns>The elapsed periods as a fraction.</returns>
    // public double ElapsedPeriodsAt(DateTime time)
    // {
    //     if (time >= _lastRatingPeriodStart)
    //     {
    //         var elapsedDuration = time - _lastRatingPeriodStart;
    //         return elapsedDuration.TotalSeconds / _settings.RatingPeriodDuration.TotalSeconds;
    //     }
    //     return 0.0;
    // }

    private Rating RatingFromGlickoRating(GlickoRating rating)
    {
        return new Rating(_calculator, rating.Rating, rating.Deviation, rating.Volatility);
    }

    // 
    public async Task<(string GameId, List<int> RatingDiffs, List<PlayerRatingData> PrevPerfs, List<PlayerRatingData> NewPerfs, List<UserWithPlayerRating> Users, string PerfKey)> CalculateRatingAndPerfsAsync(Game game)
    {
        if (game.GameOverMethod == null && game.WinnerId == null)
        {
            throw new InvalidOperationException("Game is not over yet");
        }

        var ratingGame = GetPlayerRatingsForGame(game);
        var key = GetStyleKey(game);
        var users = GetUsers(game);


        var prevPerfs = ratingGame.Players.Select(p => p.Rating).ToList();

        // var prevPlayers = prevPerfs.Select(perfs => perfs[perfKey].ToGlickoPlayer()).ToList();
        var result = new RatingPeriodResults();

        var winner = ratingGame.Players[(int)game.GetStoneFromPlayerId(game.WinnerId!)!];
        var loser = ratingGame.Players[(int)game.GetOtherStoneFromPlayerId(game.WinnerId!)!];

        result.AddResult(
                winner: new Rating(
                    ratingSystem: _calculator,
                    initRating: winner.Rating.Rating.Rating,
                    initRatingDeviation: winner.Rating.Rating.Deviation,
                    initVolatility: winner.Rating.Rating.Volatility
                ),
                loser: new Rating(
                    ratingSystem: _calculator,
                    initRating: loser.Rating.Rating.Rating,
                    initRatingDeviation: loser.Rating.Rating.Deviation,
                    initVolatility: loser.Rating.Rating.Volatility
                )
        );

        var computedPlayers = ComputeGlickoAsync(result, ratingGame);

        // var newGlickos = _ratingRegulator.Regulate(
        //     perfKey,
        //     prevPlayers.Select(p => p.Glicko).ToList(),
        //     computedPlayers.Select(p => p.Glicko).ToList(),
        //     users.Select(u => u.IsBot).ToList()
        // );

        var newPerfs = computedPlayers;

        // var newPerfs = prevPerfs.Zip(newGlickos, (perfs, glicko) => AddToPerfs(game, perfs, perfKey, glicko)).ToList();

        var ratingDiffs = prevPerfs.Zip(newPerfs, (prev, next) =>
        {
            int ratingOf(PlayerRatingData p) => (int)p.Rating.Rating;
            return ratingOf(next) - ratingOf(prev);
        }).ToList();

        var newUsers = users.Zip(newPerfs, (user, perfs) =>
        {
            user.Ratings[key] = perfs;
            return new UserWithPlayerRating(
                        user.UserId,
                        user.Ratings
                    );
        }).ToList();

        // _bus.Publish(new PerfsUpdate(game, newUsers), "perfsUpdate");

        return (game.Id!, ratingDiffs, prevPerfs, newPerfs, users.ToList(), key);
    }

    private List<PlayerRatingData> ComputeGlickoAsync(RatingPeriodResults results, RatingGame game)
    {
        Debug.Assert(results.GetParticipants().Count() == 2, "Only one result should be added at a time");

        try
        {
            var result = results.GetResults(results.GetParticipants().First()).First();

            _calculator.UpdateRatings(results, skipDeviationIncrease: true);

            List<PlayerRatingData> players = [null, null];

            players[game.Winner] = new PlayerRatingData(
                glicko: new GlickoRating(
                    rating: result.GetWinner().GetRating(),
                    deviation: result.GetWinner().GetRatingDeviation(),
                    volatility: result.GetWinner().GetVolatility()
                ),
                nb: 1,
                recent: null,
                latest: DateTime.Now
            );


            players[game.Winner] = new PlayerRatingData(
                glicko: new GlickoRating(
                    rating: result.GetWinner().GetRating(),
                    deviation: result.GetWinner().GetRatingDeviation(),
                    volatility: result.GetWinner().GetVolatility()
                ),
                nb: 1,
                recent: null,
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

    private RatingGame GetPlayerRatingsForGame(Game game)
    {
        throw new NotImplementedException();
    }

    private string GetStyleKey(Game game)
    {
        throw new NotImplementedException();
    }


    private List<UserWithPlayerRating> GetUsers(Game game)
    {
        throw new NotImplementedException();
    }


}

public class RatingGame
{
    public List<PlayerRatingPerStyle> Players;
    public int Winner;
    public int Loser => 1 - Winner;

    public RatingGame(List<PlayerRatingPerStyle> players, int winner)
    {
        Players = players;
        Winner = winner;
    }

}

public enum BoardSize
{
    Nine = 0,
    Thirteen = 1,
    Nineteen = 2
}


public enum GameSpeed
{
    Blitz = 0,
    Rapid = 1,
    Classical = 2,
    Correspondence = 3
}

public class UserWithPlayerRating
{
    public string UserId;
    public Dictionary<string, PlayerRatingData> Ratings;

    public UserWithPlayerRating(string userId, Dictionary<string, PlayerRatingData> ratings)
    {
        UserId = userId;
        Ratings = ratings;
    }
}


public class PlayerRatingPerStyle
{
    public PlayerRatingData Rating;
    public BoardSize? BoardSize;
    public GameSpeed? GameSpeed;

    public String Key()
    {
        return $"B{BoardSize}-S{GameSpeed}";
    }
}

public class PlayerRatingData
{
    public GlickoRating Rating;
    public int NB;
    public List<int>? Recent;
    public DateTime Latest;

    public PlayerRatingData(GlickoRating glicko, int nb, List<int>? recent, DateTime latest)
    {
        Rating = glicko;
        NB = nb;
        Recent = recent;
        Latest = latest;
    }
}

public class GlickoRating
{
    public double Rating;
    public double Deviation;
    public double Volatility;

    public GlickoRating(double rating, double deviation, double volatility)
    {
        Rating = rating;
        Deviation = deviation;
        Volatility = volatility;
    }
}


