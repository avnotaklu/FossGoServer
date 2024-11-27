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

    public static BoardSizeParams GetBoardSizeParams(this Game game)
    {
        return new BoardSizeParams(rows: game.Rows, columns: game.Columns);
    }
}

public enum BoardSize
{
    Nine = 0,
    Thirteen = 1,
    Nineteen = 2,
    Other = 3
}

public class GameFieldNames
{
    public const string Rows = "r";
    public const string Cols = "c";
    public const string TimeControl = "tc";
    public const string PlayerTimeSnapshots = "ts";
    public const string PlaygroundMap = "map";
    public const string Moves = "mv";
    public const string Players = "p";
    public const string Prisoners = "pr";
    public const string StartTime = "st";
    public const string EndTime = "et";
    public const string KoPositionInLastMove = "ko";
    public const string GameState = "gs";
    public const string DeadStones = "ds";
    public const string WinnerId = "wi";
    public const string FinalTerritoryScores = "fts";
    public const string Komi = "k";
    public const string GameOverMethod = "gom";
    public const string StoneSelectionType = "sst";
    public const string GameCreator = "gc";
    public const string PlayersRatings = "rts";
    public const string PlayersRatingsDiff = "prd";

    public const string MainTimeSeconds = "mts";
    public const string IncrementSeconds = "is";
    public const string ByoYomiTime = "byt";
    public const string TimeStandard = "ts";
    public const string ByoYomiCount = "byc";
    public const string ByoYomiSeconds = "bys";

    public const string SnapshotTimestamp = "st";
    public const string MainTimeMilliseconds = "mt";
    public const string ByoYomisLeft = "byl";
    public const string ByoYomiActive = "bya";
    public const string TimeActive = "ta";
}

[Immutable, GenerateSerializer]
[Alias("Game")]
[BsonIgnoreExtraElements]
public class Game
{
    public Game(string gameId, int rows, int columns, TimeControl timeControl, List<PlayerTimeSnapshot> playerTimeSnapshots, List<MoveData> moves, Dictionary<string, StoneType> playgroundMap, Dictionary<string, StoneType> players, List<int> prisoners, string? startTime, GameState gameState, string? koPositionInLastMove, List<string> deadStones, string? winnerId, List<int> finalTerritoryScores, float komi, GameOverMethod? gameOverMethod, string? endTime,
StoneSelectionType stoneSelectionType,
string? gameCreator,
List<int>? playersRatings,
List<int>? playersRatingsDiff
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
        PlayersRatings = playersRatings;
        PlayersRatingsDiff = playersRatingsDiff;
    }

    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [Id(1)]
    public string GameId { get; set; }
    [BsonElement(GameFieldNames.Rows)]
    [Id(2)]
    public int Rows { get; set; }
    [BsonElement(GameFieldNames.Cols)]
    [Id(3)]
    public int Columns { get; set; }
    [BsonElement(GameFieldNames.TimeControl)]
    [Id(4)]
    public TimeControl TimeControl { get; set; }
    [BsonElement(GameFieldNames.PlayerTimeSnapshots)]
    [Id(5)]
    public List<PlayerTimeSnapshot> PlayerTimeSnapshots { get; set; }
    [BsonElement(GameFieldNames.PlaygroundMap)]
    [Id(6)]
    public Dictionary<string, StoneType> PlaygroundMap { get; set; }
    [BsonElement(GameFieldNames.Moves)]
    [Id(7)]
    public List<MoveData> Moves { get; set; }
    [BsonElement(GameFieldNames.Players)]
    [Id(8)]
    public Dictionary<string, StoneType> Players { get; set; }
    [BsonElement(GameFieldNames.Prisoners)]
    [Id(9)]
    public List<int> Prisoners { get; set; }
    [BsonElement(GameFieldNames.StartTime)]
    [Id(10)]
    public string? StartTime { get; set; }
    [BsonElement(GameFieldNames.KoPositionInLastMove)]
    [Id(11)]
    public string? KoPositionInLastMove { get; set; }
    [BsonElement(GameFieldNames.GameState)]
    [Id(12)]
    public GameState GameState { get; set; }
    [BsonElement(GameFieldNames.DeadStones)]
    [Id(13)]
    public List<string> DeadStones { get; set; }
    [BsonElement(GameFieldNames.WinnerId)]
    [Id(14)]
    public string? WinnerId { get; set; }
    [BsonElement(GameFieldNames.FinalTerritoryScores)]
    [Id(15)]
    public List<int> FinalTerritoryScores { get; set; }
    [BsonElement(GameFieldNames.Komi)]
    [Id(16)]
    public float Komi { get; set; }
    [BsonElement(GameFieldNames.GameOverMethod)]
    [Id(17)]
    public GameOverMethod? GameOverMethod { get; set; }
    [BsonElement(GameFieldNames.EndTime)]
    [Id(18)]
    public string? EndTime { get; set; }

    [BsonElement(GameFieldNames.StoneSelectionType)]
    [Id(19)]
    public StoneSelectionType StoneSelectionType { get; set; }

    [BsonElement(GameFieldNames.GameCreator)]
    [Id(20)]
    public string? GameCreator { get; set; }

    [BsonElement(GameFieldNames.PlayersRatings)]
    [Id(21)]
    public List<int>? PlayersRatings { get; set; }

    [BsonElement(GameFieldNames.PlayersRatingsDiff)]
    [Id(22)]
    public List<int>? PlayersRatingsDiff { get; set; }
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
    [BsonElement(GameFieldNames.MainTimeSeconds)]
    [Id(0)]
    public int MainTimeSeconds { get; set; }

    [BsonElement(GameFieldNames.IncrementSeconds)]
    [Id(1)]
    public int? IncrementSeconds { get; set; }

    [BsonElement(GameFieldNames.ByoYomiTime)]
    [Id(2)]
    public ByoYomiTime? ByoYomiTime { get; set; }

    [BsonElement(GameFieldNames.TimeStandard)]
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
    [BsonElement(GameFieldNames.ByoYomiCount)]
    [Id(0)]
    public int ByoYomis { get; set; }

    [BsonElement(GameFieldNames.ByoYomiSeconds)]
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

    [BsonElement(GameFieldNames.SnapshotTimestamp)]
    [Id(0)]
    public string SnapshotTimestamp { get; set; }

    [BsonElement(GameFieldNames.MainTimeMilliseconds)]
    [Id(1)]
    public int MainTimeMilliseconds { get; set; }

    [BsonElement(GameFieldNames.ByoYomisLeft)]
    [Id(2)]
    public int? ByoYomisLeft { get; set; }

    [BsonElement(GameFieldNames.ByoYomiActive)]
    [Id(3)]
    public bool ByoYomiActive { get; set; }

    [BsonElement(GameFieldNames.TimeActive)]
    [Id(4)]
    public bool TimeActive { get; set; }
}

public static class BoardSizeParamsExt
{
    public static bool checkIfInsideBounds(this BoardSizeParams p, Position pos)
    {
        return pos.X > -1 && pos.X < p.Rows && pos.Y < p.Columns && pos.Y > -1;
    }
}

/// <summary>
/// Helper class to store board size parameters
/// </summary>
public class BoardSizeParams
{
    public int Rows;
    public int Columns;
    public BoardSizeParams(int rows, int columns)
    {
        Rows = rows;
        Columns = columns;
    }
}