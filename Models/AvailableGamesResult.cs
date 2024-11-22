using BadukServer;

public class AvailableGameData
{
    public Game game { get; set; }
    public PublicUserInfo creatorInfo { get; set; }

    public AvailableGameData(Game game, PublicUserInfo creatorInfo)
    {
        this.game = game;
        this.creatorInfo = creatorInfo;
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