using System.Diagnostics;
using System.Reflection.Metadata;
using MongoDB.Bson.Serialization.Conventions;

namespace BadukServer;

[GenerateSerializer]
public enum RankedOrCasual
{
    Rated = 0,
    Casual = 1
}

[Immutable, GenerateSerializer]
[Alias("GameCreationData")]
public class GameCreationData
{
    public GameCreationData(int rows, int columns, TimeControlDto timeControl, StoneSelectionType firstPlayerStone)
    {
        Rows = rows;
        Columns = columns;
        TimeControl = timeControl;
        FirstPlayerStone = firstPlayerStone;
    }
    [Id(0)]
    public int Rows { get; set; }
    [Id(1)]
    public int Columns { get; set; }
    [Id(2)]
    public StoneSelectionType FirstPlayerStone { get; set; }
    [Id(3)]
    public TimeControlDto TimeControl { get; set; }
}

public static class GameCreationDtoExt {
    public static GameCreationData ToData(this GameCreationDto dto)
    {
        return new GameCreationData(dto.Rows, dto.Columns, dto.TimeControl, dto.FirstPlayerStone);
    }
}


[Immutable, GenerateSerializer]
[Alias("GameCreationDto")]
public class GameCreationDto
{
    public GameCreationDto(int rows, int columns, TimeControlDto timeControl, StoneSelectionType firstPlayerStone, RankedOrCasual rankedOrCasual)
    {
        Rows = rows;
        Columns = columns;
        TimeControl = timeControl;
        FirstPlayerStone = firstPlayerStone;
        RankedOrCasual = rankedOrCasual;
    }
    [Id(0)]
    public int Rows { get; set; }
    [Id(1)]
    public int Columns { get; set; }
    [Id(2)]
    public StoneSelectionType FirstPlayerStone { get; set; }
    [Id(3)]
    public TimeControlDto TimeControl { get; set; }
    [Id(4)]
    public RankedOrCasual RankedOrCasual { get; set; }
}

public static class TimeControlDtoExt
{
    public static string SimpleRepr(this TimeControlDto timeControl)
    {
        var ms = timeControl.MainTimeSeconds;
        var ins = timeControl.IncrementSeconds;
        var bys = timeControl.ByoYomiTime?.ByoYomiSeconds;
        var byc = timeControl.ByoYomiTime?.ByoYomis;

        if (ins == null && bys == null)
        {
            return $"{ms}";
        }
        else if (ins == null)
        {
            return $"{ms}+{bys}x{byc}";
        }
        else if (bys == null)
        {
            return $"{ms}+{ins}";
        }
        else
        {
            throw new Exception("Invalid time control");
        }
    }

    public static Dictionary<TimeStandard, (TimeSpan min, TimeSpan max)> Times = new Dictionary<TimeStandard, (TimeSpan min, TimeSpan max)> {
        { TimeStandard.Blitz, (TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(5)) },
        { TimeStandard.Rapid, (TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(20)) },
        { TimeStandard.Classical, (TimeSpan.FromMinutes(20), TimeSpan.FromMinutes(120)) },
        { TimeStandard.Correspondence, (TimeSpan.FromDays(1), TimeSpan.FromDays(7)) },
    };

    public static TimeStandard GetStandard(this TimeControlDto data)
    {
        var mins = TimeSpan.FromSeconds(data.MainTimeSeconds);

        foreach (var (standard, (min, max)) in Times)
        {
            if (mins >= min && mins <= max)
            {
                return standard;
            }
        }

        throw new Exception("Invalid time control");
    }

    public static TimeControlDto FromRepr(string repr)
    {
        var parts = repr.Split('+');
        var MainTimeSeconds = int.Parse(parts[0]);
        int? IncrementSeconds;
        ByoYomiTime? ByoYomiTime;
        if (parts.Length == 1)
        {
            IncrementSeconds = null;
            ByoYomiTime = null;
        }
        else
        {
            var secondPart = parts[1].Split('x');
            if (secondPart.Length == 1)
            {
                IncrementSeconds = int.Parse(secondPart[0]);
                ByoYomiTime = null;
            }
            else
            {
                IncrementSeconds = null;
                ByoYomiTime = new ByoYomiTime(int.Parse(secondPart[0]), int.Parse(secondPart[1]));
            }
        }

        return new TimeControlDto(MainTimeSeconds, IncrementSeconds, ByoYomiTime);
    }
}

[Immutable, GenerateSerializer]
[Alias("TimeControlDto")]
public class TimeControlDto
{
    [Id(0)]
    public int MainTimeSeconds { get; set; }

    [Id(1)]
    public int? IncrementSeconds { get; set; }
    [Id(2)]
    public ByoYomiTime? ByoYomiTime { get; set; }

    public TimeControlDto(int mainTimeSeconds, int? incrementSeconds, ByoYomiTime? byoYomiTime)
    {
        Debug.Assert(mainTimeSeconds > 0);
        Debug.Assert(incrementSeconds == null || incrementSeconds > 0);
        Debug.Assert(byoYomiTime == null || byoYomiTime.ByoYomiSeconds > 0);
        Debug.Assert(byoYomiTime == null || byoYomiTime.ByoYomis > 0);

        MainTimeSeconds = mainTimeSeconds;
        IncrementSeconds = incrementSeconds;
        ByoYomiTime = byoYomiTime;
    }
}

[GenerateSerializer]
public enum StoneSelectionType
{
    Black = 0,
    White = 1,
    Auto = 1,
}

public static class TimeStandards
{

}