using BadukServer;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
public class UserController : ControllerBase
{
    private UserRatingService _ratingService;
    private Logger<UserController> _logger;
    public UserController(UserRatingService userRepo, Logger<UserController> logger)
    {
        _ratingService = userRepo;
        _logger = logger;
    }

    [HttpGet("GetUserRatings")]
    public async Task<ActionResult<UserRating>> GetUserRatings([FromQuery] string userId)
    {
        return await _ratingService.GetUserRatings(userId);
    }

}