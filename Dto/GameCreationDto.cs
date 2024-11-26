using System.Diagnostics;
using System.Reflection.Metadata;
using MongoDB.Bson.Serialization.Conventions;

namespace BadukServer;

public class GameCreationDto
{
    public GameCreationDto(int rows, int columns, TimeControlData timeControl, StoneSelectionType firstPlayerStone)
    {
        Rows = rows;
        Columns = columns;
        TimeControl = timeControl;
        FirstPlayerStone = firstPlayerStone;
    }

    public int Rows { get; set; }
    public int Columns { get; set; }
    public StoneSelectionType FirstPlayerStone { get; set; }
    public TimeControlData TimeControl { get; set; }
}


[Immutable, GenerateSerializer]
[Alias("TimeControlData")]
public class TimeControlData
{
    [Id(0)]
    public int MainTimeSeconds { get; set; }

    [Id(1)]
    public int? IncrementSeconds { get; set; }
    [Id(2)]
    public ByoYomiTime? ByoYomiTime { get; set; }

    public TimeControlData(int mainTimeSeconds, int? incrementSeconds, ByoYomiTime? byoYomiTime)
    {
        Debug.Assert(mainTimeSeconds > 0);
        Debug.Assert(incrementSeconds == null || incrementSeconds > 0);
        Debug.Assert(byoYomiTime == null || byoYomiTime.ByoYomiSeconds > 0);
        Debug.Assert(byoYomiTime == null || byoYomiTime.ByoYomis > 0);

        MainTimeSeconds = mainTimeSeconds;
        IncrementSeconds = incrementSeconds;
        ByoYomiTime = byoYomiTime;
    }

    public TimeStandard GetTimeStandard()
    {
        return TimeStandards.GetStandard(this);
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
    public static Dictionary<TimeStandard, (TimeSpan min, TimeSpan max)> Times = new Dictionary<TimeStandard, (TimeSpan min, TimeSpan max)> {
        { TimeStandard.Blitz, (TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(20)) },
        { TimeStandard.Rapid, (TimeSpan.FromMinutes(20), TimeSpan.FromMinutes(40)) },
        { TimeStandard.Classical, (TimeSpan.FromMinutes(40), TimeSpan.FromMinutes(120)) },
        { TimeStandard.Correspondence, (TimeSpan.FromDays(1), TimeSpan.FromDays(7)) },
    };

    public static TimeStandard GetStandard(TimeControlData data)
    {
        var mins = TimeSpan.FromSeconds(data.MainTimeSeconds);

        foreach (var (standard, (min, max)) in Times)
        {
            if (mins >= min && mins <= max)
            {
                return standard;
            }
        }
        return TimeStandard.Unknown;

    }
}