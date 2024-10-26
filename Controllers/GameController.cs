
using Microsoft.AspNetCore.Authorization;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using BadukServer.Dto;
using BadukServer.Services;
using System.ComponentModel.DataAnnotations;
using BadukServer;

[ApiController]
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

    // [HttpPost("MakeMove")]
    // async Task<ActionResult<Game>> MakeMove([FromBody] GameMove move, [FromQuery] string GameId) {
    //     var gameGrain = _grainFactory.GetGrain<IGameGrain>(GameId);
    //     await gameGrain.MakeMove(move);
    // }
}