using BadukServer;

[Immutable,  GenerateSerializer]
[Alias("GameStartMessage")]
public class GameStartMessage {
    public Game Game {get; set;}

    public GameStartMessage(Game game)
    {
        Game = game;
    }
}