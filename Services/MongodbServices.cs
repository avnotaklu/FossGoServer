using System.Data.Entity;
using BadukServer.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

public class MongodbService
{
    public MongoClient client;
    public IMongoDatabase database;

    public MongodbService(IOptions<DatabaseSettings> settings)
    {
        client = new MongoClient(settings.Value.ConnectionString);
        database = client.GetDatabase(settings.Value.DatabaseName);
    }
}