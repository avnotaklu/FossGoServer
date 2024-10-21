namespace BadukServer;

public class GameJoinDto
{
    public GameJoinDto(string gameId)
    {
        GameId = gameId;
    }

    public string GameId { get; set; }
}