using BadukServer;

public class AvailableGameData
{
    public Game Game { get; set; }
    public PlayerInfo CreatorInfo { get; set; }

    public AvailableGameData(Game game, PlayerInfo creatorInfo)
    {
        Game = game;
        CreatorInfo = creatorInfo;
    }

}

public class AvailableGamesResult
{
    public List<AvailableGameData> Games { get; set; }

    public AvailableGamesResult(List<AvailableGameData> games)
    {
        Games = games;
    }
}