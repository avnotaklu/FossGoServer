using System.Reflection.Metadata.Ecma335;
using BadukServer;
using BadukServer.Models;
using Glicko2;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

public interface IUserRatingService
{
    public Task<UserRating> GetUserRatings(string userId);
    public Task<UserRating?> CreateUserRatings(string userId);
    public Task<UserRating?> SaveUserRatings(UserRating userRating);
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

    public async Task<UserRating> GetUserRatings(string userId)
    {
        return await _ratingsCollection.Find(a => a.UserId == userId).FirstOrDefaultAsync();
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

    public async Task<UserRating?> SaveUserRatings(UserRating userRating)
    {
        try
        {
            var res = await _ratingsCollection.UpdateOneAsync(Builders<UserRating>.Filter.Eq(a => a.UserId, userRating.UserId), Builders<UserRating>.Update.Set(a => a.Ratings, userRating.Ratings));

            return userRating;
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

