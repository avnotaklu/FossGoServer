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
    private readonly IMongoOperationLogger _mongoOperation;

    public GameService(IOptions<DatabaseSettings> gameDatabaseSettings, IOptions<MongodbCollectionParams<Game>> gameCollection, IMongoOperationLogger mongoOperation)
    {
        var mongoClient = new MongoClient(
            gameDatabaseSettings.Value.ConnectionString);

        var mongoDatabase = mongoClient.GetDatabase(
            gameDatabaseSettings.Value.DatabaseName);

        _gameCollection = mongoDatabase.GetCollection<Game>(
            gameCollection.Value.Name);
        _mongoOperation = mongoOperation;
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
            return await _mongoOperation.Operation(() => SaveGameInternal(game));
        }
        // Here we catch the DatabaseException and return null
        // This is because the SaveGame function isn't called by any api
        catch (DatabaseOperationException)
        {
            return null;
        }
    }

    private async Task<Game> SaveGameInternal(Game game)
    {
        var res = await _gameCollection.ReplaceOneAsync(a => a.GameId == game.GameId, game, new ReplaceOptions { IsUpsert = true });
        return game;
    }
}