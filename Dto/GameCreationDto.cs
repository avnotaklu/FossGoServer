namespace BadukServer;

public class GameCreationDto
{
    public GameCreationDto(int rows, int columns, TimeControl timeControl, StoneType firstPlayerStone)
    {
        Rows = rows;
        Columns = columns;
        TimeControl = timeControl;
        FirstPlayerStone = firstPlayerStone;
    }

    public int Rows { get; set; }
    public int Columns { get; set; }
    public StoneType FirstPlayerStone { get; set; }
    public TimeControl TimeControl { get; set; }
}

