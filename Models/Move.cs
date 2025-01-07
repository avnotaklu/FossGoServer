using System.CodeDom;
using System.Diagnostics;
using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;

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


[Immutable, GenerateSerializer]
[Alias("GameMove")]
public class GameMove
{
    [BsonElement("x")]
    [Id(0)]
    public int? X { get; set; }

    [BsonElement("y")]
    [Id(1)]
    public int? Y { get; set; }

    [BsonElement("t")]
    [Id(2)]
    public int SecondsAfterStart { get; set; }

    public GameMove(int? x, int? y, int secondsAfterStart)
    {
        Debug.Assert((x == null && y == null) || (x != null && y != null));
        X = x;
        Y = y;
        SecondsAfterStart = secondsAfterStart;
    }

    public bool IsPass()
    {
        return X == null;
    }
};

[Immutable, GenerateSerializer]
public class MovePosition
{
    // [Id(0)] public string PlayerId { get; set; }
    [Id(0)]
    public int? X { get; set; }
    [Id(1)]
    public int? Y { get; set; }

    public MovePosition(int? x, int? y)
    {
        Debug.Assert((x == null && y == null) || (x != null && y != null));
        X = x;
        Y = y;
    }

    public bool IsPass()
    {
        return X == null;
    }
};
