using BadukServer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests;
[TestClass]
public class RatingEngineTest
{
    [TestMethod]
    public async Task TestRatingEngineSimple()
    {
        var serviceProvider = new ServiceCollection()
            .AddLogging()
            .BuildServiceProvider();

        var factory = serviceProvider.GetService<ILoggerFactory>()!;

        var logger = factory.CreateLogger<RatingEngine>();

        var userRepoMock = new Mock<IUserRatingService>();

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

        Assert.IsTrue(res.Users[(int)players[winnerId]].Ratings[blitz_nine_by_nine_style].Glicko.Rating > 1500);
        Assert.IsTrue(res.Users[(int)players.GetOtherStoneFromPlayerIdAlt(winnerId)!].Ratings[blitz_nine_by_nine_style].Glicko.Rating < 1500);


        // Assert.IsTrue(res.Users[(int)players[winnerId]].Ratings[blitz_style].Glicko.Rating > 1500);
        // Assert.IsTrue(res.Users[(int)players.GetOtherStoneFromPlayerIdAlt(winnerId)!].Ratings[blitz_style].Glicko.Rating < 1500);
    }

    private static List<UserRating> GetSimpleUserRatings()
    {
        return new List<UserRating>
        {
            new UserRating("winner", UserRatingService.GetInitialRatings()),
            new UserRating("loser", UserRatingService.GetInitialRatings())
        };
    }

}