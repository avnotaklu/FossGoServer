using System.Diagnostics;
using BadukServer;

public static class PlayerResultExtensions
{
    public static GameResult WhenBlack(this PlayerResult result)
    {
        return result switch
        {
            PlayerResult.Won => GameResult.BlackWon,
            PlayerResult.Lost => GameResult.WhiteWon,
            PlayerResult.Draw => GameResult.Draw,
            PlayerResult.NoResult => GameResult.NoResult,
            _ => throw new UnreachableException("Player result not supported")
        };
    }

    public static GameResult WhenWhite(this PlayerResult result)
    {
        return result switch
        {
            PlayerResult.Won => GameResult.WhiteWon,
            PlayerResult.Lost => GameResult.BlackWon,
            PlayerResult.Draw => GameResult.Draw,
            PlayerResult.NoResult => GameResult.NoResult,
            _ => throw new UnreachableException("Player result not supported")
        };
    }
}


[GenerateSerializer]
public enum PlayerResult
{
    Won,
    Lost,
    Draw,
    NoResult
}
