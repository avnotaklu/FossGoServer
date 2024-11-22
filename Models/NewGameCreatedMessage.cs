using BadukServer;

[Immutable, GenerateSerializer]
class NewGameCreatedMessage
{
    public AvailableGameData Game { get; set; }
    public NewGameCreatedMessage(AvailableGameData game)
    {
        Game = game;
    }
}