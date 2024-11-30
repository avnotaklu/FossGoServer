using BadukServer.Services;

public interface IPublicUserInfoService
{
    public Task<PublicUserInfo> GetPublicUserInfo(string userId);
}
public class PublicUserInfoService : IPublicUserInfoService
{
    private readonly IUsersService _usersService;
    private readonly IUserRatingService _userRatingService;

    public PublicUserInfoService(IUsersService usersService, IUserRatingService userRatingService)
    {
        _usersService = usersService;
        _userRatingService = userRatingService;
    }

    public async Task<PublicUserInfo> GetPublicUserInfo(string userId)
    {
        var user = await _usersService.GetByIds(new List<string> { userId });
        if (user.Count == 0) throw new UserNotFoundException(userId);
        var rating = await _userRatingService.GetUserRatings(userId);
        if (rating == null) throw new UserNotFoundException(userId);

        return new PublicUserInfo(id: user[0].Id!, email: user[0].Email, rating: rating);
    }

}