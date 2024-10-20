using BadukServer;
using BadukServer.Dto;
using BadukServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
namespace BadukServer.Services;

public class UsersService
{
    private readonly IMongoCollection<User> _usersCollection;

    public UsersService(
        IOptions<DatabaseSettings> userDatabaseSettings,
        IOptions<MongodbCollectionParams<User>> userCollection
        )
    {
        var mongoClient = new MongoClient(
            userDatabaseSettings.Value.ConnectionString);

        var mongoDatabase = mongoClient.GetDatabase(
            userDatabaseSettings.Value.DatabaseName);

        _usersCollection = mongoDatabase.GetCollection<User>(
            userCollection.Value.Name);
    }

    public async Task<List<User>> Get() =>
        await _usersCollection.Find(_ => true).ToListAsync();

    public async Task<User?> GetByEmail(string email) =>
        await _usersCollection.Find(user => user.Email == email).FirstOrDefaultAsync();

    public async Task<User?> CreateUser(string email, bool googleSignIn, string? password = null)
    {
        try
        {
            var user = new User(email, googleSignIn, password);
            await _usersCollection.InsertOneAsync(user);
            return user;
        }
        catch
        {
            return null;
        }
    }
}