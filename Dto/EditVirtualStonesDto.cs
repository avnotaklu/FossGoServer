using BadukServer;


[GenerateSerializer]
public enum DeadStoneState
{
    Dead,
    Alive
}

[Immutable, GenerateSerializer]
public class EditDeadStoneClusterDto
{
    public RawPosition Position { get; set; }
    public DeadStoneState State { get; set; }

    public EditDeadStoneClusterDto(DeadStoneState state, RawPosition position)
    {
        State = state;
        Position = position;
    }
}

[Immutable, GenerateSerializer]
public class RawPosition
{
    public int X { get; set; }
    public int Y { get; set; }

    public RawPosition(int x, int y)
    {
        X = x;
        Y = y;
    }
}