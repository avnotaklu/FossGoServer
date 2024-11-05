using System.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BadukServer;

[Immutable]
[GenerateSerializer]
public class Game
{
    public Game(string gameId, int rows, int columns, int timeInSeconds, Dictionary<string, int> timeLeftForPlayers, Dictionary<string, StoneType> playgroundMap, List<MoveData> moves, Dictionary<string, StoneType> players, Dictionary<string, int> prisoners, string? startTime, string? koPositionInLastMove, GameState gameState, List<string> deadStones)
    {
        GameId = gameId;
        Rows = rows;
        Columns = columns;
        TimeInSeconds = timeInSeconds;
        TimeLeftForPlayers = timeLeftForPlayers;
        PlaygroundMap = playgroundMap;
        Moves = moves;
        Players = players;
        Prisoners = prisoners;
        StartTime = startTime;
        KoPositionInLastMove = koPositionInLastMove;
        GameState = gameState;
        DeadStones = deadStones;
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
    // [BsonElement("playgroundMap")]
    // public Dictionary<string, string> PlaygroundMap { get; set; }
    [BsonElement("playgroundMap")]
    public Dictionary<string, StoneType> PlaygroundMap { get; set; }
    [BsonElement("moves")]
    public List<MoveData> Moves { get; set; }
    [BsonElement("players")]
    public Dictionary<string, StoneType> Players { get; set; }
    [BsonElement("prisoners")]
    public Dictionary<string, int> Prisoners { get; set; }
    [BsonElement("startTime")]
    public string? StartTime { get; set; }
    [BsonElement("koPositionInLastMove")]
    public string? KoPositionInLastMove { get; set; }
    [BsonElement("gameState")]
    public GameState GameState { get; set; }
    [BsonElement("deadStones")]
    public List<string> DeadStones { get; set; }
}



[GenerateSerializer]
public enum StoneType
{
    /// <summary>
    /// Black stone
    /// </summary>
    // [BsonRepresentation(BsonType.String)]
    Black,
    /// <summary>
    /// White stone
    /// </summary>
    // [BsonRepresentation(BsonType.String)]
    White,
}

[Serializable]
public enum GameState
{
    WaitingForStart = 0,
    // Started,
    Playing = 1,
    ScoreCalculation = 2,
    Ended = 3
}
