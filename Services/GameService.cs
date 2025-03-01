using System.Diagnostics;
using BadukServer;
using BadukServer.Models;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

public interface IGameService
{
    public Task<List<GameAndOpponent>> GetGamesWithOpponent(string player);
    public Task<List<Game>> GetGamesForPlayers(string player, int page, BoardSize? boardSize = null, TimeStandard? timeStandard = null, PlayerResult? result = null, DateTime? from = null, DateTime? to = null);
    Task<List<Game>> GetActiveGamesForPlayer(string playerId);
    public Task<Game?> GetGame(string gameId);
    public Task<Game?> SaveGame(Game game);
}
public class GameService : IGameService
{
    private readonly IMongoCollection<Game> _gameCollection;
    private readonly IMongoOperationLogger _mongoOperation;
    private readonly ILogger<IGameService> _logger;
    private static readonly int historyPageSize = 12;

    public GameService(MongodbService mongodb,
     IOptions<MongodbCollectionParams<Game>> gameCollection, IMongoOperationLogger mongoOperation, ILogger<IGameService> logger)
    {
        _gameCollection = mongodb.database.GetCollection<Game>(
            gameCollection.Value.Name);
        _mongoOperation = mongoOperation;
        _logger = logger;
    }

    public async Task<List<Game>> GetActiveGamesForPlayer(string playerId)
    {
        var filter = Builders<Game>.Filter.Where(a => a.Players.Contains(playerId) && a.GameState != GameState.Ended);
        var games = await _gameCollection.Find(filter).ToListAsync();
        return games;
    }

    public async Task<List<Game>> GetGamesForPlayers(string player, int page, BoardSize? boardSize = null, TimeStandard? timeStandard = null, PlayerResult? result = null, DateTime? from = null, DateTime? to = null)
    {
        _logger.LogInformation("Finding games for player {player} {page}", player, page);


        List<FilterDefinition<Game>> filters = [];

        var filter = Builders<Game>.Filter.Eq(
            (a) => a.GameState, GameState.Ended
        );

        filter = Builders<Game>.Filter.And(filter, Builders<Game>.Filter.Where(a => a.Players.Contains(player)));


        if (boardSize != null)
        {
            var bSize = (BoardSize)boardSize!;
            int? dims = bSize.MatchingDims();
            var nonMatchingIfOther = BoardSizeExtensions.NonDimsMatchingToOther();

            _logger.LogInformation("FilteredBoardSize {boardSize} {rows}", bSize, dims);

            if (boardSize == BoardSize.Other)
            {
                filter = Builders<Game>.Filter.And(filter, Builders<Game>.Filter.Nin(
                    (a) => a.Rows, nonMatchingIfOther
                ));

                filter = Builders<Game>.Filter.And(filter, Builders<Game>.Filter.Nin(
                    (a) => a.Columns, nonMatchingIfOther
                ));
            }
            else
            {
                filter = Builders<Game>.Filter.And(filter, Builders<Game>.Filter.Eq(
                    a => a.Rows, dims
                ));

                filter = Builders<Game>.Filter.And(filter, Builders<Game>.Filter.Eq(
                    a => a.Columns, dims
                ));
            }

        }

        if (timeStandard != null)
        {
            _logger.LogInformation("FilteredTimeStandard {timeStandard}", timeStandard);
            filter = Builders<Game>.Filter.And(filter, Builders<Game>.Filter.Where(
                a => a.TimeControl.TimeStandard == timeStandard
            ));
        }

        if (result != null)
        {
            _logger.LogInformation("FilteredResult {result}", result);

            var res = (PlayerResult)result!;

            var blackFilter = Builders<Game>.Filter.Where(
                a => a.Result == res.WhenBlack() && a.Players[0] == player
            );

            var whiteFilter = Builders<Game>.Filter.Or(blackFilter, Builders<Game>.Filter.Where(
                a => a.Result == res.WhenWhite() && a.Players[1] == player
            ));

            filter = Builders<Game>.Filter.And(filter, whiteFilter);
        }

        if (from != null)
        {
            _logger.LogInformation("FilteredTime {time}", from);
            filter = Builders<Game>.Filter.And(filter, Builders<Game>.Filter.Where(
                a => a.CreationTime >= from
            ));
        }

        if (to != null)
        {
            _logger.LogInformation("FilteredTime {time}", to);
            filter = Builders<Game>.Filter.And(filter, Builders<Game>.Filter.Where(
                a => a.CreationTime <= to
            ));
        }

        var sort = Builders<Game>.Sort.Descending(a => a.CreationTime);
        var games = await _gameCollection.Find(filter).Sort(sort).Skip(page * historyPageSize).Limit(historyPageSize).ToListAsync();
        return games;
    }

    // NOTE: Unused
    public async Task<List<GameAndOpponent>> GetGamesWithOpponent(string player)
    {
        // Ignored TODO: pagination
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