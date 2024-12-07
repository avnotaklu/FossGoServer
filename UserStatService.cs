using BadukServer.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

public interface IUserStatService
{
    Task<UserStat?> GetUserStatAsync(string userId);
    Task<UserStat> SaveUserStat(UserStat userStat);
    Task<UserStat> CreateUserStat(string uid);
}

public class UserStatService : IUserStatService
{
    private readonly IMongoCollection<UserStat> _userStatCollection;
    private readonly IStatCalculator _statCalculator;


    public UserStatService(IOptions<DatabaseSettings> userDatabaseSettings, IOptions<MongodbCollectionParams<UserStat>> userStatCollection, IStatCalculator statCalculator)
    {
        var mongoClient = new MongoClient(
            userDatabaseSettings.Value.ConnectionString);

        var mongoDatabase = mongoClient.GetDatabase(
            userDatabaseSettings.Value.DatabaseName);

        _userStatCollection = mongoDatabase.GetCollection<UserStat>(
            userStatCollection.Value.Name);
        _statCalculator = statCalculator;
    }


    public async Task<UserStat?> GetUserStatAsync(string userId)
    {
        var res = await _userStatCollection.Find(a => a.UserId == userId).FirstOrDefaultAsync();

        return res;
    }

    public async Task<UserStat> SaveUserStat(UserStat userStat)
    {
        var res = await _userStatCollection.ReplaceOneAsync(a => a.UserId == userStat.UserId, userStat, new ReplaceOptions { IsUpsert = true });
        return userStat;
    }


    public async Task<UserStat> CreateUserStat(string uid)
    {
        var emptyStat = new UserStat(uid, []);
        await _userStatCollection.InsertOneAsync(emptyStat);

        return emptyStat;
    }
}