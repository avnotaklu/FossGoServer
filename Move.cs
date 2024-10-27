using System.CodeDom;
using System.Diagnostics;

namespace BadukServer;
//
// public class Move
// {
//     public Move(MoveType moveType,  DateTime moveTime, StoneMoveDetails? stoneMoveDetails)
//     {
//         MoveType = moveType;
//         StoneMoveDetails = stoneMoveDetails;
//         MoveTime = moveTime;
//     }
//
//     public MoveType MoveType { get; set; }
//     public DateTime MoveTime { get; set; }
//     public StoneMoveDetails? StoneMoveDetails { get; set; }
// }
//
//
// [Serializable]
// public enum MoveType 
// {
//     Stone,
//     Pass
// }
//
// public class StoneMoveDetails
// {
//     public int? X { get; set; }
//     public int? Y { get; set; }
// }


[GenerateSerializer]
public class MoveData
{
    // [Id(0)] public string PlayerId { get; set; }
    public int? X { get; set; }
    public int? Y { get; set; }
    public string Time { get; set; }

    public MoveData(int? x, int? y, string time)
    {
        Debug.Assert((x == null && y == null) || (x != null && y != null));
        Time = time;
        X = x;
        Y = y;
    }

    public bool IsPass()
    {
        return X == null;
    }
};

[GenerateSerializer]
public class MovePosition
{
    // [Id(0)] public string PlayerId { get; set; }
    public int? X { get; set; }
    public int? Y { get; set; }

    public MovePosition(int? x, int? y)
    {
        Debug.Assert((x == null && y == null) || (x != null && y != null));
    }

    public bool IsPass()
    {
        return X == null;
    }
};
