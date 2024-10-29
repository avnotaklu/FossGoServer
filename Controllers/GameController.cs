
using Microsoft.AspNetCore.Authorization;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using BadukServer.Dto;
using BadukServer.Services;
using System.ComponentModel.DataAnnotations;
using BadukServer;

[ApiController]
[Authorize]
[Route("[controller]")]
public class GameController : ControllerBase
{
    private readonly ILogger<AuthenticationController> _logger;
    private readonly UsersService _usersService;
    private readonly IGrainFactory _grainFactory;

    [ActivatorUtilitiesConstructorAttribute]
    public GameController(ILogger<AuthenticationController> logger, UsersService usersService, IGrainFactory grainFactory)
    {
        _logger = logger;
        _usersService = usersService;
        _grainFactory = grainFactory;
    }

    [HttpPost("{gameId}/MakeMove")]
    public async Task<ActionResult<NewMoveResult>> MakeMove([FromBody] MovePosition move, string GameId)
    {
        var userId = User.FindFirst("user_id")?.Value;
        if (userId == null) return Unauthorized();

        var gameGrain = _grainFactory.GetGrain<IGameGrain>(GameId);
        var res = await gameGrain.MakeMove(move, userId);

        return Ok(new NewMoveResult(
            game: res.game,
            result: res.moveSuccess
        ));
    }
}