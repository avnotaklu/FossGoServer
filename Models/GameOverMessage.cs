using BadukServer;

[GenerateSerializer]
public enum GameOverMethod
{
    Timeout = 0,
    Resign = 1,
    Score = 2,
    Abandon = 3,
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