
using Microsoft.AspNetCore.Authorization;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using BadukServer.Dto;
using BadukServer.Services;
using System.ComponentModel.DataAnnotations;
using BadukServer;
using BadukServer.Orleans.Grains;
using Microsoft.CodeAnalysis.Differencing;

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

    [HttpPost("{gameId}/ContinueGame")]
    public async Task<ActionResult<Game>> ContinueGame(string GameId)
    {
        var userId = User.FindFirst("user_id")?.Value;
        if (userId == null) return Unauthorized();

        var gameGrain = _grainFactory.GetGrain<IGameGrain>(GameId);
        var game = await gameGrain.ContinueGame(userId);

        // var res = await gameGrain.MakeMove(move, userId);
        return Ok(game);
    }

    [HttpPost("{gameId}/AcceptScores")]
    public async Task<ActionResult<Game>> AcceptScores(string GameId)
    {
        var userId = User.FindFirst("user_id")?.Value;
        if (userId == null) return Unauthorized();

        var gameGrain = _grainFactory.GetGrain<IGameGrain>(GameId);
        var game = await gameGrain.AcceptScores(userId);

        // var res = await gameGrain.MakeMove(move, userId);
        return Ok(game);
    }

    [HttpPost("{gameId}/ResignGame")]
    public async Task<ActionResult<Game>> ResignGame(string GameId)
    {
        var userId = User.FindFirst("user_id")?.Value;
        if (userId == null) return Unauthorized();

        var gameGrain = _grainFactory.GetGrain<IGameGrain>(GameId);
        var game = await gameGrain.ResignGame(userId);

        return Ok(game);
    }

    [HttpPost("{gameId}/EditDeadStoneCluster")]
    public async Task<ActionResult<Game>> EditDeadStoneCluster(string GameId, [FromBody] EditDeadStoneClusterDto data)
    {
        var userId = User.FindFirst("user_id")?.Value;
        if (userId == null) return Unauthorized();

        var position = new Position(data.Position.X, data.Position.Y);        

        var gameGrain = _grainFactory.GetGrain<IGameGrain>(GameId);
        var game = await gameGrain.EditDeadStone(position, data.State);

        var otherPlayerId = game.GetOtherPlayerIdFromPlayerId(userId);

        var pushNotifier = _grainFactory.GetGrain<IPushNotifierGrain>(otherPlayerId);

        await pushNotifier.SendMessage(new SignalRMessage(
            type: SignalRMessageType.editDeadStone,
            data: new EditDeadStoneMessage(
                position: data.Position,
                state: data.State,
                game: game
            )
        ), GameId, toMe: true);

        // var res = await gameGrain.MakeMove(move, userId);
        return Ok(game);
    }
}