using System.Diagnostics;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BadukServer;

[Immutable, GenerateSerializer]
[Alias("Game")]
public class Game
{
    public Game(string gameId, int rows, int columns, TimeControl timeControl, List<PlayerTimeSnapshot> playerTimeSnapshots, List<MoveData> moves, Dictionary<string, StoneType> playgroundMap, Dictionary<string, StoneType> players, Dictionary<string, int> prisoners, string? startTime, GameState gameState, string? koPositionInLastMove, List<string> deadStones, string? winnerId, List<int> finalTerritoryScores, float komi, GameOverMethod? gameOverMethod, string? endTime,
StoneSelectionType stoneSelectionType,
string? gameCreator
    )
    {
        GameId = gameId;
        Rows = rows;
        Columns = columns;
        TimeControl = timeControl;
        PlayerTimeSnapshots = playerTimeSnapshots;
        PlaygroundMap = playgroundMap;
        Moves = moves;
        Players = players;
        Prisoners = prisoners;
        StartTime = startTime;
        KoPositionInLastMove = koPositionInLastMove;
        GameState = gameState;
        DeadStones = deadStones;
        WinnerId = winnerId;
        FinalTerritoryScores = finalTerritoryScores;
        Komi = komi;
        GameOverMethod = gameOverMethod;
        EndTime = endTime;
        StoneSelectionType = stoneSelectionType;
        GameCreator = gameCreator;
    }

    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [Id(0)]
    public string? Id { get; set; }
    [BsonElement("gameId")]
    [Id(1)]
    public string GameId { get; set; }
    [BsonElement("rows")]
    [Id(2)]
    public int Rows { get; set; }
    [BsonElement("columns")]
    [Id(3)]
    public int Columns { get; set; }
    [BsonElement("timeControl")]
    [Id(4)]
    public TimeControl TimeControl { get; set; }
    [BsonElement("playerTimeSnapshots")]
    [Id(5)]
    public List<PlayerTimeSnapshot> PlayerTimeSnapshots { get; set; }
    [BsonElement("playgroundMap")]
    [Id(6)]
    public Dictionary<string, StoneType> PlaygroundMap { get; set; }
    [BsonElement("moves")]
    [Id(7)]
    public List<MoveData> Moves { get; set; }
    [BsonElement("players")]
    [Id(8)]
    public Dictionary<string, StoneType> Players { get; set; }
    [BsonElement("prisoners")]
    [Id(9)]
    public Dictionary<string, int> Prisoners { get; set; }
    [BsonElement("startTime")]
    [Id(10)]
    public string? StartTime { get; set; }
    [BsonElement("koPositionInLastMove")]
    [Id(11)]
    public string? KoPositionInLastMove { get; set; }
    [BsonElement("gameState")]
    [Id(12)]
    public GameState GameState { get; set; }
    [BsonElement("deadStones")]
    [Id(13)]
    public List<string> DeadStones { get; set; }
    [BsonElement("winnerId")]
    [Id(14)]
    public string? WinnerId { get; set; }
    [BsonElement("finalTerritoryScores")]
    [Id(15)]
    public List<int> FinalTerritoryScores { get; set; }
    [BsonElement("komi")]
    [Id(16)]
    public float Komi { get; set; }
    [BsonElement("gameOverMethod")]
    [Id(17)]
    public GameOverMethod? GameOverMethod { get; set; }
    [BsonElement("endTime")]
    [Id(18)]
    public string? EndTime { get; set; }

    [BsonElement("stoneSelectionType")]
    [Id(19)]
    public StoneSelectionType StoneSelectionType { get; set; }

    [BsonElement("gameCreator")]
    [Id(20)]
    public string? GameCreator { get; set; }
}

[GenerateSerializer]
public enum StoneType
{
    /// <summary>
    /// Black stone
    /// </summary>
    // [BsonRepresentation(BsonType.String)]
    Black = 0,
    /// <summary>
    /// White stone
    /// </summary>
    // [BsonRepresentation(BsonType.String)]
    White = 1,
}

[GenerateSerializer]
public enum GameState
{
    WaitingForStart = 0,
    // Started,
    Playing = 1,
    ScoreCalculation = 2,
    Paused = 3,
    Ended = 4
}

[Immutable, GenerateSerializer]
[Alias("TimeControl")]
public class TimeControl
{
    [BsonElement("mainTimeSeconds")]
    [Id(0)]
    public int MainTimeSeconds { get; set; }

    [BsonElement("incrementSeconds")]
    [Id(1)]
    public int? IncrementSeconds { get; set; }

    [BsonElement("byoYomiTime")]
    [Id(2)]
    public ByoYomiTime? ByoYomiTime { get; set; }

    public TimeControl(ByoYomiTime? byoYomiTime, int? incrementSeconds, int mainTimeSeconds)
    {
        ByoYomiTime = byoYomiTime;
        IncrementSeconds = incrementSeconds;
        MainTimeSeconds = mainTimeSeconds;
    }
}

[Immutable, GenerateSerializer]
[Alias("ByoYomiTime")]
public class ByoYomiTime
{
    [BsonElement("byoYomis")]
    [Id(0)]
    public int ByoYomis { get; set; }

    [BsonElement("byoYomiSeconds")]
    [Id(1)]
    public int ByoYomiSeconds { get; set; }

    public ByoYomiTime(int byoYomis, int byoYomiSeconds)
    {
        ByoYomis = byoYomis;
        ByoYomiSeconds = byoYomiSeconds;
    }
}


[Immutable, GenerateSerializer]
[Alias("PlayerTimeSnapshot")]
public class PlayerTimeSnapshot
{
    public PlayerTimeSnapshot(string snapshotTimestamp, int mainTimeMilliseconds, int? byoYomisLeft, bool byoYomiActive, bool timeActive)
    {
        SnapshotTimestamp = snapshotTimestamp;
        MainTimeMilliseconds = mainTimeMilliseconds;
        ByoYomisLeft = byoYomisLeft;
        ByoYomiActive = byoYomiActive;
        TimeActive = timeActive;
    }

    [BsonElement("snapshotTimestamp")]
    [Id(0)]
    public string SnapshotTimestamp { get; set; }

    [BsonElement("mainTimeMilliseconds")]
    [Id(1)]
    public int MainTimeMilliseconds { get; set; }

    [BsonElement("byoYomisLeft")]
    [Id(2)]
    public int? ByoYomisLeft { get; set; }

    [BsonElement("byoYomiActive")]
    [Id(3)]
    public bool ByoYomiActive { get; set; }

    [BsonElement("timeActive")]
    [Id(4)]
    public bool TimeActive { get; set; }
}