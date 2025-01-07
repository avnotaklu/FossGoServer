using BadukServer;

[Immutable, GenerateSerializer]
[Alias("GamePlayersAggregate")]
public class GamePlayersAggregate
{
    public Game Game { get; set; }
    public List<PlayerInfo> Players { get; set; }

    public GamePlayersAggregate(Game game, List<PlayerInfo> players)
    {
        Game = game;
        Players = players;
    }
}