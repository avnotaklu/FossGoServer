using BadukServer;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
public class UserController : ControllerBase
{
    private IUserRatingService _ratingService;
    private IUserStatService _statService;
    private ILogger<UserController> _logger;
    public UserController(IUserRatingService userRepo, IUserStatService userStatService, ILogger<UserController> logger)
    {
        _ratingService = userRepo;
        _statService = userStatService;
        _logger = logger;
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

}