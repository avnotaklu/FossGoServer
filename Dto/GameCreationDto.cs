namespace BadukServer;

public class GameCreationDto
{
    public GameCreationDto(int rows, int columns, int timeInSeconds, Stone firstPlayerStone)
    {
        Rows = rows;
        Columns = columns;
        TimeInSeconds = timeInSeconds;
        FirstPlayerStone = firstPlayerStone;
    }

    public int Rows { get; set; }
    public int Columns { get; set; }
    public Stone FirstPlayerStone { get; set; }
    public int TimeInSeconds { get; set; }
}

