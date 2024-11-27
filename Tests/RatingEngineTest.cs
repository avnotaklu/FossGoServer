using BadukServer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests;
[TestClass]
public class RatingEngineTest
{
    private (ILogger<RatingEngine>, Mock<IUserRatingService>) GetEngineConstructorParams()
    {
        var serviceProvider = new ServiceCollection()
            .AddLogging()
            .BuildServiceProvider();

        var factory = serviceProvider.GetService<ILoggerFactory>()!;

        var logger = factory.CreateLogger<RatingEngine>();

        var userRepoMock = new Mock<IUserRatingService>();
        return (logger, userRepoMock);
    }

    [TestMethod]
    public async Task TestRatingEngineSimple()
    {
        var (logger, userRepoMock) = GetEngineConstructorParams();

        var blitz_nine_by_nine_style = "B0-S0";
        var blitz_style = "S0";

        List<UserRating> userRatings = GetSimpleUserRatings();

        userRepoMock.Setup(x => x.GetUserRatings("a")).ReturnsAsync(() => userRatings[0]);
        userRepoMock.Setup(x => x.GetUserRatings("b")).ReturnsAsync(() => userRatings[1]);

        var engine = new RatingEngine(logger, userRepoMock.Object);

        var winnerId = "a";
        var boardSize = BoardSize.Nine;
        var timeStandard = TimeStandard.Blitz;
        var players = new Dictionary<string, StoneType>
        {
            ["a"] = StoneType.Black,
            ["b"] = StoneType.White
        };

        var res = await engine.CalculateRatingAndPerfsAsync(winnerId, boardSize, timeStandard, players, DateTime.Now);

        Assert.IsTrue(res.RatingDiffs[(int)players[winnerId]] > 0);
        Assert.IsTrue(res.RatingDiffs[(int)players.GetOtherStoneFromPlayerIdAlt(winnerId)!] < 0);

        Assert.IsTrue(res.UserRatings[(int)players[winnerId]].Ratings[blitz_nine_by_nine_style].Glicko.Rating > 1500);
        Assert.IsTrue(res.UserRatings[(int)players.GetOtherStoneFromPlayerIdAlt(winnerId)!].Ratings[blitz_nine_by_nine_style].Glicko.Rating < 1500);

        Assert.IsTrue(res.UserRatings[(int)players[winnerId]].Ratings[blitz_style].Glicko.Rating > 1500);
        Assert.IsTrue(res.UserRatings[(int)players.GetOtherStoneFromPlayerIdAlt(winnerId)!].Ratings[blitz_style].Glicko.Rating < 1500);
    }

    [TestMethod]
    public void TestPreviewRatingDeviation()
    {
        var rating = GetRatingDataWithWithYearOldRatingPeriod();
        var now = DateTime.Now;
        var reverse = false;

        var (logger, userRepoMock) = GetEngineConstructorParams();

        var preview = new RatingEngine(logger, userRepoMock.Object).PreviewDeviation(rating, now, reverse);

        Assert.IsTrue(preview > 110);
    }

    private static List<UserRating> GetSimpleUserRatings()
    {
        return new List<UserRating>
        {
            new UserRating("winner", GetInitialTestRatings()),
            new UserRating("loser", GetInitialTestRatings())
        };
    }


    public static Dictionary<string, PlayerRatingData> GetInitialTestRatings()
    {
        return new Dictionary<string, PlayerRatingData>(
        RatingEngine.RateableBoards().Select(a => (BoardSize?)a).Append(null).Select(b => RatingEngine.RateableTimeStandards().Select(t => new KeyValuePair<string, PlayerRatingData>(RatingEngine.RatingKey(b, t), GetRatingDataWithAlmostGoodDeviation()))).SelectMany(a => a)
                    );
    }

    private static PlayerRatingData GetRatingDataWithAlmostGoodDeviation()
    {
        return new PlayerRatingData(new GlickoRating(1500, 111, 0.06), nb: 0, recent: [], latest: null);
    }

    private static PlayerRatingData GetRatingDataWithWithYearOldRatingPeriod()
    {
        return new PlayerRatingData(new GlickoRating(1500, 80, 0.06), nb: 0, recent: [], latest: DateTime.Now.AddYears(-1));
    }
}