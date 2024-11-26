using System.Diagnostics;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BadukServer;

public static class GameHelpers
{
    public static bool DidStart(this Game game)
    {
        return game.GameState != GameState.WaitingForStart;
    }


    public static string? GetPlayerIdWithTurn(this Game game)
    {
        if (!game.DidStart()) return null;

        var turn = game.Moves.Count;
        foreach (var item in game.Players)
        {
            if ((int)item.Value == (turn % 2))
            {
                return item.Key;
            }
        }

        throw new UnreachableException("This path shouldn't be reachable, as there always exists one user with supposed next turn once game has started");
    }


    public static StoneType? GetStoneFromPlayerId(this Game game, string id)
    {
        if (!game.DidStart()) return null;
        return game.Players[id];
    }


    public static StoneType? GetOtherStoneFromPlayerId(this Game game, string id)
    {
        if (!game.DidStart()) return null;
        return 1 - game.GetStoneFromPlayerId(id);
    }

    public static string? GetOtherPlayerIdFromPlayerId(this Game game, string id)
    {
        var otherStone = game.GetOtherStoneFromPlayerId(id);
        if (otherStone == null) return null;

        return game.GetPlayerIdFromStoneType((StoneType)otherStone);
    }

    public static StoneType? GetOtherStoneFromPlayerIdAlt(this Dictionary<string, StoneType> players, string id)
    {
        return 1 - players[id];
    }

    public static string? GetOtherPlayerIdFromPlayerIdAlt(this Dictionary<string, StoneType> players, string id)
    {
        var otherStone = players.GetOtherStoneFromPlayerIdAlt(id);
        if (otherStone == null) return null;

        return players.GetPlayerIdFromStoneTypeAlt((StoneType)otherStone);
    }


    public static string? GetPlayerIdFromStoneTypeAlt(this Dictionary<string, StoneType> players, StoneType stone)
    {
        foreach (var item in players)
        {
            if (item.Value == stone)
            {
                return item.Key;
            }
        }

        // Player: {stone} has not yet joined the game
        return null;
    }


    public static string? GetPlayerIdFromStoneType(this Game game, StoneType stone)
    {
        if (!game.DidStart()) return null;
        foreach (var item in game.Players)
        {
            if (item.Value == stone)
            {
                return item.Key;
            }
        }

        // Player: {stone} has not yet joined the game
        return null;
    }


    public static BoardSize GetBoardSize(this Game game)
    {
        return game.Rows switch
        {
            9 => game.Columns switch { 9 => BoardSize.Nine, _ => BoardSize.Other },
            13 => game.Columns switch { 13 => BoardSize.Thirteen, _ => BoardSize.Other },
            19 => game.Columns switch { 19 => BoardSize.Nineteen, _ => BoardSize.Other },
            _ => BoardSize.Other
        };
    }
}

public enum BoardSize
{
    Nine = 0,
    Thirteen = 1,
    Nineteen = 2,
    Other = 3
}

[Immutable, GenerateSerializer]
[Alias("Game")]
public class Game
{
    public Game(string gameId, int rows, int columns, TimeControl timeControl, List<PlayerTimeSnapshot> playerTimeSnapshots, List<MoveData> moves, Dictionary<string, StoneType> playgroundMap, Dictionary<string, StoneType> players, List<int> prisoners, string? startTime, GameState gameState, string? koPositionInLastMove, List<string> deadStones, string? winnerId, List<int> finalTerritoryScores, float komi, GameOverMethod? gameOverMethod, string? endTime,
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
    public List<int> Prisoners { get; set; }
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

public enum TimeStandard
{
    Blitz = 0,
    Rapid = 1,
    Classical = 2,
    Correspondence = 3,
    Unknown = 4
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

    [BsonElement("timeStandard")]
    [Id(3)]
    public TimeStandard TimeStandard { get; set; }


    public TimeControl(ByoYomiTime? byoYomiTime, int? incrementSeconds, int mainTimeSeconds, TimeStandard timeStandard)
    {
        Debug.Assert(mainTimeSeconds > 0);
        Debug.Assert(incrementSeconds == null || incrementSeconds > 0);
        Debug.Assert(byoYomiTime == null || byoYomiTime.ByoYomiSeconds > 0);
        Debug.Assert(byoYomiTime == null || byoYomiTime.ByoYomis > 0);

        ByoYomiTime = byoYomiTime;
        IncrementSeconds = incrementSeconds;
        MainTimeSeconds = mainTimeSeconds;
        TimeStandard = timeStandard;
    }

    public TimeControl(TimeControlData data)
    {
        ByoYomiTime = data.ByoYomiTime;
        IncrementSeconds = data.IncrementSeconds;
        MainTimeSeconds = data.MainTimeSeconds;
        TimeStandard = data.GetTimeStandard();
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