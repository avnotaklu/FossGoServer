using BadukServer;
using Orleans;

public interface IMatchMakingGrain : IGrainWithIntegerKey
{
    public ValueTask FindMatch(PlayerInfo userInfo,  List<MatchableBoardSize> boardSizes, List<TimeControlDto> timeStandards);

    public Task CancelFind(string player);
}