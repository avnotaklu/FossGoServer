namespace BadukServer;

public class GameCreationDto
{
    public GameCreationDto(int rows, int columns, int timeInSeconds)
    {
        Rows = rows;
        Columns = columns;
        TimeInSeconds = timeInSeconds;
    }

    public int Rows { get; set; }
    public int Columns { get; set; }
    public int TimeInSeconds { get; set; }
}