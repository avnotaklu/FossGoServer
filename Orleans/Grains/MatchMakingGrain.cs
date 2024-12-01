using System.Runtime.CompilerServices;
using BadukServer;
using BadukServer.Orleans.Grains;
using MongoDB.Bson;
using Orleans;
using Tests;

public class MatchMakingGrain : Grain, IMatchMakingGrain
{
    private readonly ILogger<MatchMakingGrain> _logger;
    private readonly IPublicUserInfoService _publicUserInfoService;

    public MatchMakingGrain(ILogger<MatchMakingGrain> logger, IPublicUserInfoService publicUserInfoService)
    {
        _publicUserInfoService = publicUserInfoService;
        _logger = logger;
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

    public async ValueTask FindMatch(string finderId, UserRating finderRating, List<MatchableBoardSize> boardSizes, List<TimeControlDto> timeStandards)
    {
        var matchingMatches = boardSizes.Select(b => timeStandards.Select(t => _matchesByBoardAndTime.GetOrNull((int)b)?.GetOrNull(t.SimpleRepr()))).SelectMany(m => m).SelectMany(m => m ?? []).ToList();

        // var match = matchingMatches.FirstOrDefault(m => m.CreatorRating.RatingRangeOverlap(finderRating.GetRatingData(m.BoardSize.ToBoardSize(), m.TimeControl.GetStandard())));
        var match = matchingMatches.FirstOrDefault();

        if (match == null)
        {
            boardSizes.ForEach(b => timeStandards.ForEach(t => _matchesByBoardAndTime[(int)b][t.SimpleRepr()].Add(new Match(
                finderId,
                finderRating.GetRatingData(b.ToBoardSize(), t.GetStandard()),
                b,
                t
            ))));
        }
        else if (match.CreatorId != finderId)
        {
            _matchesByBoardAndTime[(int)match.BoardSize.ToBoardSize()][match.TimeControl.SimpleRepr()].Remove(match);
            var gameGrain = GrainFactory.GetGrain<IGameGrain>(ObjectId.GenerateNewId().ToString());

            var game = await gameGrain.StartMatch(match, finderId);

            // await gameGrain.CreateGame(
            //     rows: match.BoardSize.ToBoardSizeData().Rows,
            //     columns: match.BoardSize.ToBoardSizeData().Columns,
            //     timeControl: match.TimeControl,
            //     stoneSelectionType: match.StoneType,
            //     gameCreator: null
            // );

            // var (game, otherPlayerInfos) = await gameGrain.JoinGame(finderId, DateTime.Now.SerializedDate());


            // else
            {
                foreach (var player in game.Players.Keys)
                {

                    var otherInfo = await _publicUserInfoService.GetPublicUserInfo(game.Players.GetOtherPlayerIdFromPlayerId(player)!);
                    if (otherInfo == null) throw new Exception("Public info was null");
                    // var publicInfos = (await Task.WhenAll(game.Players.Keys.Select(async p => await _publicUserInfoService.GetPublicUserInfo(p) ?? throw new Exception($"Player info wasn't fetched {p}")))).ToList();
                    // if (player == finderId) continue;
                    var playerGrain = GrainFactory.GetGrain<IPlayerGrain>(player);
                    var pushGrain = GrainFactory.GetGrain<IPushNotifierGrain>(await playerGrain.GetConnectionId());
                    pushGrain.SendMessageToMe(
                        new SignalRMessage(
                            SignalRMessageType.gameJoin,
                            new GameJoinResult(
                                game,
                                otherInfo,
                                game.StartTime!
                            )
                        // new FindMatchResult(publicInfos, game)
                        )
                    );
                }
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
    public MatchableBoardSize BoardSize { get; set; }
    [Id(3)]
    public TimeControlDto TimeControl { get; set; }
    public StoneSelectionType StoneType => StoneSelectionType.Auto;

    public Match(string creatorId, PlayerRatingData creatorRating, MatchableBoardSize boardSizes, TimeControlDto timeControl)
    {
        CreatorId = creatorId;
        CreatorRating = creatorRating;
        BoardSize = boardSizes;
        TimeControl = timeControl;
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
    public List<PublicUserInfo> JoinedPlayers { get; set; }

    [Id(1)]
    public Game Game { get; set; }

    public FindMatchResult(List<PublicUserInfo> joinedUsers, Game game)
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