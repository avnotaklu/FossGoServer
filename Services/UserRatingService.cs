using System.Reflection.Metadata.Ecma335;
using BadukServer;
using BadukServer.Models;
using Glicko2;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

public interface IUserRatingService
{
    public Task<PlayerRatings> GetUserRatings(string userId);
    public Task<PlayerRatings?> CreateUserRatings(string userId);
    public Task<PlayerRatings?> SaveUserRatings(PlayerRatings userRating);
}

public class UserRatingService : IUserRatingService
{
    private readonly IMongoCollection<PlayerRatings> _ratingsCollection;
    private readonly IRatingEngine _ratingEngine;
    private readonly IDateTimeService _dateTimeService;
    public UserRatingService(IOptions<DatabaseSettings> userDatabaseSettings, IOptions<MongodbCollectionParams<PlayerRatings>> ratingsCollection, IRatingEngine ratingEngine, IDateTimeService dateTimeService)
    {
        var mongoClient = new MongoClient(
            userDatabaseSettings.Value.ConnectionString);

        var mongoDatabase = mongoClient.GetDatabase(
            userDatabaseSettings.Value.DatabaseName);

        _ratingsCollection = mongoDatabase.GetCollection<PlayerRatings>(
            ratingsCollection.Value.Name);

        _ratingEngine = ratingEngine;
        _dateTimeService = dateTimeService;
    }


    public PlayerRatings UpdateUserRatingToCurrentRead(PlayerRatings oldRating)
    {
        return new PlayerRatings(oldRating.PlayerId, new Dictionary<string, PlayerRatingsData>(oldRating.Ratings.Select(a =>
        new KeyValuePair<string, PlayerRatingsData>(a.Key, new PlayerRatingsData(new GlickoRating(
            a.Value.Glicko.Rating, _ratingEngine.PreviewDeviation(a.Value, _dateTimeService.Now(), false), a.Value.Glicko.Volatility
        ), a.Value.NB, a.Value.Recent, a.Value.Latest))
        )));
    }

    public PlayerRatings UpdateUserRatingToCurrentWrite(PlayerRatings oldRating)
    {
        return new PlayerRatings(oldRating.PlayerId, new Dictionary<string, PlayerRatingsData>(oldRating.Ratings.Select(a =>
        new KeyValuePair<string, PlayerRatingsData>(a.Key, new PlayerRatingsData(new GlickoRating(
            a.Value.Glicko.Rating, _ratingEngine.PreviewDeviation(a.Value, _dateTimeService.Now(), true), a.Value.Glicko.Volatility
        ), a.Value.NB, a.Value.Recent, a.Value.Latest))
        )));
    }


    public async Task<PlayerRatings> GetUserRatings(string userId)
    {
        var oldRating = await _ratingsCollection.Find(a => a.PlayerId == userId).FirstOrDefaultAsync();

        if (oldRating == null)
        {
            throw new UserNotFoundException(userId);
        }

        return UpdateUserRatingToCurrentRead(oldRating);
    }

    public async Task<PlayerRatings?> CreateUserRatings(string userId)
    {
        try
        {
            var rating = new PlayerRatings(userId, GetInitialRatings());
            await _ratingsCollection.InsertOneAsync(rating);
            return rating;
        }
        catch
        {
            return null;
        }
    }

    public async Task<PlayerRatings?> SaveUserRatings(PlayerRatings userRating)
    {
        try
        {
            var newRat = UpdateUserRatingToCurrentWrite(userRating);
            var res = await _ratingsCollection.UpdateOneAsync(Builders<PlayerRatings>.Filter.Eq(a => a.PlayerId, userRating.PlayerId), Builders<PlayerRatings>.Update.Set(a => a.Ratings, newRat.Ratings));

            return userRating;
        }
        catch
        {
            return null;
        }
    }

    public static Dictionary<string, PlayerRatingsData> GetInitialRatings()
    {
        return new Dictionary<string, PlayerRatingsData>(
            RatingEngine.RateableVariants().Select(
                t => new KeyValuePair<string, PlayerRatingsData>(t.ToKey(), GetInitialRatingData())
            )
        );
    }

    private static PlayerRatingsData GetInitialRatingData()
    {
        return new PlayerRatingsData(new GlickoRating(1500, 200, 0.06), nb: 0, recent: [], latest: null);
    }
}

