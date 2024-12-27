using BadukServer;

[Immutable]
[GenerateSerializer]
public class GameJoinMessage
{
    public PlayerInfo? OtherPlayerData { get; set; }
    public Game Game { get; set; }
    public DateTime JoinTime { get; set; } // This is the initial time the players entered, start of game is [GameGrain.startDelay] seconds after this

    public GameJoinMessage(Game game, PlayerInfo? otherPlayerData,  DateTime time)
    {
        Game = game;
        OtherPlayerData = otherPlayerData;
        JoinTime = time;
    }
}