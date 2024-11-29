using BadukServer;
using Orleans;

public interface IMatchMakingGrain : IGrainWithIntegerKey
{
    public Task FindMatch(string finderId, UserRating finderRating, List<BoardSize> boardSizes, List<TimeStandard> timeStandards);
}