namespace BadukServer;

[Immutable, GenerateSerializer]
public class JoinMessage
{
    public JoinMessage(string gameId)
    {
        GameId = gameId;
    }

    public string GameId { get; set; }
}

[Immutable, GenerateSerializer]
public record class JoinMessagesBatch(List<JoinMessage> Messages);
