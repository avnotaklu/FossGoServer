public class GameHistoryBatch
{
    public List<GameAndOpponent> Games { get; set; }
    public GameHistoryBatch(List<GameAndOpponent> games)
    {
        Games = games;
    }
}