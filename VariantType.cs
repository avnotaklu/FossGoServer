using System.Diagnostics;
using BadukServer;
using Orleans.Concurrency;

public static class BoardSizeExtensions
{
    public static bool RatingAllowed(this BoardSize boardSize)
    {
        return boardSize switch
        {
            BoardSize.Nine => true,
            BoardSize.Thirteen => true,
            BoardSize.Nineteen => true,
            _ => false,
        };
    }

    public static bool StatAllowed(this BoardSize boardSize)
    {
        return boardSize switch
        {
            BoardSize.Nine => true,
            BoardSize.Thirteen => true,
            BoardSize.Nineteen => true,
            _ => true,
        };
    }

    public static string ToKey(this BoardSize me)
    {
        return $"B{(int)me}";
    }


    public static BoardSizeParams GetBoardSizeParams(this BoardSize boardSize)
    {
        return boardSize switch
        {
            BoardSize.Nine => new BoardSizeParams(9, 9),
            BoardSize.Thirteen => new BoardSizeParams(13, 13),
            BoardSize.Nineteen => new BoardSizeParams(19, 19),
            _ => throw new UnreachableException("Board size not supported")
        };
    }
}

[GenerateSerializer]
public enum BoardSize
{
    Nine = 0,
    Thirteen = 1,
    Nineteen = 2,
    Other = 3
}

public static class TimeStandardExt
{
    public static bool RatingAllowed(this TimeStandard timeStandard)
    {
        return timeStandard switch
        {
            TimeStandard.Blitz => true,
            TimeStandard.Rapid => true,
            TimeStandard.Classical => true,
            TimeStandard.Correspondence => true,
            _ => false,
        };
    }

    public static bool StatAllowed(this TimeStandard timeStandard)
    {
        return timeStandard switch
        {
            TimeStandard.Blitz => true,
            TimeStandard.Rapid => true,
            TimeStandard.Classical => true,
            TimeStandard.Correspondence => true,
            _ => true,
        };
    }

    public static string ToKey(this TimeStandard me)
    {
        return $"S{(int)me}";
    }
}

[GenerateSerializer]
public enum TimeStandard
{
    Blitz = 0,
    Rapid = 1,
    Classical = 2,
    Correspondence = 3,
}

public static class VariantTypeExt
{
    public static bool RatingAllowed(this VariantType me)
    {
        if (me.TimeStandard == null) return false;
        if (me.BoardSize == null) return (bool)me.TimeStandard?.RatingAllowed()!;
        return (bool)me.BoardSize?.RatingAllowed()! && (bool)me.TimeStandard?.RatingAllowed()!;
    }

    public static bool StatAllowed(this VariantType me)
    {
        if (me.TimeStandard == null) return true;
        if (me.BoardSize == null) return true;

        return (bool)me.BoardSize?.RatingAllowed()! && (bool)me.TimeStandard?.RatingAllowed()!;
    }

    public static string ToKey(this VariantType me)
    {
        var bs = me.BoardSize?.ToKey();
        var ts = me.TimeStandard?.ToKey();

        switch (bs, ts)
        {
            case (null, null):
                return "o";
            case (null, string t):
                return $"_{t}";
            case (string b, null):
                return $"{b}_";
            case (string b, string t):
                return $"{b}_{t}";
            default:
                throw new UnreachableException("Invalid variant type");
        }
    }

    public static VariantType FromKey(string key)
    {
        if (key == "o") return new VariantType(null, null);
        var parts = key.Split('_');

        BoardSize? bdS = null;
        TimeStandard? timeS = null;

        if (parts[0] != "")
        {
            var bs = parts[0].Substring(1);
            bdS = (BoardSize)int.Parse(bs);
        }

        if (parts[1] != "")
        {
            var ts = parts[1].Substring(1);
            timeS = (TimeStandard)int.Parse(ts);
        }


        return new VariantType(bdS, timeS);
    }
}


[Immutable, GenerateSerializer]
[Alias("VariantType")]
public class VariantType
{
    public BoardSize? BoardSize { get; set; }
    public TimeStandard? TimeStandard { get; set; }

    public VariantType(BoardSize? boardSize, TimeStandard? timeStandard)
    {
        BoardSize = boardSize;
        TimeStandard = timeStandard;
    }

    public override string ToString()
    {
        return $"BoardSize: {BoardSize}, TimeStandard: {TimeStandard}";
    }
}