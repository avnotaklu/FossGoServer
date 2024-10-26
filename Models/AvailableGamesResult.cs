using BadukServer;

public class AvailableGamesResult {
    public List<Game> Games {get; set;}

    public AvailableGamesResult(List<Game> games)
    {
        Games = games;
    }
}