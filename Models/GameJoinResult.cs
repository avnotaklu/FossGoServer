using BadukServer;

[Immutable]
[GenerateSerializer]
public class GameJoinResult
{
    public PublicUserInfo? OtherPlayerData { get; set; }
    public Game Game { get; set; }
    public string Time { get; set; }

    public GameJoinResult(Game game, PublicUserInfo? otherPlayerData,  string time)
    {
        Game = game;
        OtherPlayerData = otherPlayerData;
        Time = time;
    }
}