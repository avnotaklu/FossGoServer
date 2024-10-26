using BadukServer;

namespace Tests;

[TestClass]
public class StoneLogicTest
{
    [TestMethod]
    public void TestMethod1()
    {
        int[,] freedoms = {
            { 0, 0, 0, 2, 0, },
            { 0, 0, 1, 0, 2, },
            { 0, 0, 0, 3, 0, },
            { 0, 0, 0, 0, 0, },
            { 0, 0, 0, 0, 0, },
        };
        int[,] board = {
            { 0, 0, 0, 1, 0, },
            { 0, 0, 1, 1, 1, },
            { 0, 0, 0, 1, 0, },
            { 0, 0, 0, 0, 0, },
            { 0, 0, 0, 0, 0, },
        };

    }

    public BoardState ConstructBoard(int[,] board, int turn)
    {
        return new BoardState(
            rows: board.GetLength(0),
            cols: board.GetLength(1),
            null,
            playgroundMap:
            Enumerable.Range(0, board.GetLength(0))
            .SelectMany(row => Enumerable.Range(0, board.GetLength(1))
            .Select(col => (new Position(row, col), board[row, col] - 1))).ToDictionary(e => e.Item1, e => e.Item2 == -1 ? null : new Stone(e.Item1, e.Item2, new Cluster(
               [], 4, e.Item2
            ))),
            prisoners: [0, 0]
        );

    }
}