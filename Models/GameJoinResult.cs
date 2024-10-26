using BadukServer;

[Immutable]
[GenerateSerializer]
public class GameJoinResult
{
    public List<PublicUserInfo> Players { get; set; }
    public Game Game { get; set; }
    public string Time { get; set; }

    public GameJoinResult(Game game, List<PublicUserInfo> players, string time)
    {
        Game = game;
        Players = players;
        Time = time;
    }
}