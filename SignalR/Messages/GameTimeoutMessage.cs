using BadukServer;

public class GameTimeoutMessage
{
    public Game Game;

    public GameTimeoutMessage(Game game)
    {
        Game = game;
    }
}