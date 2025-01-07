using BadukServer;

public class GameTimerUpdateMessage
{
    public PlayerTimeSnapshot CurrentPlayerTime { get; set; }
    public StoneType Player { get; set; }


    public GameTimerUpdateMessage(PlayerTimeSnapshot currentPlayerTime, StoneType player)
    {
        CurrentPlayerTime = currentPlayerTime;
        Player = player;
    }
}