using BadukServer;

[Immutable, GenerateSerializer]
class NewGameCreatedMessage
{
    public Game Game { get; set; }
    public NewGameCreatedMessage(Game game)
    {
        Game = game;
    }
}