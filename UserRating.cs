using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

[Immutable, GenerateSerializer]
[Alias("UserRating")]

public class UserRatingFieldNames {
    public const string Ratings ="rts";

    public const string Glicko ="g";
    public const string NB = "nb";
    public const string Recent = "rc";
    public const string Latest = "lt";

    public const string Rating = "mu"; // mu
    public const string Deviation = "phi"; // phi
    public const string Volatility = "si"; // sigma
}

public class UserRating
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [Id(0)]
    public string UserId { get; set; }

    [BsonElement(UserRatingFieldNames.Ratings)]
    [Id(1)]
    public Dictionary<string, PlayerRatingData> Ratings { get; set; }

    public UserRating(string userId, Dictionary<string, PlayerRatingData> ratings)
    {
        UserId = userId;
        Ratings = ratings;
    }
}


[Immutable, GenerateSerializer]
[Alias("PlayerRatingData")]
public class PlayerRatingData
{
    [BsonElement(UserRatingFieldNames.Glicko)]
    [Id(0)]
    public GlickoRating Glicko { get; set; }

    [BsonElement(UserRatingFieldNames.NB)]
    [Id(1)]
    public int NB { get; set; } // number of results

    [BsonElement(UserRatingFieldNames.Recent)]
    [Id(2)]
    public List<int> Recent { get; set; }

    [BsonElement(UserRatingFieldNames.Latest)]
    [Id(3)]
    public DateTime? Latest { get; set; } // last rating period end date

    public PlayerRatingData(GlickoRating glicko, int nb, List<int> recent, DateTime? latest)
    {
        Glicko = glicko;
        NB = nb;
        Recent = recent;
        Latest = latest;
    }
}

public static class GlickoRatingExtensions
{
    public static (double min, double max) GetRatingRange(this PlayerRatingData playerRatingData)
    {
        return (playerRatingData.Glicko.Rating - playerRatingData.Glicko.Deviation, playerRatingData.Glicko.Rating + playerRatingData.Glicko.Deviation);
    }

    public static bool RatingRangeOverlap(this PlayerRatingData playerRatingData, PlayerRatingData otherPlayerRatingData)
    {
        var (min, max) = playerRatingData.GetRatingRange();
        var (otherMin, otherMax) = otherPlayerRatingData.GetRatingRange();

        return min <= otherMax && max >= otherMin;
    }
}


[Immutable, GenerateSerializer]
[Alias("GlickoRating")]
public class GlickoRating
{
    [BsonElement(UserRatingFieldNames.Rating)]
    [Id(0)]
    public double Rating { get; set; }

    [BsonElement(UserRatingFieldNames.Deviation)]
    [Id(1)]
    public double Deviation { get; set; }

    [BsonElement(UserRatingFieldNames.Volatility)]
    [Id(2)]
    public double Volatility { get; set; }

    public GlickoRating(double rating, double deviation, double volatility)
    {
        Rating = rating;
        Deviation = deviation;
        Volatility = volatility;
    }
}