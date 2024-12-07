using BadukServer;
using BadukServer.Dto;
using BadukServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
namespace BadukServer.Services;


public interface IUsersService
{
    public Task<List<User>> Get();
    public Task<List<User>> GetByIds(List<string> ids);
    public Task<User?> GetByEmail(string email);
    public Task<User?> GetByUserName(string userName);
    public Task<User?> CreateUser(UserDetailsDto userDetails, string? passwordHash);
}

public class UsersService : IUsersService
{
    private readonly IMongoCollection<User> _usersCollection;
    private readonly IDateTimeService _dateTimeService;

    public UsersService(
        IOptions<DatabaseSettings> userDatabaseSettings,
        IOptions<MongodbCollectionParams<User>> userCollection,
        IDateTimeService dateTimeService
        )
    {
        var mongoClient = new MongoClient(
            userDatabaseSettings.Value.ConnectionString);

        var mongoDatabase = mongoClient.GetDatabase(
            userDatabaseSettings.Value.DatabaseName);

        _usersCollection = mongoDatabase.GetCollection<User>(
            userCollection.Value.Name);
        _dateTimeService = dateTimeService;
    }

    public async Task<List<User>> Get() =>
        await _usersCollection.Find(_ => true).ToListAsync();

    public async Task<List<User>> GetByIds(List<string> ids) =>
        await (from user in _usersCollection.AsQueryable()
               where ids.Contains(user.Id!)
               select user).ToListAsync();

    public async Task<User?> GetByEmail(string email) =>
        await _usersCollection.Find(user => user.Email == email).FirstOrDefaultAsync();
    public async Task<User?> GetByUserName(string uname) =>
        await _usersCollection.Find(user => user.UserName == uname).FirstOrDefaultAsync();

    public async Task<User?> CreateUser(UserDetailsDto userDetails, string? passwordHash)
    {
        try
        {
            var user = new User(
                email: userDetails.Email,
                googleSignIn: userDetails.GoogleSignIn,
                passwordHash: passwordHash,
                userName: userDetails.Username,
                fullName: userDetails.FullName,
                bio: userDetails.Bio,
                avatar: userDetails.Avatar,
                nationality: userDetails.Nationalilty,
                creation: _dateTimeService.Now(),
                lastSeen: _dateTimeService.Now()
            );

            await _usersCollection.InsertOneAsync(user);
            return user;
        }
        catch
        {
            return null;
        }
    }
}