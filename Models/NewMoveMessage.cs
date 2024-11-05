using BadukServer;

class NewMoveMessage
{
    public Game Game { get; set; }

    public NewMoveMessage(Game game)
    {
        Game = game;
    }
}