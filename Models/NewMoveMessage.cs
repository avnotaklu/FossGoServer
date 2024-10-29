using BadukServer;

class NewMoveMessage
{
    public Game game { get; set; }

    public NewMoveMessage(Game game)
    {
        this.game = game;
    }
}