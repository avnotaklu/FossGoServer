using BadukServer;
using BadukServer.Orleans.Grains;

public static class PlayerTypeExt
{
    public static PlayerType FromString(string type)
    {
        return type switch
        {
            "normal_user" => PlayerType.Normal,
            "guest_user" => PlayerType.Guest,
            _ => throw new Exception("Invalid player type")
        };
    }
    public static string ToTypeString(this PlayerType type)
    {
        return type switch
        {
            PlayerType.Normal => "normal_user",
            PlayerType.Guest => "guest_user",
            _ => throw new Exception("Invalid player type")
        };
    }

    public static GameType GetGameType(this PlayerType type, RankedOrCasual rankedOrCasual)
    {
        return type switch
        {
            PlayerType.Normal => rankedOrCasual switch
            {
                RankedOrCasual.Rated => GameType.Rated,
                RankedOrCasual.Casual => GameType.Casual,
                _ => throw new Exception("Invalid game type")
            },
            PlayerType.Guest => GameType.Anonymous,
            _ => throw new Exception("Invalid player type")
        };
    }
}

public enum PlayerType
{
    Normal = 0,
    Guest = 1
}

[Immutable]
[GenerateSerializer]
public class PlayerInfo
{
    public string Id { get; set; }
    public string? Email { get; set; }
    public PlayerRatings? Rating { get; set; }
    public PlayerType PlayerType { get; set; }

    public PlayerInfo(string id, string? email, PlayerRatings? rating, PlayerType playerType)
    {
        Email = email;
        Id = id;
        Rating = rating;
        PlayerType =playerType;
    }
}