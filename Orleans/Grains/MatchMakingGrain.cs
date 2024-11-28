using BadukServer;
using BadukServer.Orleans.Grains;
using Orleans;
using Tests;

public class MatchMakingGrain : Grain, IMatchMakingGrain
{
    Dictionary<int, Dictionary<int, List<Match>>> _matchesByBoardAndTime = [];

    public async Task FindMatch(string finderId, PlayerRatingData finderRating, List<BoardSize> boardSizes, List<TimeStandard> timeStandards)
    {
        var matchingMatches = boardSizes.Select(b => timeStandards.Select(t => _matchesByBoardAndTime[(int)b][(int)t])).SelectMany(m => m).SelectMany(m => m).ToList();

        var ratingRange = finderRating.GetRatingRange();

        var match = matchingMatches.FirstOrDefault(m => m.CreatorRating.RatingRangeOverlap(finderRating));

        if (match == null)
        {
            match = new Match(
                finderId,
                finderRating,
                boardSizes,
                timeStandards
            );

            boardSizes.ForEach(b => timeStandards.ForEach(t => _matchesByBoardAndTime[(int)b][(int)t].Add(match)));
        }
        else
        {
            var gameGrain = GrainFactory.GetGrain<IGameGrain>(Guid.NewGuid().ToString());
            var game = await gameGrain.StartMatch(match, finderId);

            foreach (var player in game.Players.Keys)
            {
                var pushGrain = GrainFactory.GetGrain<IPushNotifierGrain>(player);
                await pushGrain.SendMessageToMe(
                    new SignalRMessage(
                        SignalRMessageType.matchFound,
                        new FindMatchResult(match, true, game)
                    )
                );
            }

            // return new FindMatchResult(match, true, game);
        }
        // return new FindMatchResult(match, false, null);
    }
}

[Immutable, GenerateSerializer]
[Alias("Match")]
public class Match
{
    [Id(0)]
    public string CreatorId { get; set; }
    [Id(1)]
    public PlayerRatingData CreatorRating { get; set; }
    [Id(2)]
    public List<BoardSize> BoardSizes { get; set; }
    [Id(3)]
    public List<TimeStandard> TimeStandards { get; set; }
    public StoneSelectionType StoneType => StoneSelectionType.Auto;

    public Match(string creatorId, PlayerRatingData creatorRating, List<BoardSize> boardSizes, List<TimeStandard> timeStandards)
    {
        CreatorId = creatorId;
        CreatorRating = creatorRating;
        BoardSizes = boardSizes;
        TimeStandards = timeStandards;
    }
}

public class FindMatchDto
{
    [Id(0)]
    public List<BoardSize> BoardSizes { get; set; }
    [Id(1)]
    public List<TimeStandard> TimeStandards { get; set; }

    public FindMatchDto(List<BoardSize> boardSizes, List<TimeStandard> timeStandards)
    {
        BoardSizes = boardSizes;
        TimeStandards = timeStandards;
    }
}

public class FindMatchResult
{
    [Id(0)]
    public Match Match { get; set; }
    [Id(1)]
    public Game? Game { get; set; }
    [Id(2)]
    public bool MatchFound { get; set; }

    public FindMatchResult(Match match, bool matchFound, Game? game)
    {
        Match = match;
        MatchFound = matchFound;
        Game = game;
    }

}