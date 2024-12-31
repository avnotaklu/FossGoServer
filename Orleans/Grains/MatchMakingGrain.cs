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

    Dictionary<Match, LinkedList<string>> _matches = [];
    Dictionary<string, List<(Match, LinkedListNode<string>)>> _players = [];

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnActivateAsync(cancellationToken);
    }

    private string? lookupMatch(Match match, string finder)
    {
        if (!_matches.ContainsKey(match)) return null;
        var matches = _matches[match];
        if (matches.Count == 0) return null;

        var player = matches.First!.Value;
        if (player == finder) return null;

        matches.RemoveFirst();
        return player;
    }


    public async ValueTask FindMatch(PlayerInfo playerInfo, List<MatchableBoardSize> boardSizes, List<TimeControlDto> timeStandards)
    {
        var finderId = playerInfo.Id;
        var finderRating = playerInfo.Rating;
        var wantedGameType = playerInfo.PlayerType.GetGameType(RankedOrCasual.Rated);

        var lookedMatches = boardSizes.SelectMany(b => timeStandards.Select(t =>
        {
            var variant = new ConcreteGameVariant(b.ToBoardSize(), t.GetStandard());
            var wantedGameTypeRating = playerInfo.Rating?.GetRatingDataOrInitial(variant);
            var wantedRatingRange = wantedGameTypeRating?.GetRatingRange();
            return new Match(b, t, wantedGameType, wantedRatingRange);
        }));

        Match? match = null;
        string? matchingPlayer = null;

        foreach (var m in lookedMatches)
        {
            matchingPlayer = lookupMatch(m, finderId);
            if (matchingPlayer != null)
            {
                match = m;
                break;
            }
        }

        if (!match.HasValue || matchingPlayer == null)
        {
            List<(Match, LinkedListNode<string>)> playerMatches = new List<(Match, LinkedListNode<string>)>();
            foreach (var m in lookedMatches)
            {
                if (!_matches.ContainsKey(m))
                {
                    _matches[m] = new LinkedList<string>();
                }
                var refr = _matches[m].AddLast(playerInfo.Id);
                playerMatches.Add((m, refr));
            }
            _players[finderId] = playerMatches;
            return;
        }
        else
        {
            await StartGame(finderId, match.GetValueOrDefault(), matchingPlayer);
        }
    }

    private async Task StartGame(string finderId, Match match, string matchingPlayer)
    {
        var gameGrain = GrainFactory.GetGrain<IGameGrain>(ObjectId.GenerateNewId().ToString());

        // REVIEW: Getting match creator info using finder type, i'm assuming that the creator is the same type as finder
        var publicInfos = (await Task.WhenAll(new List<string>([matchingPlayer, finderId]).Select(async p => await _publicUserInfoService.GetPublicUserInfoForPlayer(p, match.GameType.AllowedPlayerType()) ?? throw new Exception($"Player info wasn't fetched {p}")))).ToList();

        var (game, time) = await gameGrain.StartMatch(match, publicInfos);

        await InformGameStart(publicInfos, game, time);
    }

    private async Task InformGameStart(List<PlayerInfo> publicInfos, Game game, DateTime time)
    {
        foreach (var player in publicInfos)
        {
            var playerGrain = GrainFactory.GetGrain<IPlayerGrain>(player.Id);
            await playerGrain.InformMyJoin(game, publicInfos, time, PlayerJoinMethod.Match);
            await playerGrain.AddActiveGame(game.GameId);
        }
    }

    public Task CancelFind(string player)
    {
        if (!_players.ContainsKey(player)) return Task.CompletedTask;

        var matches = _players[player];
        foreach (var match in matches)
        {
            _matches[match.Item1].Remove(match.Item2);
        }
        _players.Remove(player);

        return Task.CompletedTask;
    }
}

[Immutable, GenerateSerializer]
[Alias("Match")]
public struct Match
{
    [Id(0)]
    public MatchableBoardSize BoardSize { get; set; }
    [Id(1)]
    public TimeControlDto TimeControl { get; set; }
    [Id(2)]
    public GameType GameType { get; set; }
    [Id(3)]
    public RatingRange? Range { get; set; }

    public Match(MatchableBoardSize boardSize, TimeControlDto timeControl, GameType gameType, RatingRange? range)
    {
        BoardSize = boardSize;
        TimeControl = timeControl;
        GameType = gameType;
        Range = range;
    }

    public override bool Equals(object? obj)
    {
        if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }

        var other = (Match)obj;
        return BoardSize == other.BoardSize && TimeControl.IncrementSeconds == other.TimeControl.IncrementSeconds && TimeControl.MainTimeSeconds == other.TimeControl.MainTimeSeconds && TimeControl.ByoYomiTime?.ByoYomis == other.TimeControl.ByoYomiTime?.ByoYomis && other.TimeControl.ByoYomiTime?.ByoYomiSeconds == other.TimeControl.ByoYomiTime?.ByoYomiSeconds && GameType == other.GameType && Range?.LowerBound == other.Range?.LowerBound && Range?.HigherBound == other.Range?.HigherBound;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(BoardSize, TimeControl.MainTimeSeconds, TimeControl.IncrementSeconds, TimeControl.ByoYomiTime?.ByoYomis, TimeControl.ByoYomiTime?.ByoYomiSeconds, GameType, Range?.HigherBound, Range?.HigherBound);
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

[Immutable, GenerateSerializer]
[Alias("RatingRange")]
public class RatingRange
{
    [Id(0)]
    public int LowerBound { get; set; }
    [Id(1)]
    public int HigherBound { get; set; }

    public RatingRange(int lowerBound, int higherBound)
    {
        Debug.Assert(lowerBound <= higherBound);
        Debug.Assert(higherBound - lowerBound <= 200);
        LowerBound = lowerBound;
        HigherBound = higherBound;
    }

    public RatingRange(PlayerRatingsData data)
    {
        LowerBound = (int)MathF.Round(((float)data.Glicko.Rating - 100) / 100) * 100;
        HigherBound = (int)MathF.Round(((float)data.Glicko.Rating + 100) / 100) * 100;
    }

    public override bool Equals(object? obj)
    {
        if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }

        var other = (RatingRange)obj;
        return LowerBound == other.LowerBound && HigherBound == other.HigherBound;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(LowerBound, HigherBound);
    }
}

public static class PlayerRatingDataMatchExt
{
    public static RatingRange GetRatingRange(this PlayerRatingsData playerRatingData)
    {
        return new RatingRange(playerRatingData);
    }
}