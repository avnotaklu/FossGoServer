using BadukServer;

[GenerateSerializer]
public enum GameOverMethod
{
    Timeout,
    Resign,
    Score,
    Abandon,

}

[Immutable, GenerateSerializer]
public class GameOverMessage
{
    public GameOverMethod Method { get; set; }
    public Game Game { get; set; }

    public GameOverMessage(GameOverMethod method, Game game)
    {
        Method = method;
        Game = game;
    }
}