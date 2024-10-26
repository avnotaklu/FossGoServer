using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BadukServer;

[Immutable]
[GenerateSerializer]
public class Game
{
    public Game(string gameId, int rows, int columns, int timeInSeconds, Dictionary<string, int> timeLeftForPlayers, Dictionary<string, string> playgroundMap, List<GameMove> moves, Dictionary<string, Stone> players, Dictionary<string, int> playerScores)
    {
        GameId = gameId;
        Rows = rows;
        Columns = columns;
        TimeInSeconds = timeInSeconds;
        TimeLeftForPlayers = timeLeftForPlayers;
        PlaygroundMap = playgroundMap;
        Moves = moves;
        Players = players;
        PlayerScores = playerScores;
    }

    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    [BsonElement("gameId")]
    public string GameId { get; set; }
    [BsonElement("rows")]
    public int Rows { get; set; }
    [BsonElement("columns")]
    public int Columns { get; set; }
    [BsonElement("timeInSeconds")]
    public int TimeInSeconds { get; set; }
    [BsonElement("timeLeftForPlayers")]
    public Dictionary<string, int> TimeLeftForPlayers { get; set; }
    [BsonElement("playgroundMap")]
    public Dictionary<string, string> PlaygroundMap { get; set; }
    [BsonElement("moves")]
    public List<GameMove> Moves { get; set; }
    [BsonElement("players")]
    public Dictionary<string, Stone> Players { get; set; }
    [BsonElement("playerScores")]
    public Dictionary<string, int> PlayerScores { get; set; }
}


[GenerateSerializer]
public enum Stone
{
    /// <summary>
    /// Black stone
    /// </summary>
    [BsonRepresentation(BsonType.String)]
    Black,
    /// <summary>
    /// White stone
    /// </summary>
    [BsonRepresentation(BsonType.String)]
    White
}