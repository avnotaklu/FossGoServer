using BadukServer;

public class AvailableGameData
{
    public Game Game { get; set; }
    public PublicUserInfo CreatorInfo { get; set; }

    public AvailableGameData(Game game, PublicUserInfo creatorInfo)
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