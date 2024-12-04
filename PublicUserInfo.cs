using BadukServer.Orleans.Grains;


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
    public UserRating? Rating { get; set; }
    public PlayerType PlayerType { get; set; }

    public PlayerInfo(string id, string? email, UserRating? rating, PlayerType playerType)
    {
        Email = email;
        Id = id;
        Rating = rating;
        PlayerType =playerType;
    }
}