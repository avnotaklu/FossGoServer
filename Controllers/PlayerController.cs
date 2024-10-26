using BadukServer.Dto;
using BadukServer.Orleans.Grains;
using BadukServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BadukServer.Controllers;

[ApiController]
[Authorize]
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
        var gameId = await player.CreateGame(gameParams.Rows, gameParams.Columns, gameParams.TimeInSeconds, gameParams.FirstPlayerStone);

        var gameGrain = _grainFactory.GetGrain<IGameGrain>(gameId);

        return Ok(await gameGrain.GetGame());
    }

    [HttpPost("JoinGame")]
    public async Task<ActionResult<GameJoinResult>> JoinGame([FromBody] GameJoinDto gameParams)
    {
        var userId = User.FindFirst("user_id")?.Value;
        if (userId == null) return Unauthorized();

        var gameId = gameParams.GameId;
        var player = _grainFactory.GetGrain<IPlayerGrain>(userId);

        await player.JoinGame(gameId);

        var gameGrain = _grainFactory.GetGrain<IGameGrain>(gameId);
        var game = await gameGrain.GetGame();
        var players = await _usersService.GetByIds([.. game.Players.Keys]);
        var playerInfos = players.Select(p => new PublicUserInfo(id: p.Id!, email: p.Email)).ToList();
        var time = DateTime.Now;

        var joinRes = new GameJoinResult(
            game: game,
            players: playerInfos,
            time: time.ToString("o")
        );

        var notifierGrain = _grainFactory.GetGrain<IPushNotifierGrain>(game.Players.First().Key);
        await notifierGrain.SendMessage(new SignalRMessage(type: "GameJoin", data: joinRes), gameId);

        return Ok(joinRes);
    }
}