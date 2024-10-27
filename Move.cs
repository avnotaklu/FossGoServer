using System.CodeDom;

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
public struct MovePosition
{
    // [Id(0)] public string PlayerId { get; set; }
    [Id(1)] public int X { get; set; }
    [Id(2)] public int Y { get; set; }
};
