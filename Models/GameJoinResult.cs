using BadukServer;

[Immutable]
[GenerateSerializer]
public class GameJoinResult
{
    public PlayerInfo? OtherPlayerData { get; set; }
    public Game Game { get; set; }
    public string JoinTime { get; set; }

    public GameJoinResult(Game game, PlayerInfo? otherPlayerData,  string time)
    {
        Game = game;
        OtherPlayerData = otherPlayerData;
        JoinTime = time;
    }
}