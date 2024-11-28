using BadukServer;


public enum DeadStoneState
{
    Dead = 0,
    Alive = 1
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

public static class RawPositionExtensions
{
    public static Position ToGamePosition(this RawPosition position)
    {
        return new Position(position.X, position.Y);
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