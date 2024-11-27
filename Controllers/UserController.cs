using BadukServer;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
public class UserController : ControllerBase
{
    private IUserRatingService _ratingService;
    private ILogger<UserController> _logger;
    public UserController(IUserRatingService userRepo, ILogger<UserController> logger)
    {
        _ratingService = userRepo;
        _logger = logger;
    }

    [HttpGet("GetUserRatings")]
    public async Task<ActionResult<UserRating>> GetUserRatings([FromQuery] string userId)
    {
        _logger.LogInformation("Getting user ratings for user {userId}", userId);
        var res = await _ratingService.GetUserRatings(userId);
        return Ok(res);
    }

}