using BadukServer;

[Immutable]
[GenerateSerializer]
public class GameJoinResult
{
    public PlayerInfo? OtherPlayerData { get; set; }
    public Game Game { get; set; }
    public DateTime JoinTime { get; set; }

    public GameJoinResult(Game game, PlayerInfo? otherPlayerData,  DateTime time)
    {
        Game = game;
        OtherPlayerData = otherPlayerData;
        JoinTime = time;
    }
}