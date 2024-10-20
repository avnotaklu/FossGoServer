
public interface IPairingGrain : IGrainWithIntegerKey
{
    // Task AddGame(Guid gameId);

    // Task RemoveGame(Guid gameId);

    // Task<PairingSummary[]> GetGames();
}

[Immutable]
[GenerateSerializer]
public class PairingSummary
{
    [Id(0)] public Guid GameId { get; set; }
}

