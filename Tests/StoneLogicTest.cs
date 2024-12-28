// using BadukServer;

using BadukServer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests;

[TestClass]
public class StoneLogicTest
{
    int[,] _5x5BasicBoard()
    {
        int[,] board = {
            { 0, 0, 0, 1, 0, },
            { 0, 0, 1, 1, 1, },
            { 0, 0, 0, 1, 0, },
            { 0, 0, 0, 0, 0, },
            { 0, 0, 0, 0, 0, },
        };
        return board;
    }

    new Dictionary<string, StoneType> _5x5BasicBoardHighLevelRepr()
    {
        var board = new Dictionary<string, StoneType> {
            { "0 3" , StoneType.Black},
            { "1 3", StoneType.Black},
            { "2 3", StoneType.Black},
            { "1 2", StoneType.Black},
            { "1 4", StoneType.Black}
        };
        return board;
    }


    int[,] _5x5DeathBoard()
    {
        int[,] board = {
            { 0, 0, 2, 1, 0, },
            { 0, 2, 0, 2, 1, },
            { 0, 0, 2, 1, 0, },
            { 0, 0, 0, 0, 0, },
            { 0, 0, 0, 0, 0, },
        };
        return board;
    }

    // Position from https://senseis.xmp.net/?Cycles
    int[,] _3_move_cycle_6x6_DeathBoard()
    {
        int[,] board = {
            { 0, 1, 0, 2, 1, 0 },
            { 2, 1, 2, 2, 1, 0 },
            { 0, 2, 2, 1, 1, 0 },
            { 2, 2, 1, 0, 0, 0 },
            { 1, 1, 1, 0, 1, 0 },
            { 0, 0, 0, 0, 0, 0 },
        };
        return board;
    }


    [TestMethod]
    public void TestGetClusters()
    {
        var board = _5x5BasicBoard();
        var boardCons = new BoardStateUtilities();
        var clusters = boardCons.GetClusters(board, new BoardSizeParams(5, 5));

        Assert.AreEqual(1, clusters.Count);
        Assert.AreEqual(5, clusters.First().data.Count);
        // Console.WriteLine("Freedoms: ", clusters.First().FreedomPositions.Count);
        Assert.AreEqual(6, clusters.First().freedoms);
        Assert.AreEqual(0, clusters.First().player);
    }

    [TestMethod]
    public void TestGetStones()
    {
        var board = _5x5BasicBoard();
        var boardCons = new BoardStateUtilities();
        var clusters = boardCons.GetClusters(board, new BoardSizeParams(5, 5));
        var stones = boardCons.GetStones(clusters);

        Assert.AreEqual(5, stones.Count);
    }

    [TestMethod]
    public void TestAddition()
    {
        var board = _5x5BasicBoard();
        var boardCons = new BoardStateUtilities();
        var clusters = boardCons.GetClusters(board, new BoardSizeParams(5, 5));
        var stones = boardCons.GetStones(clusters);
        var stoneLogic = new StoneLogic(boardCons.ConstructBoard(5, 5, stones));

        var movePos = new Position(0, 4);
        var updateResult = stoneLogic.HandleStoneUpdate(movePos, 0);

        Assert.AreEqual(true, updateResult.result);

        Assert.IsTrue(updateResult.board.playgroundMap.ContainsKey(movePos));
        Assert.AreEqual(updateResult.board.playgroundMap.Count(), 6);
        // Assert.AreEqual(updateResult.board.playgroundMap[new Position(3, 1)].cluster.freedoms, 5);
        // Assert.AreEqual(updateResult.board.playgroundMap[new Position(3, 0)].cluster.data.Count, 5);
        Assert.AreEqual(updateResult.board.playgroundMap[movePos].cluster.data.Count, 6);
        Assert.AreEqual(updateResult.board.playgroundMap[movePos].cluster.freedoms, 5);
    }

    [TestMethod]
    public void TestUnplayableDueToNoFreedoms()
    {
        var board = _5x5BasicBoard();
        var boardCons = new BoardStateUtilities();
        var clusters = boardCons.GetClusters(board, new BoardSizeParams(5, 5));
        var stones = boardCons.GetStones(clusters);
        var stoneLogic = new StoneLogic(boardCons.ConstructBoard(5, 5, stones));

        var movePos = new Position(0, 4);
        var updateResult = stoneLogic.HandleStoneUpdate(movePos, 1);

        Assert.AreEqual(false, updateResult.result);
        Assert.IsFalse(updateResult.board.playgroundMap.ContainsKey(movePos));
        Assert.AreEqual(updateResult.board.playgroundMap.Count(), 5);
    }

    [TestMethod]
    public void TestKillStone()
    {
        var board = _5x5DeathBoard();
        var boardCons = new BoardStateUtilities();
        var clusters = boardCons.GetClusters(board, new BoardSizeParams(5, 5));
        var stones = boardCons.GetStones(clusters);
        var boardState = boardCons.ConstructBoard(5, 5, stones);
        var stoneLogic = new StoneLogic(boardState);

        var movePos = new Position(1, 2);
        var killPosition = new Position(1, 3);

        Assert.IsTrue(boardState.playgroundMap.ContainsKey(killPosition));
        Assert.AreEqual(1, boardState.playgroundMap[killPosition].cluster.freedoms);

        var updateResult = stoneLogic.HandleStoneUpdate(movePos, 0);

        Assert.AreEqual(true, updateResult.result);
        Assert.IsTrue(updateResult.board.playgroundMap.ContainsKey(movePos));
        Assert.IsFalse(updateResult.board.playgroundMap.ContainsKey(killPosition));
        // Assert.AreEqual(0, boardState.playgroundMap[killPosition].cluster.freedoms);
        Assert.AreEqual(7, updateResult.board.playgroundMap.Count());
    }

    [TestMethod]
    public void TestKoInsert()
    {
        var board = _5x5DeathBoard();
        var boardCons = new BoardStateUtilities();
        var clusters = boardCons.GetClusters(board, new BoardSizeParams(5, 5));
        var stones = boardCons.GetStones(clusters);
        var boardState = boardCons.ConstructBoard(5, 5, stones);
        var stoneLogic = new StoneLogic(boardState);

        var movePos = new Position(1, 2);
        var killPosition = new Position(1, 3);

        var updateResult = stoneLogic.HandleStoneUpdate(movePos, 0);

        Assert.IsTrue(updateResult.result);
        Assert.AreEqual(killPosition, updateResult.board.koDelete);
        Assert.IsTrue(updateResult.board.playgroundMap.ContainsKey(movePos));
        Assert.IsFalse(updateResult.board.playgroundMap.ContainsKey(killPosition));

        var movePos2 = new Position(1, 3);
        var killPosition2 = new Position(1, 2);

        var updateResult2 = new StoneLogic(updateResult.board).HandleStoneUpdate(movePos2, 1);

        Assert.IsFalse(updateResult2.result);
        Assert.IsFalse(updateResult2.board.playgroundMap.ContainsKey(movePos2));
        Assert.IsTrue(updateResult2.board.playgroundMap.ContainsKey(killPosition2));
    }

    [TestMethod]
    public void TestSimpleBoardRepresentation()
    {
        var highLevelBoard = _5x5BasicBoardHighLevelRepr();
        var boardCons = new BoardStateUtilities();
        var result = boardCons.SimpleBoardRepresentation(highLevelBoard, new BoardSizeParams(5, 5));    

        Assert.IsTrue(_2DArrayEqual(result, _5x5BasicBoard()));
    }

    [TestMethod]
    public void TestThreeMoveCycle() {
        var pos = _3_move_cycle_6x6_DeathBoard();

        var boardCons = new BoardStateUtilities();
        var clusters = boardCons.GetClusters(pos, new BoardSizeParams(6, 6));
        var stones = boardCons.GetStones(clusters);
        var stoneLogic = new StoneLogic(boardCons.ConstructBoard(6, 6, stones));

        var m1 = new Position(0, 0);
        var m2 = new Position(2, 0);
        var m3 = new Position(1, 0);

        var r1 = stoneLogic.HandleStoneUpdate(m1, 1);

        Assert.IsTrue(r1.result);

        var r2 = stoneLogic.HandleStoneUpdate(m2, 0);

        Assert.IsTrue(r2.result);

        var r3 = stoneLogic.HandleStoneUpdate(m3, 1);

        Assert.IsFalse(r3.result);
    }

    private bool _2DArrayEqual<T>(T[,] array1, T[,] array2)
    {
        if (array1.GetLength(0) != array2.GetLength(0) || array1.GetLength(1) != array2.GetLength(1))
        {
            return false;
        }

        for (int i = 0; i < array1.GetLength(0); i++)
        {
            for (int j = 0; j < array1.GetLength(1); j++)
            {
                if (!Equals(array1[i, j], array2[i, j]))
                {
                    return false;
                }
            }
        }

        return true;
    }
}
