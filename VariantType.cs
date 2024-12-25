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
            _ => false,
        };
    }

    public static string ToKey(this BoardSize me)
    {
        return $"{(int)me}";
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


    public static List<int?> NonDimsMatchingToOther()
    {
        return new List<int?> { 9, 13, 19 };
    }

    public static int? MatchingDims(this BoardSize board)
    {
        switch (board)
        {
            case BoardSize.Nine:
                return 9;
            case BoardSize.Thirteen:
                return 13;
            case BoardSize.Nineteen:
                return 19;
            default:
                return null;
                // throw new Exception("cannot match rows");
        }
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
        return $"{(int)me}";
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
    public static bool RatingAllowed(this ConcreteGameVariant me)
    {
        return me.BoardSize.RatingAllowed()! && me.TimeStandard.RatingAllowed()!;
    }

    public static bool StatAllowed(this ConcreteGameVariant me)
    {
        return me.BoardSize.RatingAllowed()! && me.TimeStandard.RatingAllowed()!;
    }

    public static string ToKey(this ConcreteGameVariant me)
    {
        var bs = me.BoardSize.ToKey();
        var ts = me.TimeStandard.ToKey();

        return $"{bs}_{ts}";
    }

    public static ConcreteGameVariant FromKey(string key)
    {
        var parts = key.Split('_');

        var bs = parts[0];
        var bdS = (BoardSize)int.Parse(bs);

        var ts = parts[1];
        var timeS = (TimeStandard)int.Parse(ts);

        return new ConcreteGameVariant(bdS, timeS);
    }
}


[Immutable, GenerateSerializer]
[Alias("ConcreteVariantType")]
public class ConcreteGameVariant
{
    public BoardSize BoardSize { get; set; }
    public TimeStandard TimeStandard { get; set; }

    public ConcreteGameVariant(BoardSize boardSize, TimeStandard timeStandard)
    {
        BoardSize = boardSize;
        TimeStandard = timeStandard;
    }

    public override string ToString()
    {
        return $"BoardSize: {BoardSize}, TimeStandard: {TimeStandard}";
    }

    public override bool Equals(object? obj)
    {
        if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }

        var other = (ConcreteGameVariant)obj;

        return BoardSize == other.BoardSize && TimeStandard == other.TimeStandard;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(BoardSize, TimeStandard);
    }
}