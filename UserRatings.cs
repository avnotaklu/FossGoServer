using BadukServer;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class UserRatingFieldNames
{
    public const string Ratings = "rts";

    public const string Glicko = "g";
    public const string NB = "nb";
    public const string Recent = "rc";
    public const string Latest = "lt";

    public const string Rating = "mu"; // mu
    public const string Deviation = "phi"; // phi
    public const string Volatility = "si"; // sigma
}

public static class UserRatingExtensions
{
    public static PlayerRatingsData? GetRatingData(this PlayerRatings rating, ConcreteGameVariant variant)
    {
        return rating.Ratings.ContainsKey(variant.ToKey()) ? rating.Ratings[variant.ToKey()] : null;
    }
}

[Immutable, GenerateSerializer]
[Alias("PlayerRatings")]
public class PlayerRatings
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [Id(0)]
    public string PlayerId { get; set; }

    [BsonElement(UserRatingFieldNames.Ratings)]
    [Id(1)]
    public Dictionary<string, PlayerRatingsData> Ratings { get; set; }

    public PlayerRatings(string userId, Dictionary<string, PlayerRatingsData> ratings)
    {
        PlayerId = userId;
        Ratings = ratings;
    }
}

[Immutable, GenerateSerializer]
[Alias("PlayerRatingsData")]
public class PlayerRatingsData
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

    public PlayerRatingsData(GlickoRating glicko, int nb, List<int> recent, DateTime? latest)
    {
        Glicko = glicko;
        NB = nb;
        Recent = recent;
        Latest = latest;
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