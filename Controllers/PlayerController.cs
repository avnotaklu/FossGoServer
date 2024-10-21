using BadukServer.Services;
using Microsoft.AspNetCore.Mvc;

namespace BadukServer.Controllers;

[ApiController]
[Route("[controller]")]
public class PlayerController : ControllerBase
{
    private readonly ILogger<AuthenticationController> _logger;
    private readonly UsersService _usersService;
    private readonly IGrainFactory _grainFactory;

    [ActivatorUtilitiesConstructor]
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
        var player = _grainFactory.GetGrain<IPlayerGrain>(userId);
        var playerPoolGrain = _grainFactory.GetGrain<IPlayerPoolGrain>(0);
        await playerPoolGrain.AddActivePlayer(userId);
        var playerIds = await playerPoolGrain.GetActivePlayers();
        var users = await _usersService.GetByIds(playerIds.ToList());
        return Ok(new RegisterPlayerResult(users));
    }
    
    [HttpPost("CreateGame")]
    public async Task<ActionResult<Game>> CreateGame([FromBody] GameCreationDto gameParams)
    {
        var userId = User.FindFirst("user_id")?.Value;
        if (userId == null) return Unauthorized();
        
        var player = _grainFactory.GetGrain<IPlayerGrain>(userId);
        var gameId = await player.CreateGame(gameParams.Rows, gameParams.Columns, gameParams.TimeInSeconds);
        var gameGrain = _grainFactory.GetGrain<IGameGrain>(gameId);

        return Ok(gameGrain.GetGame());
    }
    
    [HttpPost("JoinGame")]
    public async Task<ActionResult<Game>> JoinGame([FromBody] GameJoinDto gameParams)
    {
        var userId = User.FindFirst("user_id")?.Value;
        if (userId == null) return Unauthorized();
        
        var player = _grainFactory.GetGrain<IPlayerGrain>(userId);
        var gameId = await player.JoinGame(gameParams.GameId);
        var gameGrain = _grainFactory.GetGrain<IGameGrain>(gameId);

        return Ok(gameGrain.GetGame());
    }
}