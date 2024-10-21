
using Microsoft.AspNetCore.Authorization;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using BadukServer.Dto;
using BadukServer.Services;
using System.ComponentModel.DataAnnotations;

[ApiController]
[Route("[controller]")]
public class PlayerController : ControllerBase
{
    private readonly ILogger<AuthenticationController> _logger;
    private readonly UsersService _usersService;
    private readonly IGrainFactory _grainFactory;

    [ActivatorUtilitiesConstructorAttribute]
    public PlayerController(ILogger<AuthenticationController> logger, UsersService usersService, IGrainFactory grainFactory)
    {
        _logger = logger;
        _usersService = usersService;
        _grainFactory = grainFactory;
    }


    [HttpPost("RegisterPlayer")]
    public async Task<ActionResult<RegisterPlayerResult>> RegisterPlayer()
    {
        var userId = User.FindFirst("user_id")?.Value;
        if (userId == null) return Unauthorized();
        // _grainFactory.GetGrain<IPlayerGrain>(userId);
        var playerPoolGrain = _grainFactory.GetGrain<IPlayerPoolGrain>(0);
        await playerPoolGrain.AddActivePlayer(userId);
        var playerIds = await playerPoolGrain.GetActivePlayers();
        var users = await _usersService.GetByIds(playerIds.ToList());
        return Ok(new RegisterPlayerResult(users));
    }
}