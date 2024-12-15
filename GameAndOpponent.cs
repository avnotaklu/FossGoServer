using BadukServer;

[Immutable, GenerateSerializer]
[Alias("GameAndOpponent")]
public class GameAndOpponent {
    public Game Game {get; set;}
    public PlayerInfo Opponent {get;set ;}

    public GameAndOpponent(PlayerInfo opponent, Game game)
    {
        Game = game;
        Opponent = opponent;
    }
}