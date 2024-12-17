using BadukServer;
using MongoDB.Bson.Serialization.Attributes;

[Immutable, GenerateSerializer]
[Alias("GameHistoryBatch")]
public class GameHistoryBatch
{
    public List<Game> Games { get; set; }
    public GameHistoryBatch(List<Game> games)
    {
        Games = games;
    }
}


[Immutable, GenerateSerializer]
[Alias("GameQueryData")]
[BsonIgnoreExtraElements]
public class GameQueryData
{
    [BsonElement("g")]
    public Game game {get; set;}
    [BsonElement("o")]
    public User user {get; set;}
    [BsonElement("rtng")]
    public PlayerRatings playerRating {get; set;}

    public GameQueryData(Game game, User user, PlayerRatings playerRating)
    {
        this.game = game;
        this.user = user;
        this.playerRating = playerRating;
    }
}
