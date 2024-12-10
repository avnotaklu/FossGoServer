using BadukServer;
using BadukServer.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
public class UserController : ControllerBase
{
    private IUsersService _usersService;
    private IUserRatingService _ratingService;
    private IUserStatService _statService;
    private ILogger<UserController> _logger;
    public UserController(IUserRatingService userRepo, IUserStatService userStatService, ILogger<UserController> logger, IUsersService usersService)
    {
        _ratingService = userRepo;
        _statService = userStatService;
        _logger = logger;
        _usersService = usersService;
    }


    [HttpGet("GetUserRatings")]
    public async Task<ActionResult<PlayerRatings>> GetUserRatings([FromQuery] string userId)
    {
        _logger.LogInformation("Getting user ratings for user {userId}", userId);
        var res = await _ratingService.GetUserRatings(userId);
        return Ok(res);
    }

    [HttpGet("GetUserStats")]
    public async Task<ActionResult<UserStat>> GetUserStats([FromQuery] string userId)
    {
        _logger.LogInformation("Getting user stats for user {userId}", userId);
        var res = await _statService.GetUserStat(userId);
        return Ok(res);
    }

    [HttpPost("UpdateUserProfile")]
    public async Task<ActionResult<User>> UpdateUserProfile([FromQuery] string userId, [FromBody] UpdateProfileDto userProfile)
    {
        _logger.LogInformation("Updating user profile for user {userId}", userId);
        var res = await _usersService.UpdateUserProfile(userId, userProfile);
        return Ok(new UpdateProfileResult(user: res));
    }

}