using BadukServer.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

public interface IUserStatService
{
    Task<UserStat?> GetUserStat(string userId);
    Task<UserStat> SaveUserStat(UserStat userStat);
    Task<UserStat> CreateUserStat(string uid);
}

public class UserStatService : IUserStatService
{
    private readonly IMongoCollection<UserStat> _userStatCollection;
    private readonly IMongoOperationLogger _mongoOperation;

    public UserStatService(IOptions<DatabaseSettings> userDatabaseSettings, IOptions<MongodbCollectionParams<UserStat>> userStatCollection, IMongoOperationLogger mongoOperation)
    {
        var mongoClient = new MongoClient(
            userDatabaseSettings.Value.ConnectionString);

        var mongoDatabase = mongoClient.GetDatabase(
            userDatabaseSettings.Value.DatabaseName);

        _userStatCollection = mongoDatabase.GetCollection<UserStat>(
            userStatCollection.Value.Name);
        _mongoOperation = mongoOperation;
    }

    public async Task<UserStat?> GetUserStat(string userId)
    {
        return await _mongoOperation.Operation(() => GetUserStatInternal(userId));
    }

    private async Task<UserStat?> GetUserStatInternal(string userId)
    {
        var res = await _userStatCollection.Find(a => a.UserId == userId).FirstOrDefaultAsync();
        return res;
    }

    public async Task<UserStat> SaveUserStat(UserStat userStat)
    {
        return await _mongoOperation.Operation(() => SaveUserStatInternal(userStat));
    }

    private async Task<UserStat> SaveUserStatInternal(UserStat userStat)
    {
        var res = await _userStatCollection.ReplaceOneAsync(a => a.UserId == userStat.UserId, userStat, new ReplaceOptions { IsUpsert = true });

        if (res.IsAcknowledged)
        {
            return userStat;
        }
        throw new Exception("Failed to update user profile");
    }

    public async Task<UserStat> CreateUserStat(string uid)
    {
        return await _mongoOperation.Operation(() => CreateUserStatInternal(uid));
    }


    private async Task<UserStat> CreateUserStatInternal(string uid)
    {
        var emptyStat = new UserStat(uid, []);
        await _userStatCollection.InsertOneAsync(emptyStat);

        return emptyStat;
    }
}