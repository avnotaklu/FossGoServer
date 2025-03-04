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

        var blitz_nine_by_nine_style = "0_0";
        var blitz_style = "_0";
        var nine_by_nine_style = "0_";

        List<PlayerRatings> userRatings = GetSimpleUserRatings();

        userRepoMock.Setup(x => x.GetUserRatings("a")).ReturnsAsync(() => userRatings[0]);
        userRepoMock.Setup(x => x.GetUserRatings("b")).ReturnsAsync(() => userRatings[1]);

        var engine = new RatingEngine(logger);

        var winnerId = "a";

        var boardSize = BoardSize.Nine;
        var timeStandard = TimeStandard.Blitz;

        var variant = new ConcreteGameVariant(boardSize, timeStandard);

        List<string> players = [
            "a", "b"
        ];

        // new Dictionary<string, StoneType>
        // {
        //     ["a"] = StoneType.Black,
        //     ["b"] = StoneType.White
        // };

        var res = engine.CalculateRatingAndPerfsAsync(
                gameResult: GameResult.BlackWon,
                variantType: variant,
                usersRatings: [.. (await Task.WhenAll(players.Select(a => userRepoMock.Object.GetUserRatings(a))))],
                endTime: DateTime.Now
            );

        Assert.IsTrue(res.RatingDiffs[(int)players.GetStoneFromPlayerId(winnerId)!] > 0);
        Assert.IsTrue(res.RatingDiffs[(int)players.GetOtherStoneFromPlayerId(winnerId)!] < 0);

        Assert.IsTrue(res.UserRatings[(int)players.GetStoneFromPlayerId(winnerId)!].Ratings[blitz_nine_by_nine_style].Glicko.Rating > 1500);
        Assert.IsTrue(res.UserRatings[(int)players.GetOtherStoneFromPlayerId(winnerId)!].Ratings[blitz_nine_by_nine_style].Glicko.Rating < 1500);

        // Assert.IsTrue(res.UserRatings[(int)players[winnerId]].Ratings[blitz_style].Glicko.Rating > 1500);
        // Assert.IsTrue(res.UserRatings[(int)players.GetOtherStoneFromPlayerId(winnerId)!].Ratings[blitz_style].Glicko.Rating < 1500);

        // Assert.IsTrue(res.UserRatings[(int)players[winnerId]].Ratings[nine_by_nine_style].Glicko.Rating > 1500);
        // Assert.IsTrue(res.UserRatings[(int)players.GetOtherStoneFromPlayerId(winnerId)!].Ratings[nine_by_nine_style].Glicko.Rating < 1500);
    }

    [TestMethod]
    public void TestPreviewRatingDeviation()
    {
        var rating = GetRatingDataWithWithYearOldRatingPeriod();
        var now = DateTime.Now;
        var reverse = false;

        var (logger, userRepoMock) = GetEngineConstructorParams();

        var preview = new RatingEngine(logger).PreviewDeviation(rating, now, reverse);

        Assert.IsTrue(preview > 110);
    }

    [TestMethod]
    public void TestRateableVariants() {
        var variants = RatingEngine.RateableVariants().ToList();

        Assert.AreEqual(12, variants.Count);

        // Assert.IsTrue(variants.Contains(VariantTypeExt.FromKey("o")));
        Assert.IsTrue(variants.Contains(VariantTypeExt.FromKey("0_0")));
        // Assert.IsTrue(variants.Contains(VariantTypeExt.FromKey("_0")));
        // Assert.IsTrue(variants.Contains(VariantTypeExt.FromKey("0_")));
    }

    private static List<PlayerRatings> GetSimpleUserRatings()
    {
        return new List<PlayerRatings>
        {
            new PlayerRatings("winner", GetInitialTestRatings()),
            new PlayerRatings("loser", GetInitialTestRatings())
        };
    }


    public static Dictionary<string, PlayerRatingsData> GetInitialTestRatings()
    {

        return new Dictionary<string, PlayerRatingsData>();
        //     RatingEngine.RateableVariants().Select(
        //         t => new KeyValuePair<string, PlayerRatingsData>(t.ToKey(), GetRatingDataWithAlmostGoodDeviation())
        //     )
        // );

    }

    private static PlayerRatingsData GetRatingDataWithAlmostGoodDeviation()
    {
        return new PlayerRatingsData(new GlickoRating(1500, 111, 0.06), nb: 0, recent: [], latest: null);
    }

    private static PlayerRatingsData GetRatingDataWithWithYearOldRatingPeriod()
    {
        return new PlayerRatingsData(new GlickoRating(1500, 80, 0.06), nb: 0, recent: [], latest: DateTime.Now.AddYears(-1));
    }
}