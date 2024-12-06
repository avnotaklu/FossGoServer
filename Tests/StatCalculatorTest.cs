using System.Security.Cryptography;
using BadukServer;

namespace Tests;

[TestClass]
public class StatCalculatorTest
{
    [TestMethod]
    public void TestInitialUserStatCounts()
    {
        // Arrange
        var uid = "p1"; // is black

        var oldUserStats = GetEmptyUserStat();

        var game = BlackWonGame9x9BlitzGameOnlyCountsShouldChange();

        var statCalculator = new StatCalculator();

        // Act
        var result = statCalculator.CalculateUserStat(oldUserStats, game);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.stats.ContainsKey("B0_S0"));

        Assert.AreEqual(1, result.stats["B0_S0"].StatCounts.Total);
        Assert.AreEqual(1, result.stats["B0_S0"].StatCounts.Wins);
        Assert.AreEqual(0, result.stats["B0_S0"].StatCounts.Losses);
        Assert.AreEqual(0, result.stats["B0_S0"].StatCounts.Draws);
        Assert.AreEqual(0, result.stats["B0_S0"].StatCounts.Disconnects);
    }


    [TestMethod]
    public void TestUpdatedUserStatCountsAndNoOps()
    {
        // Arrange
        var uid = "p1"; // is black

        var oldUserStats = GetProgressedUserStat(null, null);

        var oldHR = oldUserStats.stats["B0_S0"].HighestRating;
        var oldLR = oldUserStats.stats["B0_S0"].LowestRating;

        var game = BlackWonGame9x9BlitzGameOnlyCountsShouldChange();

        var statCalculator = new StatCalculator();

        // Act
        var result = statCalculator.CalculateUserStat(oldUserStats, game);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.stats.ContainsKey("B0_S0"));
        

        // Counts
        Assert.AreEqual(6, result.stats["B0_S0"].StatCounts.Total);
        Assert.AreEqual(6, result.stats["B0_S0"].StatCounts.Wins);
        Assert.AreEqual(5, result.stats["B0_S0"].StatCounts.Losses);
        Assert.AreEqual(5, result.stats["B0_S0"].StatCounts.Draws);
        Assert.AreEqual(5, result.stats["B0_S0"].StatCounts.Disconnects);

        // No Ops

        Assert.AreEqual(oldHR, result.stats["B0_S0"].HighestRating);
        Assert.AreEqual(oldLR, result.stats["B0_S0"].LowestRating);
    }

    [TestMethod]
    public void TestCurrentStreakLengthUpdate()
    {
        // Arrange
        var uid = "p1"; // is black

        var oldUserStats = GetProgressedUserStat(new ResultStreakData(winningStreaks: new StreakData(currentStreak: Get2To0MonthOldStreakLen5(), greatestStreak: null), losingStreaks: null), null);

        var game = BlackWonGame9x9BlitzGameOnlyCountsShouldChange();

        var statCalculator = new StatCalculator();

        // Act
        var result = statCalculator.CalculateUserStat(oldUserStats, game);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.stats.ContainsKey("B0_S0"));

        Assert.IsNotNull(result.stats["B0_S0"].ResultStreakData);
        Assert.AreEqual(6, result.stats["B0_S0"].ResultStreakData!.WinningStreaks!.CurrentStreak!.StreakLength);
    }


    [TestMethod]
    public void TestGreatestStreakLengthUpdate()
    {
        // Arrange
        var uid = "p1"; // is black
        var curS = Get2To0MonthOldStreakLen5();


        var oldUserStats = GetProgressedUserStat(new ResultStreakData(winningStreaks: new StreakData(currentStreak: curS, greatestStreak: Get12To10MonthOldStreakLen5()), losingStreaks: null), null);

        var game = BlackWonGame9x9BlitzGameOnlyCountsShouldChange();

        var statCalculator = new StatCalculator();

        // Act
        var result = statCalculator.CalculateUserStat(oldUserStats, game);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.stats.ContainsKey("B0_S0"));

        Assert.IsNotNull(result.stats["B0_S0"].ResultStreakData);
        Assert.AreEqual(6, result.stats["B0_S0"].ResultStreakData!.WinningStreaks!.GreatestStreak!.StreakLength);
        Assert.AreEqual(curS.StreakFrom, result.stats["B0_S0"].ResultStreakData!.WinningStreaks!.GreatestStreak!.StreakFrom);
        Assert.AreEqual(game.EndTime!.DeserializedDate(), result.stats["B0_S0"].ResultStreakData!.WinningStreaks!.GreatestStreak!.StreakTo);
    }

    [TestMethod]
    public void TestGreatestWinsUpdate()
    {
        // Arrange
        var uid = "p1"; // is black

        var oldUserStats = GetProgressedUserStat(null, GetBasicGreatestWins());

        var game = BlackWonAgainst1550Player();

        var statCalculator = new StatCalculator();

        // Act
        var result = statCalculator.CalculateUserStat(oldUserStats, game);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.stats.ContainsKey("B0_S0"));

        Assert.IsNotNull(result.stats["B0_S0"].GreatestWins);

        Assert.AreEqual(5, result.stats["B0_S0"].GreatestWins!.Count);

        Assert.AreEqual(1600, result.stats["B0_S0"].GreatestWins![0].OpponentRating);
        Assert.AreEqual(1580, result.stats["B0_S0"].GreatestWins![1].OpponentRating);
        Assert.AreEqual(1550, result.stats["B0_S0"].GreatestWins![2].OpponentRating);
        Assert.AreEqual(1540, result.stats["B0_S0"].GreatestWins![3].OpponentRating);
        Assert.AreEqual(1530, result.stats["B0_S0"].GreatestWins![4].OpponentRating);
    }

    [TestMethod]
    public void TestNoGreatestWinsUpdateForProvisionalWin()
    {
        // Arrange
        var uid = "p1"; // is black

        var oldUserStats = GetProgressedUserStat(null, GetBasicGreatestWins());

        var game = WinAgainstProvisiona1550Playero();

        var statCalculator = new StatCalculator();

        // Act
        var result = statCalculator.CalculateUserStat(oldUserStats, game);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.stats.ContainsKey("B0_S0"));

        Assert.IsNotNull(result.stats["B0_S0"].GreatestWins);

        Assert.AreEqual(5, result.stats["B0_S0"].GreatestWins!.Count);

        Assert.AreEqual(1600, result.stats["B0_S0"].GreatestWins![0].OpponentRating);
        Assert.AreEqual(1580, result.stats["B0_S0"].GreatestWins![1].OpponentRating);
        Assert.AreEqual(1540, result.stats["B0_S0"].GreatestWins![2].OpponentRating);
        Assert.AreEqual(1530, result.stats["B0_S0"].GreatestWins![3].OpponentRating);
        Assert.AreEqual(1510, result.stats["B0_S0"].GreatestWins![4].OpponentRating);
    }


    [TestMethod]
    private void TestHighestRatingUpdate()
    {
        // Arrange
        var uid = "p1"; // is black

        var oldUserStats = GetProgressedUserStat(null, null);

        var game = BlackWonGame9x9BlitzGameOnlyCountsShouldChange();

        var statCalculator = new StatCalculator();

        // Act
        var result = statCalculator.CalculateUserStat(oldUserStats, game);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.stats.ContainsKey("B0_S0"));

        Assert.AreEqual(1600, result.stats["B0_S0"].HighestRating);
    }



    private static Game BlackWonAgainst1550Player()
    {
        return MasterGame1();
    }


    private static Game BlackWonGame9x9BlitzGameOnlyCountsShouldChange()
    {
        return MasterGame0();
    }

    private static Game WinAgainstProvisiona1550Playero()
    {
        return MasterGame2();
    }

    private static Game MasterGame2()
    {
        return new Game(
            players: new Dictionary<string, StoneType>
            {
                ["p1"] = StoneType.Black,
                ["p2"] = StoneType.White
            },
            result: GameResult.BlackWon,
            endTime: _1980Jan1_1_30PM.AddMinutes(30).SerializedDate(),
            startTime: _1980Jan1_1_30PM.SerializedDate(),


            timeControl: new TimeControl(null, null, 1, TimeStandard.Blitz), // doesn't matter
            moves: [], // doesn't matter
            playgroundMap: [], // doesn't matter
            prisoners: [0, 0], // doesn't matter
            koPositionInLastMove: null, // doesn't matter
            komi: 0, // doesn't matter
            gameCreator: "p1", // doesn't matter
            gameId: "gameId", // doesn't matter
            deadStones: [], // doesn't matter
            finalTerritoryScores: [], // doesn't matter

            gameState: GameState.Ended,
            gameOverMethod: GameOverMethod.Resign,
            gameType: GameType.Rated,
            stoneSelectionType: StoneSelectionType.Black,
            rows: 9,
            columns: 9,
            playerTimeSnapshots: [],
            playersRatingsAfter: ["1600", "1545?"],
            playersRatingsBefore: ["1590", "1550?"]
        );
    }



    private static Game MasterGame0()
    {
        return new Game(
            players: new Dictionary<string, StoneType>
            {
                ["p1"] = StoneType.Black,
                ["p2"] = StoneType.White
            },
            result: GameResult.BlackWon,
            endTime: _1980Jan1_1_30PM.AddMinutes(30).SerializedDate(),
            startTime: _1980Jan1_1_30PM.SerializedDate(),


            timeControl: new TimeControl(null, null, 1, TimeStandard.Blitz), // doesn't matter
            moves: [], // doesn't matter
            playgroundMap: [], // doesn't matter
            prisoners: [0, 0], // doesn't matter
            koPositionInLastMove: null, // doesn't matter
            komi: 0, // doesn't matter
            gameCreator: "p1", // doesn't matter
            gameId: "gameId", // doesn't matter
            deadStones: [], // doesn't matter
            finalTerritoryScores: [], // doesn't matter

            gameState: GameState.Ended,
            gameOverMethod: GameOverMethod.Resign,
            gameType: GameType.Rated,
            stoneSelectionType: StoneSelectionType.Black,
            rows: 9,
            columns: 9,
            playerTimeSnapshots: [],
            playersRatingsAfter: ["1500", "1445"],
            playersRatingsBefore: ["1490", "1450"]
        );
    }



    private static Game MasterGame1()
    {
        return new Game(
            players: new Dictionary<string, StoneType>
            {
                ["p1"] = StoneType.Black,
                ["p2"] = StoneType.White
            },
            result: GameResult.BlackWon,
            endTime: _1980Jan1_1_30PM.AddMinutes(30).SerializedDate(),
            startTime: _1980Jan1_1_30PM.SerializedDate(),


            timeControl: new TimeControl(null, null, 1, TimeStandard.Blitz), // doesn't matter
            moves: [], // doesn't matter
            playgroundMap: [], // doesn't matter
            prisoners: [0, 0], // doesn't matter
            koPositionInLastMove: null, // doesn't matter
            komi: 0, // doesn't matter
            gameCreator: "p1", // doesn't matter
            gameId: "gameId", // doesn't matter
            deadStones: [], // doesn't matter
            finalTerritoryScores: [], // doesn't matter

            gameState: GameState.Ended,
            gameOverMethod: GameOverMethod.Resign,
            gameType: GameType.Rated,
            stoneSelectionType: StoneSelectionType.Black,
            rows: 9,
            columns: 9,
            playerTimeSnapshots: [],
            playersRatingsAfter: ["1500", "1545"],
            playersRatingsBefore: ["1490", "1550"]
        );
    }

    List<GameResultStat> GetBasicGreatestWins()
    {
        return new List<GameResultStat>
        {
            new GameResultStat(1600,"op1" , _1980Jan1_1_30PM.AddYears(-1), "g1"),
            new GameResultStat(1580,"op2" , _1980Jan1_1_30PM.AddYears(-1), "g1"),
            new GameResultStat(1540,"op3" , _1980Jan1_1_30PM.AddYears(-1), "g1"),
            new GameResultStat(1530,"op4" , _1980Jan1_1_30PM.AddYears(-1), "g1"),
            new GameResultStat(1510,"op5" , _1980Jan1_1_30PM.AddYears(-1), "g1")
        };
    }

    private static UserStat GetEmptyUserStat()
    {
        return new UserStat("p1", []);
    }

    private static UserStat GetProgressedUserStat(ResultStreakData? customStreak, List<GameResultStat>? customGreatestWins)
    {
        return new UserStat("p1", new Dictionary<string, UserStatForVariant>
        {
            ["B0_S0"] = new UserStatForVariant(
                highestRating: 1590,
                lowestRating: 800,
                resultStreakData: customStreak,
                playTime: 0,
                greatestWins: customGreatestWins,
                statCounts: new GameStatCounts(5, 5, 5, 5, 5)
            )
        });
    }

    Streak Get12To10MonthOldStreakLen5()
    {
        return new Streak(5, _1980Jan1_1_30PM.AddYears(-1), _1980Jan1_1_30PM.AddYears(-1).AddMonths(2));
    }

    Streak Get2To0MonthOldStreakLen5()
    {
        return new Streak(5, _1980Jan1_1_30PM.AddMonths(-2), _1980Jan1_1_30PM);
    }


    static public DateTime _1980Jan1_1_30PM = new DateTime(
        year: 1980,
        month: 1,
        day: 1,
        hour: 13,
        minute: 30,
        second: 0
    );
}