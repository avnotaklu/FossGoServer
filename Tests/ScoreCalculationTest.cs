using BadukServer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests;
[TestClass]
public class ScoreCalculationTest
{
    [TestMethod]
    public void TestSimpleScoreCalculation()
    {
        var board = new int[,] {
            { 0, 0, 0, 0, 0, },
            { 0, 0, 0, 0, 1, },
            { 0, 0, 0, 0, 0, },
            { 0, 0, 0, 0, 0, },
            { 0, 0, 0, 0, 0, },
        };

        var rows = 5;
        var cols = 5;
        var boardCons = new BoardStateUtilities();
        var clusters = boardCons.GetClusters(board, new BoardSizeParams(rows, cols));
        var stones = boardCons.GetStones(clusters);
        var boardState = boardCons.ConstructBoard(rows, cols, stones);
        // var stoneLogic = new StoneLogic(boardState);

        // var movePos = new Position(0, 4);
        // var updateResult = stoneLogic.HandleStoneUpdate(movePos, 0);

        // Assert.AreEqual(true, updateResult.result);

        var scoreCalc = new ScoreCalculation(
            rows,
            cols,
            6.5f,
            [0, 0],
            [],
            boardState.playgroundMap
        );

        var score = scoreCalc.Score;

        Assert.AreEqual(25, score[0]);
        Assert.AreEqual(0, score[1]);
    }


    [TestMethod]
    public void TestSimpleScoreCalculation2()
    {
        var board = new int[,] {
            { 0, 0, 0, 1, 0, },
            { 0, 0, 1, 1, 1, },
            { 0, 0, 2, 1, 0, },
            { 0, 2, 0, 2, 0, },
            { 0, 0, 2, 0, 0, },
        };

        var rows = 5;
        var cols = 5;
        var boardCons = new BoardStateUtilities();
        var clusters = boardCons.GetClusters(board, new BoardSizeParams(rows, cols));
        var stones = boardCons.GetStones(clusters);
        var boardState = boardCons.ConstructBoard(rows, cols, stones);
        // var stoneLogic = new StoneLogic(boardState);

        // var movePos = new Position(0, 4);
        // var updateResult = stoneLogic.HandleStoneUpdate(movePos, 0);

        // Assert.AreEqual(true, updateResult.result);

        var scoreCalc = new ScoreCalculation(
            rows,
            cols,
            6.5f,
            [0, 0],
            [],
            boardState.playgroundMap
        );

        var score = scoreCalc.Score;

        Assert.AreEqual(6, score[0]);
        Assert.AreEqual(5, score[1]);
    }

    [TestMethod]
    public void TestSekiScoreCalculation2()
    {
        var board = new int[,] {
            { 1, 0, 2, 0, 2, 0 },
            { 0, 1, 2, 2, 2, 0 },
            { 2, 2, 1, 1, 1, 1 },
            { 0, 2, 1, 0, 0, 0 },
            { 2, 2, 1, 0, 0, 0 },
            { 1, 1, 1, 0, 0, 0 },
        };

        var rows = 6;
        var cols = 6;
        var boardCons = new BoardStateUtilities();
        var clusters = boardCons.GetClusters(board, new BoardSizeParams(rows, cols));
        var stones = boardCons.GetStones(clusters);
        var boardState = boardCons.ConstructBoard(rows, cols, stones);
        // var stoneLogic = new StoneLogic(boardState);

        // var movePos = new Position(0, 4);
        // var updateResult = stoneLogic.HandleStoneUpdate(movePos, 0);

        // Assert.AreEqual(true, updateResult.result);

        var scoreCalc = new ScoreCalculation(
            rows,
            cols,
            6.5f,
            [0, 0],
            [],
            boardState.playgroundMap
        );

        var score = scoreCalc.Score;

        Assert.AreEqual(20, score[0]);
        Assert.AreEqual(12, score[1]);
    }
}