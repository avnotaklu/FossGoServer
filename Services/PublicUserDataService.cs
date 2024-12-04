using BadukServer.Orleans.Grains;
using BadukServer.Services;

public interface IPlayerInfoService
{
    public Task<PlayerInfo> GetPublicUserInfoForPlayer(string userId, PlayerType playerType);
    public Task<PlayerInfo> GetPublicUserInfoForNormalUser(string userId);
    public Task<PlayerInfo> GetPublicUserInfoForGuest(string userId);
}
public class PublicUserInfoService : IPlayerInfoService
{
    private readonly IUsersService _usersService;
    private readonly IUserRatingService _userRatingService;

    public PublicUserInfoService(IUsersService usersService, IUserRatingService userRatingService)
    {
        _usersService = usersService;
        _userRatingService = userRatingService;
    }

    public async Task<PlayerInfo> GetPublicUserInfoForNormalUser(string userId)
    {
        var user = await _usersService.GetByIds(new List<string> { userId });
        if (user.Count == 0) throw new UserNotFoundException(userId);
        var rating = await _userRatingService.GetUserRatings(userId);
        if (rating == null) throw new UserNotFoundException(userId);

        return new PlayerInfo(id: user[0].Id!, email: user[0].Email, rating: rating, PlayerType.Normal);
    }

    public Task<PlayerInfo> GetPublicUserInfoForGuest(string userId)
    {
        return Task.FromResult(new PlayerInfo(id: userId, email: null, rating: null, PlayerType.Guest));
    }


    public async Task<PlayerInfo> GetPublicUserInfoForPlayer(string userId, PlayerType playerType)
    {
        return playerType switch
        {
            PlayerType.Normal => await GetPublicUserInfoForNormalUser(userId),
            PlayerType.Guest => await GetPublicUserInfoForGuest(userId),
            _ => throw new Exception("Invalid player type")
        };
    }


}