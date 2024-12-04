using BadukServer;
using BadukServer.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

public interface IGameService
{
    public Task<Game?> GetGame(string gameId);
    public Task<Game?> SaveGame(Game game);
}
public class GameService : IGameService
{
    private readonly IMongoCollection<Game> _gameCollection;
    public GameService(IOptions<DatabaseSettings> gameDatabaseSettings, IOptions<MongodbCollectionParams<Game>> gameCollection)
    {
        var mongoClient = new MongoClient(
            gameDatabaseSettings.Value.ConnectionString);

        var mongoDatabase = mongoClient.GetDatabase(
            gameDatabaseSettings.Value.DatabaseName);

        _gameCollection = mongoDatabase.GetCollection<Game>(
            gameCollection.Value.Name);
    }

    public async Task<Game?> GetGame(string gameId)
    {
        var game = await _gameCollection.Find(Builders<Game>.Filter.Eq(a => a.GameId, gameId)).FirstOrDefaultAsync();
        return game;
    }

    public async Task<Game?> SaveGame(Game game)
    {
        try
        {
            var updateOptions = new ReplaceOptions { IsUpsert = true };

            var res = await _gameCollection.ReplaceOneAsync(Builders<Game>.Filter.Eq(a => a.GameId, game.GameId), game, updateOptions);

            return game;
        }
        catch
        {
            return null;
        }
    }
}