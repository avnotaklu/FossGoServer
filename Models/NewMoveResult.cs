using BadukServer;

public class NewMoveResult {
    public Game Game { get; set; }
    public bool Result { get; set; }

    public NewMoveResult(Game game, bool result)
    {
        Game = game;
        Result = result;
    }

}