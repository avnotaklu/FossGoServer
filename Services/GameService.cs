using BadukServer;
using BadukServer.Models;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

public interface IGameService
{
    public Task<List<GameAndOpponent>> GetGamesWithOpponent(string player);
    public Task<List<Game>> GetGamesForPlayers(string player, int page);
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


    public async Task<List<Game>> GetGamesForPlayers(string player, int page)
    {
        var pageSize = 12;
        var filter = Builders<Game>.Filter.Where(a => a.Players.Contains(player));
        var sort = Builders<Game>.Sort.Descending(a => a.CreationTime);
        var games = await _gameCollection.Find(filter).Sort(sort).Skip((page - 1) * pageSize).Limit(pageSize).ToListAsync();
        return games;
    }

    public async Task<List<GameAndOpponent>> GetGamesWithOpponent(string player)
    {
        // TODO: pagination
        /*
            [
            {
                $lookup: {
                from: "users",
                let: { uids: "$p" },
                pipeline: [
                    { $match: { $expr: { $and: [
                    { $in: ["$_id", "$$uids"] },
                    { $ne: ["$_id", "{player}"] }
                    ] }}}
                ],
                as: "o"
                }
            },
            {
                $project: {
                g: "$$ROOT",
                o: 1,
                }
            }
            ]
        */


        var l0 = new BsonDocument{
            {
                "$lookup", new BsonDocument{
                    { "from", "users" },
                    { "let", new BsonDocument{
                        { "uids", "$p" }
                    }},
                    { "pipeline", new BsonArray{
                        new BsonDocument{
                            { "$match", new BsonDocument{
                                { "$expr", new BsonDocument{
                                    { "$and", new BsonArray{
                                        new BsonDocument{
                                            { "$in", new BsonArray{ "$_id", "$$uids" } }
                                        },
                                        new BsonDocument{
                                            { "$ne", new BsonArray{ "$_id", player } }
                                        }
                                    }}
                                }}
                            }}
                        }
                    }},
                    { "as", "o" }
                }
            }
        };


        var l1 = new BsonDocument{
            {
                "$lookup", new BsonDocument{
                    { "from", "users_ratings" },
                    { "let", new BsonDocument{
                        { "uids", "$p" }
                    }},
                    { "pipeline", new BsonArray{
                        new BsonDocument{
                            { "$match", new BsonDocument{
                                { "$expr", new BsonDocument{
                                    { "$and", new BsonArray{
                                        new BsonDocument{
                                            { "$in", new BsonArray{ "$_id", "$$uids" } }
                                        },
                                        new BsonDocument{
                                            { "$ne", new BsonArray{ "$_id", player } }
                                        }
                                    }}
                                }}
                            }}
                        }
                    }},
                    { "as", "rtng" }
                }
            }
        };



        var pl2 = new BsonDocument{
            {
                "$project", new BsonDocument{
                    { "g", "$$ROOT" },
                    { "o",new BsonDocument {
                        { "$arrayElemAt" , new BsonArray { "$o", 0 } }
                    }},
                    { "rtng",new BsonDocument {
                        { "$arrayElemAt" , new BsonArray { "$rtng", 0 } }
                    }}
                }
            }
        };


        BsonDocument[] pipeline = [l0, l1, pl2];

        var game = (await _gameCollection.AggregateAsync<BsonDocument>(pipeline)).ToList();
        return game.Select(a => BsonSerializer.Deserialize<GameQueryData>(a)).ToList().Select(a => new GameAndOpponent(new PlayerInfo(
            id: a.user.Id!,
            username: a.user.UserName,
            rating: a.playerRating,
            playerType: PlayerType.Normal
        ), a.game)).ToList();
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