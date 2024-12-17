using System.Security.Cryptography;
using BadukServer;

namespace Tests;

[TestClass]
public class StatCalculatorTest
{
    static string gameKey = "0_0";

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
        Assert.IsTrue(result.Stats.ContainsKey(gameKey));

        Assert.AreEqual(1, result.Stats[gameKey].StatCounts.Total);
        Assert.AreEqual(1, result.Stats[gameKey].StatCounts.Wins);
        Assert.AreEqual(0, result.Stats[gameKey].StatCounts.Losses);
        Assert.AreEqual(0, result.Stats[gameKey].StatCounts.Draws);
        Assert.AreEqual(0, result.Stats[gameKey].StatCounts.Disconnects);
    }


    [TestMethod]
    public void TestUpdatedUserStatCountsAndNoOps()
    {
        // Arrange
        var uid = "p1"; // is black

        var oldUserStats = GetProgressedUserStat(null, null);

        var oldHR = oldUserStats.Stats[gameKey].HighestRating;
        var oldLR = oldUserStats.Stats[gameKey].LowestRating;

        var game = BlackWonGame9x9BlitzGameOnlyCountsShouldChange();

        var statCalculator = new StatCalculator();

        // Act
        var result = statCalculator.CalculateUserStat(oldUserStats, game);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Stats.ContainsKey(gameKey));


        // Counts
        Assert.AreEqual(6, result.Stats[gameKey].StatCounts.Total);
        Assert.AreEqual(6, result.Stats[gameKey].StatCounts.Wins);
        Assert.AreEqual(5, result.Stats[gameKey].StatCounts.Losses);
        Assert.AreEqual(5, result.Stats[gameKey].StatCounts.Draws);
        Assert.AreEqual(5, result.Stats[gameKey].StatCounts.Disconnects);

        // No Ops

        Assert.AreEqual(oldHR, result.Stats[gameKey].HighestRating);
        Assert.AreEqual(oldLR, result.Stats[gameKey].LowestRating);
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
        Assert.IsTrue(result.Stats.ContainsKey(gameKey));

        Assert.IsNotNull(result.Stats[gameKey].ResultStreakData);
        Assert.AreEqual(6, result.Stats[gameKey].ResultStreakData!.WinningStreaks!.CurrentStreak!.StreakLength);
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
        Assert.IsTrue(result.Stats.ContainsKey(gameKey));

        Assert.IsNotNull(result.Stats[gameKey].ResultStreakData);
        Assert.AreEqual(6, result.Stats[gameKey].ResultStreakData!.WinningStreaks!.GreatestStreak!.StreakLength);
        Assert.AreEqual(curS.StreakFrom, result.Stats[gameKey].ResultStreakData!.WinningStreaks!.GreatestStreak!.StreakFrom);
        Assert.AreEqual(game.Game.EndTime!, result.Stats[gameKey].ResultStreakData!.WinningStreaks!.GreatestStreak!.StreakTo);
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
        Assert.IsTrue(result.Stats.ContainsKey(gameKey));

        Assert.IsNotNull(result.Stats[gameKey].GreatestWins);

        Assert.AreEqual(5, result.Stats[gameKey].GreatestWins!.Count);

        Assert.AreEqual(1600, result.Stats[gameKey].GreatestWins![0].OpponentRating);
        Assert.AreEqual(1580, result.Stats[gameKey].GreatestWins![1].OpponentRating);
        Assert.AreEqual(1550, result.Stats[gameKey].GreatestWins![2].OpponentRating);
        Assert.AreEqual(1540, result.Stats[gameKey].GreatestWins![3].OpponentRating);
        Assert.AreEqual(1530, result.Stats[gameKey].GreatestWins![4].OpponentRating);
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
        Assert.IsTrue(result.Stats.ContainsKey(gameKey));

        Assert.IsNotNull(result.Stats[gameKey].GreatestWins);

        Assert.AreEqual(5, result.Stats[gameKey].GreatestWins!.Count);

        Assert.AreEqual(1600, result.Stats[gameKey].GreatestWins![0].OpponentRating);
        Assert.AreEqual(1580, result.Stats[gameKey].GreatestWins![1].OpponentRating);
        Assert.AreEqual(1540, result.Stats[gameKey].GreatestWins![2].OpponentRating);
        Assert.AreEqual(1530, result.Stats[gameKey].GreatestWins![3].OpponentRating);
        Assert.AreEqual(1510, result.Stats[gameKey].GreatestWins![4].OpponentRating);
    }


    [TestMethod]
    public void TestHighestRatingUpdate()
    {
        // Arrange
        var uid = "p1"; // is black


        var (oldUserStats, game) = BlackWonGame9x9BlitzGameAlsoHisGreatestWin();

        var statCalculator = new StatCalculator();

        // Act
        var result = statCalculator.CalculateUserStat(oldUserStats, game);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Stats.ContainsKey(gameKey));

        Assert.AreEqual(1600, result.Stats[gameKey].HighestRating);
    }

    [TestMethod]
    public void TestLowestRatingUpdate()
    {
        // Arrange
        var uid = "p2"; // is white

        var (oldUserStats, game) = WhiteLostGame9x9BlitzGameAlsoHisGreatestLoss();

        var statCalculator = new StatCalculator();

        // Act
        var result = statCalculator.CalculateUserStat(oldUserStats, game);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Stats.ContainsKey(gameKey));

        Assert.AreEqual(1445, result.Stats[gameKey].LowestRating);
    }

    private static GamePlayersAggregate BlackWonAgainst1550Player()
    {
        return MasterGame1();
    }

    private static GamePlayersAggregate BlackWonGame9x9BlitzGameOnlyCountsShouldChange()
    {
        return MasterGame0();
    }


    private static (UserStat, GamePlayersAggregate) WhiteLostGame9x9BlitzGameAlsoHisGreatestLoss()
    {
        var oldUserStats = GetProgressedUserStatForLosingPlayer(null, null);
        return (oldUserStats, MasterGame0());
    }


    private static (UserStat, GamePlayersAggregate) BlackWonGame9x9BlitzGameAlsoHisGreatestWin()
    {
        var oldUserStats = GetProgressedUserStat(null, null);
        return (oldUserStats, MasterGame2());
    }


    private static GamePlayersAggregate WinAgainstProvisiona1550Playero()
    {
        return MasterGame2();
    }

    private static GamePlayersAggregate MasterGame2()
    {
        var game = new Game(
            players: ["p1", "p2"],
            usernames: ["u1", "u2"],
            result: GameResult.BlackWon,
            endTime: _1980Jan1_1_30PM.AddMinutes(30),
            startTime: _1980Jan1_1_30PM,
            creationDate: _1980Jan1_1_30PM,


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
            playersRatingsAfter: ["1600", "1565?"],
            playersRatingsDiff: [-10, 15]
        );

        List<PlayerInfo> players = [
            GetSamplePlayerInfo(1, game.GetTopLevelVariant(), game.PlayersRatingsAfter[0]),
            GetSamplePlayerInfo(2, game.GetTopLevelVariant(), game.PlayersRatingsAfter[1]),
        ];

        return new GamePlayersAggregate(
            game: game,
            players: players
        );
    }



    private static GamePlayersAggregate MasterGame0()
    {
        var game = new Game(
            players: ["p1", "p2"],
            usernames: ["u1", "u2"],
            result: GameResult.BlackWon,
            endTime: _1980Jan1_1_30PM.AddMinutes(30),
            startTime: _1980Jan1_1_30PM,
            creationDate: _1980Jan1_1_30PM,

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
            playersRatingsDiff: [10, -5]
        );


        List<PlayerInfo> players = [
            GetSamplePlayerInfo(1, game.GetTopLevelVariant(), game.PlayersRatingsAfter[0]),
            GetSamplePlayerInfo(2, game.GetTopLevelVariant(), game.PlayersRatingsAfter[1]),
        ];

        return new GamePlayersAggregate(
            game: game,
            players: players
        );
    }



    private static GamePlayersAggregate MasterGame1()
    {
        var game = new Game(
            players: ["p1", "p2"],
            usernames: ["u1", "u2"],
            result: GameResult.BlackWon,
            endTime: _1980Jan1_1_30PM.AddMinutes(30),
            startTime: _1980Jan1_1_30PM,
            creationDate: _1980Jan1_1_30PM,


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
            playersRatingsDiff: [10, -5]
        );


        List<PlayerInfo> players = [
            GetSamplePlayerInfo(1, game.GetTopLevelVariant(), game.PlayersRatingsAfter[0]),
            GetSamplePlayerInfo(2, game.GetTopLevelVariant(), game.PlayersRatingsAfter[1]),
        ];

        return new GamePlayersAggregate(
            game: game,
            players: players
        );
    }

    List<GameResultStat> GetBasicGreatestWins()
    {
        return new List<GameResultStat>
        {
            new GameResultStat(1600, _1980Jan1_1_30PM.AddYears(-1), "g1","op1" , "op1Name1"),
            new GameResultStat(1580, _1980Jan1_1_30PM.AddYears(-1), "g1","op2" , "op1Name2"),
            new GameResultStat(1540, _1980Jan1_1_30PM.AddYears(-1), "g1","op3" , "op1Name3"),
            new GameResultStat(1530, _1980Jan1_1_30PM.AddYears(-1), "g1","op4" , "op1Name4"),
            new GameResultStat(1510, _1980Jan1_1_30PM.AddYears(-1), "g1","op5" , "op1Name5")
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
            [gameKey] = new UserStatForVariant(
                highestRating: 1590,
                lowestRating: 800,
                resultStreakData: customStreak,
                playTime: 0,
                greatestWins: customGreatestWins,
                statCounts: new GameStatCounts(5, 5, 5, 5, 5)
            )
        });
    }

    private static UserStat GetProgressedUserStatForLosingPlayer(ResultStreakData? customStreak, List<GameResultStat>? customGreatestWins)
    {
        return new UserStat("p2", new Dictionary<string, UserStatForVariant>
        {
            [gameKey] = new UserStatForVariant(
                highestRating: 1590,
                lowestRating: 1450,
                resultStreakData: customStreak,
                playTime: 0,
                greatestWins: customGreatestWins,
                statCounts: new GameStatCounts(5, 5, 5, 5, 5)
            )
        });
    }


    Streak Get12To10MonthOldStreakLen5()
    {
        return new Streak(5, _1980Jan1_1_30PM.AddYears(-1), _1980Jan1_1_30PM.AddYears(-1).AddMonths(2), "g1", "g2");
    }

    Streak Get2To0MonthOldStreakLen5()
    {
        return new Streak(5, _1980Jan1_1_30PM.AddMonths(-2), _1980Jan1_1_30PM, "g1", "g2");
    }


    static public DateTime _1980Jan1_1_30PM = new DateTime(
        year: 1980,
        month: 1,
        day: 1,
        hour: 13,
        minute: 30,
        second: 0
    );

    static public PlayerInfo GetSamplePlayerInfo(int id, ConcreteGameVariant variant, String ratingString)
    {
        var rating = MinimalRatingExt.FromString(ratingString);

        return new PlayerInfo(
            id: id.ToString(),
            username: $"username{id}",
            rating: new PlayerRatings(
                userId: id.ToString(),
                ratings: new Dictionary<string, PlayerRatingsData>
                {
                    [variant.ToKey()] = new PlayerRatingsData
                    (
                        glicko: new GlickoRating(rating.Rating, rating.Provisional ? 200 : 80, 0.06),
                        nb: 2,
                        recent: [],
                        latest: null
                    )
                }
            ),
            playerType: PlayerType.Normal
        );
    }
}