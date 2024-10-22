using BadukServer.Dto;
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
    public async Task<ActionResult<RegisterPlayerResult>> RegisterPlayer([FromBody] RegisterPlayerDto data)
    {
        var userId = User.FindFirst("user_id")?.Value;
        if (userId == null) return Unauthorized();
        // _grainFactory.GetGrain<IPlayerGrain>(userId);
        var player = _grainFactory.GetGrain<IPlayerGrain>(userId);
        await player.InitializePlayer(data.ConnectionId);
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
        if (gameParams.Rows == 0) return BadRequest("Rows can't be 0");
        if (gameParams.Columns == 0) return BadRequest("Columns can't be 0");
        if (gameParams.TimeInSeconds == 0) return BadRequest("TimeInSeconds can't be 0");
        
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