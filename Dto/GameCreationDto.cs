namespace BadukServer;

public class GameCreationDto
{
    public GameCreationDto(int rows, int columns, TimeControl timeControl, StoneSelectionType firstPlayerStone)
    {
        Rows = rows;
        Columns = columns;
        TimeControl = timeControl;
        FirstPlayerStone = firstPlayerStone;
    }

    public int Rows { get; set; }
    public int Columns { get; set; }
    public StoneSelectionType FirstPlayerStone { get; set; }
    public TimeControl TimeControl { get; set; }
}


[GenerateSerializer]
public enum StoneSelectionType
{
    Black = 0,
    White = 1,
    Auto = 1,
}
