using BadukServer;
using Orleans;

public interface IMatchMakingGrain : IGrainWithIntegerKey
{
    public ValueTask FindMatch(string finderId, UserRating finderRating, List<MatchableBoardSize> boardSizes, List<TimeControlDto> timeStandards);
}