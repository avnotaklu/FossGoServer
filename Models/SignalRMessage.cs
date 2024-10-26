[Immutable, GenerateSerializer]
public record class SignalRMessage
{
    public string Type { get; set; }
    public object Data { get; set; }

    public SignalRMessage(object data, string type)
    {
        Data = data;
        Type = type;
    }
}

[Immutable, GenerateSerializer]

public record class SignalRMessagesBatch(List<SignalRMessage> Messages);