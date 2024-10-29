using System.Reflection.Metadata;

[Immutable, GenerateSerializer]
public record class SignalRMessage
{
    public string Type { get; set; }
    public object? Data { get; set; }

    public SignalRMessage(string type, object? data )
    {
        Data = data;
        Type = type;
    }
}
static class SignalRMessageType
{
    public const string newGame = "NewGame";
    public const string newMove = "NewMove";
    public const string gameJoin = "GameJoin";
    public const string scoreCaculationStarted = "ScoreCaculationStarted";
}

[Immutable, GenerateSerializer]

public record class SignalRMessagesBatch(List<SignalRMessage> Messages);