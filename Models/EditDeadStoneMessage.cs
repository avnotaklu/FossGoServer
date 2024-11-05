using BadukServer;

[Immutable, GenerateSerializer]
public class EditDeadStoneMessage
{
    public RawPosition Position { get; set; }
    public DeadStoneState State { get; set; }
    public Game Game {get; set;}

    public EditDeadStoneMessage(RawPosition position, DeadStoneState state, Game game)
    {
        Position = position;
        State = state;
        Game = game;
    }
}