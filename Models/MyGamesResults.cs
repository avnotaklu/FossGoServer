
using BadukServer;

public class MyGameData
{
    public Game Game { get; set; }
    public PlayerInfo? OpposingPlayer { get; set; }

    public MyGameData(Game game, PlayerInfo? opposingPlayer)
    {
        Game = game;
        OpposingPlayer = opposingPlayer;
    }

}

public class MyGamesResult
{
    public List<MyGameData> Games { get; set; }

    public MyGamesResult(List<MyGameData> games)
    {
        Games = games;
    }
}