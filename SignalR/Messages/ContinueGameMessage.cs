using BadukServer;

[Immutable, GenerateSerializer]
public class ContinueGameMessage
{
    public Game Game { get; set; }

    public ContinueGameMessage(Game game)
    {
        Game = game;
    }
}