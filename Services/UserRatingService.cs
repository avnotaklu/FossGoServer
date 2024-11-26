using System.Reflection.Metadata.Ecma335;
using BadukServer;
using BadukServer.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

public interface IUserRatingService
{
    public Task<UserRating> GetUserRatings(string userId);
}

public class UserRatingService : IUserRatingService
{
    private readonly IMongoCollection<UserRating> _ratingsCollection;
    public UserRatingService(IOptions<DatabaseSettings> userDatabaseSettings, IOptions<MongodbCollectionParams<UserRating>> ratingsCollection)
    {
        var mongoClient = new MongoClient(
            userDatabaseSettings.Value.ConnectionString);

        var mongoDatabase = mongoClient.GetDatabase(
            userDatabaseSettings.Value.DatabaseName);

        _ratingsCollection = mongoDatabase.GetCollection<UserRating>(
            ratingsCollection.Value.Name);
    }

    public Task<UserRating> GetUserRatings(string userId)
    {
        throw new NotImplementedException();
    }

    public async Task<UserRating?> CreateUserRatings(string userId)
    {
        try
        {
            var rating = new UserRating(userId, GetInitialRatings());
            await _ratingsCollection.InsertOneAsync(rating);
            return rating;
        }
        catch
        {
            return null;
        }
    }

    public static Dictionary<string, PlayerRatingData> GetInitialRatings()
    {
        return new Dictionary<string, PlayerRatingData>(
        RatingEngine.RateableBoards().Select(a => (BoardSize?)a).Append(null).Select(b => RatingEngine.RateableTimeStandards().Select(t => new KeyValuePair<string, PlayerRatingData>(RatingEngine.RatingKey(b, t), GetInitialRatingData()))).SelectMany(a => a)
                    );
    }

    private static PlayerRatingData GetInitialRatingData()
    {
        return new PlayerRatingData(new GlickoRating(1500, 200, 0.06), nb: 0, recent: [], latest: null);
    }
}

public class UserRating
{
    public string UserId;
    public Dictionary<string, PlayerRatingData> Ratings;

    public UserRating(string userId, Dictionary<string, PlayerRatingData> ratings)
    {
        UserId = userId;
        Ratings = ratings;
    }
}

