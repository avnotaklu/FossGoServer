using System.Diagnostics;
using System.Text.Json.Serialization;
using BadukServer;
using BadukServer.Orleans.Grains;
using Glicko2;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BadukServer;

public static class GameExt
{
    public static bool DidStart(this Game game)
    {
        return game.GameState != GameState.WaitingForStart;
    }

    public static bool DidEnd(this Game game)
    {
        return game.GameState == GameState.Ended;
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

    public static VariantType GetTopLevelVariant(this Game game)
    {
        return new VariantType(game.GetBoardSize(), game.TimeControl.TimeStandard);
    }

    public static VariantType GetBoardVariant(this Game game)
    {
        return new VariantType(game.GetBoardSize(), null);
    }

    public static VariantType GetTimeStandardVariant(this Game game)
    {
        return new VariantType(null, game.TimeControl.TimeStandard);
    }

    public static List<VariantType> GetRelevantVariants(this Game game)
    {
        return [
            game.GetTopLevelVariant(),
            game.GetBoardVariant(),
            game.GetTimeStandardVariant()
        ];
    }



    public static StoneType? GetStoneFromPlayerId(this Dictionary<string, StoneType> players, string id)
    {
        return players[id];
    }

    public static StoneType? GetOtherStoneFromPlayerId(this Dictionary<string, StoneType> players, string id)
    {
        return 1 - players[id];
    }

    public static string? GetOtherPlayerIdFromPlayerId(this Dictionary<string, StoneType> players, string id)
    {
        var otherStone = players.GetOtherStoneFromPlayerId(id);
        if (otherStone == null) return null;

        return players.GetPlayerIdFromStoneType((StoneType)otherStone);
    }


    public static string? GetPlayerIdFromStoneType(this Dictionary<string, StoneType> players, StoneType stone)
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

    public static List<string> GetPlayerIdSortedByColor(this Dictionary<string, StoneType> players)
    {
        var black = players.GetPlayerIdFromStoneType(StoneType.Black);
        var white = players.GetPlayerIdFromStoneType(StoneType.White);
        if (black == null || white == null)
        {
            throw new UnreachableException("Both players should be present in the game");
        }

        return new List<string> { black, white };
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

    /// <summary>
    /// Get my result
    /// </summary>
    /// <param name="game"></param>
    /// <param name="myId"></param>
    /// <returns>returns 1 for my win, -1 for op, 0 for draw, null for no result</returns>
    /// <exception cref="UnreachableException"></exception>
    public static int? MyResult(this Game game, string myId)
    {
        if (game.Result == null)
        {
            return null;
        }

        return game.Result switch
        {
            GameResult.BlackWon => game.Players[myId] == StoneType.Black ? 1 : -1,
            GameResult.WhiteWon => game.Players[myId] == StoneType.White ? 1 : -1,
            GameResult.Draw => 0,
            _ => throw new UnreachableException("Invalid game result")
        };
    }

    public static bool DidIWin(this Game game, string myId)
    {
        return game.MyResult(myId) == 1;
    }

    public static bool DidILose(this Game game, string myId)
    {
        return game.MyResult(myId) == -1;
    }
    public static bool DidIDraw(this Game game, string myId)
    {
        return game.MyResult(myId) == 0;
    }


    public static StoneType? GetWinnerPlayer(this Game game)
    {
        if (game.Result == null) return null;

        return game.Result switch
        {
            
            GameResult.BlackWon => StoneType.Black,
            GameResult.WhiteWon => StoneType.White,
            _ => throw new UnreachableException("Invalid game result")
        };
    }


    public static string? GetWinnerUser(this Game game)
    {
        if (game.Result == null) return null;

        return game.Result switch
        {
            GameResult.BlackWon => game.Players.GetPlayerIdFromStoneType(StoneType.Black),
            GameResult.WhiteWon => game.Players.GetPlayerIdFromStoneType(StoneType.White),
            GameResult.Draw => null,
            _ => throw new UnreachableException("Invalid game result")
        };
    }


    /// <summary>
    /// Get the running duration of the game
    /// </summary>
    /// <param name="game"></param>
    /// <param name="playerId"></param>
    /// <returns></returns>
    public static TimeSpan? GetRunningDurationOfGame(this Game game, DateTime? forRunningGame = null)
    {
        if (game.StartTime == null)
        {
            return null;
        }
        if (!game.DidEnd() && forRunningGame == null)
        {
            // Game didn't end and current time hasn't been given
            return null;
        }
        var latestDate = game.EndTime?.DeserializedDate() ?? forRunningGame;
        return latestDate - DateTime.Parse(game.StartTime!);
    }
}

public class GameFieldNames
{
    public const string Rows = "r";
    public const string Columns = "c";
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
    public const string Result = "res";
    public const string FinalTerritoryScores = "fts";
    public const string Komi = "k";
    public const string GameOverMethod = "gom";
    public const string StoneSelectionType = "sst";
    public const string GameCreator = "gc";
    public const string PlayersRatings = "rts";
    public const string PlayersRatingsDiff = "prd";
    public const string GameType = "ty";

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

    // GameMove
    public const string Time = "t";
    public const string X = "x";
    public const string Y = "y";
}


public static class GameTypeExt
{
    public static bool IsAllowedPlayerType(this GameType type, PlayerType playerType)
    {
        return type switch
        {
            GameType.Anonymous => playerType == PlayerType.Guest,
            GameType.Casual => playerType == PlayerType.Normal,
            GameType.Rated => playerType == PlayerType.Normal,
            _ => throw new Exception("Invalid game type")
        };
    }
}

public enum GameType
{
    Anonymous = 0,
    Casual = 1,
    Rated = 2
}

public static class GameResultExt {
    public static StoneType? GetWinnerStone(this GameResult result)
    {
        return result switch
        {
            GameResult.BlackWon => StoneType.Black,
            GameResult.WhiteWon => StoneType.White,
            GameResult.Draw => null,
            _ => throw new UnreachableException("Invalid game result")
        };
    }

    public static StoneType? GetLoserStone(this GameResult result)
    {
        return result.GetWinnerStone()?.GetOpposite();
    }
}

public enum GameResult
{
    BlackWon,
    WhiteWon,
    Draw
}

[Immutable, GenerateSerializer]
[Alias("Game")]
[BsonIgnoreExtraElements]
public class Game
{
    public Game(
        string gameId,
        int rows,
        int columns,
        TimeControl timeControl,
        List<PlayerTimeSnapshot> playerTimeSnapshots,
        List<GameMove> moves,
        Dictionary<string, StoneType> playgroundMap,
        Dictionary<string, StoneType> players,
        List<int> prisoners,
        string? startTime,
        GameState gameState,
        string? koPositionInLastMove,
        List<string> deadStones,
        GameResult? result,
        List<int> finalTerritoryScores,
        float komi,
        GameOverMethod? gameOverMethod,
        string? endTime,
        StoneSelectionType stoneSelectionType,
        string? gameCreator,
        List<int> playersRatings,
        List<int> playersRatingsDiff,
        GameType gameType
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
        Result = result;
        FinalTerritoryScores = finalTerritoryScores;
        Komi = komi;
        GameOverMethod = gameOverMethod;
        EndTime = endTime;
        StoneSelectionType = stoneSelectionType;
        GameCreator = gameCreator;
        PlayersRatings = playersRatings;
        PlayersRatingsDiff = playersRatingsDiff;
        GameType = gameType;
    }

    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [Id(1)]
    public string GameId { get; set; }
    [BsonElement(GameFieldNames.Rows)]
    [Id(2)]
    public int Rows { get; set; }
    [BsonElement(GameFieldNames.Columns)]
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
    public List<GameMove> Moves { get; set; }
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
    [BsonElement(GameFieldNames.Result)]
    [Id(14)]
    public GameResult? Result { get; set; }
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
    public List<int> PlayersRatings { get; set; }

    [BsonElement(GameFieldNames.PlayersRatingsDiff)]
    [Id(22)]
    public List<int> PlayersRatingsDiff { get; set; }
    [Id(23)]
    [BsonElement(GameFieldNames.GameType)]
    public GameType GameType { get; set; }
}

public static class StoneTypeExt
{
    public static StoneType GetOpposite(this StoneType stone)
    {
        return stone switch
        {
            StoneType.Black => StoneType.White,
            StoneType.White => StoneType.Black,
            _ => throw new UnreachableException("Invalid stone type")
        };
    }

    public static GameResult ResultForIWon(this StoneType stone)
    {
        return stone switch
        {
            StoneType.Black => GameResult.BlackWon,
            StoneType.White => GameResult.WhiteWon,
            _ => throw new UnreachableException("Invalid stone type")
        };
    }

    public static GameResult ResultForOtherWon(this StoneType stone)
    {
        return stone.GetOpposite().ResultForIWon();
    }
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

    public TimeControl(TimeControlDto data)
    {
        ByoYomiTime = data.ByoYomiTime;
        IncrementSeconds = data.IncrementSeconds;
        MainTimeSeconds = data.MainTimeSeconds;
        TimeStandard = data.GetStandard();
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