using BadukServer;

[Immutable]
[GenerateSerializer]
public class GameEntranceData
{
    public PlayerInfo? OtherPlayerData { get; set; }
    public Game Game { get; set; }
    public DateTime? JoinTime { get; set; } // The creator can renter their own games even when both players haven't joined, so this is nullable

    public GameEntranceData(Game game, PlayerInfo? otherPlayerData, DateTime? time)
    {
        Game = game;
        OtherPlayerData = otherPlayerData;
        JoinTime = time;
    }
}