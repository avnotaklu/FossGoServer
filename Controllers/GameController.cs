
using Microsoft.AspNetCore.Authorization;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using BadukServer.Dto;
using BadukServer.Services;
using System.ComponentModel.DataAnnotations;
using BadukServer;
using BadukServer.Orleans.Grains;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.AspNetCore.Http.HttpResults;

[ApiController]
[Authorize]
[Route("[controller]")]
public class GameController : ControllerBase
{
    private readonly ILogger<AuthenticationController> _logger;
    private readonly IUsersService _usersService;
    private readonly IGameService _gameService;
    private readonly IPlayerInfoService _playerInfoService;
    private readonly IGrainFactory _grainFactory;


    [ActivatorUtilitiesConstructorAttribute]
    public GameController(ILogger<AuthenticationController> logger, IUsersService usersService, IGrainFactory grainFactory, IGameService gameService, IPlayerInfoService playerInfoService)
    {
        _logger = logger;
        _usersService = usersService;
        _grainFactory = grainFactory;
        _gameService = gameService;
        _playerInfoService = playerInfoService;
    }

    [HttpGet("{gameId}/GameAndOpponent")]
    public async Task<ActionResult<GameAndOpponent>> GetGameAndOpponent(string GameId)
    {
        var userId = User.FindFirst("user_id")?.Value;
        if (userId == null) return Unauthorized("Session invalid");

        var userType = User.FindFirst("user_type")?.Value;
        if (userType == null) return Unauthorized("Session invalid");

        var playerType = PlayerTypeExt.FromString(userType);
        if (playerType != PlayerType.Normal) return Unauthorized("User Account is not valid");

        var game = await _gameService.GetGame(GameId);

        if (game == null)
        {
            return BadRequest("Game not found");
        }

        var playerInfo = await _playerInfoService.GetPublicUserInfoForNormalUser(game.Players.GetOtherPlayerIdFromPlayerId(userId)!);

        if (playerInfo == null)
        {
            return BadRequest("Opponent not found");
        }

        return Ok(new GameAndOpponent(playerInfo, game));
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

        try
        {
            var game = await gameGrain.ContinueGame(userId);
            return Ok(game);
        }
        catch (InvalidOperationException e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpPost("{gameId}/AcceptScores")]
    public async Task<ActionResult<Game>> AcceptScores(string GameId)
    {
        var userId = User.FindFirst("user_id")?.Value;
        if (userId == null) return Unauthorized();

        var gameGrain = _grainFactory.GetGrain<IGameGrain>(GameId);


        try
        {
            var game = await gameGrain.AcceptScores(userId);
            return Ok(game);
        }
        catch (InvalidOperationException e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpPost("{gameId}/ResignGame")]
    public async Task<ActionResult<Game>> ResignGame(string GameId)
    {
        var userId = User.FindFirst("user_id")?.Value;
        if (userId == null) return Unauthorized();

        var gameGrain = _grainFactory.GetGrain<IGameGrain>(GameId);

        try
        {
            var game = await gameGrain.ResignGame(userId);
            return Ok(game);
        }
        catch (InvalidOperationException e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpPost("{gameId}/EditDeadStoneCluster")]
    public async Task<ActionResult<Game>> EditDeadStoneCluster(string GameId, [FromBody] EditDeadStoneClusterDto data)
    {
        var userId = User.FindFirst("user_id")?.Value;
        if (userId == null) return Unauthorized();

        var gameGrain = _grainFactory.GetGrain<IGameGrain>(GameId);

        try
        {
            var game = await gameGrain.EditDeadStone(data.Position, data.State, userId);
            return Ok(game);
        }
        catch (InvalidOperationException e)
        {
            return BadRequest(e.Message);
        }
    }
}