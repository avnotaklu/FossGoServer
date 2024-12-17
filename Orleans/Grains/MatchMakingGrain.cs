using System.Diagnostics;
using System.Runtime.CompilerServices;
using BadukServer;
using BadukServer.Orleans.Grains;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using Orleans;
using Tests;

public class MatchMakingGrain : Grain, IMatchMakingGrain
{
    private readonly ILogger<MatchMakingGrain> _logger;
    private readonly IPlayerInfoService _publicUserInfoService;
    private readonly IDateTimeService _dateTimeService;

    public MatchMakingGrain(ILogger<MatchMakingGrain> logger, IPlayerInfoService publicUserInfoService, IDateTimeService dateTimeService)
    {
        _publicUserInfoService = publicUserInfoService;
        _logger = logger;
        _dateTimeService = dateTimeService;
    }

    Dictionary<int, Dictionary<string, List<Match>>> _matchesByBoardAndTime = [];

    private void InitializeMatches()
    {
        foreach (var boardSize in Enum.GetValues(typeof(MatchableBoardSize)).Cast<BoardSize>())
        {
            _matchesByBoardAndTime[(int)boardSize] = new Dictionary<string, List<Match>>();
            foreach (var timeStandard in Constants.timeControlsForMatch)
            {
                _matchesByBoardAndTime[(int)boardSize][timeStandard.SimpleRepr()] = new List<Match>();
            }
        }
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        InitializeMatches();
        await base.OnActivateAsync(cancellationToken);
    }

    public async ValueTask FindMatch(PlayerInfo playerInfo, List<MatchableBoardSize> boardSizes, List<TimeControlDto> timeStandards)
    {
        var wantedGameType = playerInfo.PlayerType.GetGameType(RankedOrCasual.Rated);

        var matchingMatches = boardSizes.Select(b => timeStandards.Select(t => _matchesByBoardAndTime.GetOrNull((int)b)?.GetOrNull(t.SimpleRepr()))).SelectMany(m => m).SelectMany(m => m ?? []).Where(m => m.GameType.IsAllowedPlayerType(playerInfo.PlayerType)).ToList();

        Match? match;
        if (wantedGameType == GameType.Rated)
        {
            if (playerInfo.Rating == null) throw new InvalidOperationException("Unrated Player can't play a rated game");

            match = matchingMatches.FirstOrDefault();
            // match = matchingMatches.FirstOrDefault(m =>
            // {
            //     var variant = new VariantType(m.BoardSize.ToBoardSize(), m.TimeControl.GetStandard());
            //     return m.CreatorRating!.RatingRangeOverlap(playerInfo.Rating!.GetRatingData(variant));
            // });
        }
        else
        {
            match = matchingMatches.FirstOrDefault();
        }

        var finderId = playerInfo.Id;
        var finderRating = playerInfo.Rating;

        if (match == null)
        {
            boardSizes.ForEach(b => timeStandards.ForEach(t => _matchesByBoardAndTime[(int)b][t.SimpleRepr()].Add(new Match(
                finderId,
                finderRating?.GetRatingData(new ConcreteGameVariant(b.ToBoardSize(), t.GetStandard())),
                b,
                t,
                playerInfo.PlayerType.GetGameType(RankedOrCasual.Rated)
            ))));

            return;
        }

        if (match.CreatorId == finderId)
        {
            // What to do here?? 
            return;
        }

        _matchesByBoardAndTime[(int)match.BoardSize.ToBoardSize()][match.TimeControl.SimpleRepr()].Remove(match);
        var gameGrain = GrainFactory.GetGrain<IGameGrain>(ObjectId.GenerateNewId().ToString());

        // REVIEW: Getting match creator info using finder type, i'm assuming that the creator is the same type as finder
        var publicInfos = (await Task.WhenAll(new List<string>([match.CreatorId, finderId]).Select(async p => await _publicUserInfoService.GetPublicUserInfoForPlayer(p, playerInfo.PlayerType) ?? throw new Exception($"Player info wasn't fetched {p}")))).ToList();

        var game = await gameGrain.StartMatch(match, publicInfos);

        foreach (var player in game.Players)
        {
            var playerGrain = GrainFactory.GetGrain<IPlayerGrain>(player);
            await playerGrain.JoinGame(game.GameId, _dateTimeService.Now());
        }
    }
}

[Immutable, GenerateSerializer]
[Alias("Match")]
public class Match
{
    [Id(0)]
    public string CreatorId { get; set; }
    [Id(1)]
    public PlayerRatingsData? CreatorRating { get; set; }
    [Id(2)]
    public MatchableBoardSize BoardSize { get; set; }
    [Id(3)]
    public TimeControlDto TimeControl { get; set; }
    [Id(4)]
    public GameType GameType { get; set; }
    public StoneSelectionType StoneType => StoneSelectionType.Auto;

    public Match(string creatorId, PlayerRatingsData? creatorRating, MatchableBoardSize boardSizes, TimeControlDto timeControl, GameType gameType)
    {
        CreatorId = creatorId;
        CreatorRating = creatorRating;
        BoardSize = boardSizes;
        TimeControl = timeControl;
        GameType = gameType;
    }
}


[Immutable, GenerateSerializer]
[Alias("FindMatchDto")]
public class FindMatchDto
{
    [Id(0)]
    public List<MatchableBoardSize> BoardSizes { get; set; }
    [Id(1)]
    public List<TimeControlDto> TimeStandards { get; set; }

    public FindMatchDto(List<MatchableBoardSize> boardSizes, List<TimeControlDto> timeStandards)
    {
        BoardSizes = boardSizes;
        TimeStandards = timeStandards;
    }
}


[Immutable, GenerateSerializer]
[Alias("FindMatchResult")]
public class FindMatchResult
{
    [Id(0)]
    public List<PlayerInfo> JoinedPlayers { get; set; }

    [Id(1)]
    public Game Game { get; set; }

    public FindMatchResult(List<PlayerInfo> joinedUsers, Game game)
    {
        JoinedPlayers = joinedUsers;
        Game = game;
    }

}

public static class MatchableBoardSizeExt
{
    public static BoardSize ToBoardSize(this MatchableBoardSize size) => (BoardSize)size;
    public static BoardSizeParams ToBoardSizeData(this MatchableBoardSize size)
    {
        return size switch
        {
            MatchableBoardSize.Nine => new BoardSizeParams(9, 9),
            MatchableBoardSize.Thirteen => new BoardSizeParams(13, 13),
            MatchableBoardSize.Nineteen => new BoardSizeParams(19, 19),
            _ => throw new Exception("Invalid matchable board size")
        };
    }

}


[GenerateSerializer]
public enum MatchableBoardSize
{
    Nine = 0,
    Thirteen = 1,
    Nineteen = 2,
}


// public static class MatchableTimeStandardExt
// {
//     public static TimeStandard ToTimeStandard(this MatchableTimeStandard standard) => (TimeStandard)standard;
// }


// [GenerateSerializer]
// public enum MatchableTimeStandard
// {
//     Blitz = 0,
//     Rapid = 1,
//     Classical = 2,
//     Correspondence = 3,
// }