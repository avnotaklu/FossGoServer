using BadukServer.Dto;
using BadukServer.Orleans.Grains;
using BadukServer.Services;
using Google.Apis.Util;
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
        var playerGrain = _grainFactory.GetGrain<IPlayerGrain>(userId);

        // if (playerGrain.IsInitializedByOtherDevice(data.ConnectionId).Result) return BadRequest("Player session already active elsewhere");

        await playerGrain.InitializePlayer(data.ConnectionId);
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
        if (gameParams.TimeControl.MainTimeSeconds == 0) return BadRequest("main time can't be 0");
        var time = DateTime.Now.ToString("o");


        var player = _grainFactory.GetGrain<IPlayerGrain>(userId);

        var gameId = await player.CreateGame(gameParams.Rows, gameParams.Columns, gameParams.TimeControl, gameParams.FirstPlayerStone, time);

        var gameGrain = _grainFactory.GetGrain<IGameGrain>(gameId);

        var game = await gameGrain.GetGame();


        var newGameMessage = new NewGameCreatedMessage(game);

        var notifierGrain = _grainFactory.GetGrain<IPushNotifierGrain>(userId);
        await notifierGrain.SendMessage(new SignalRMessage(type: SignalRMessageType.newGame, data: newGameMessage), gameId, toMe: false);

        return Ok(game);
    }

    async Task<List<PublicUserInfo>> getPlayerInfos(Game game)
    {
        var players = await _usersService.GetByIds([.. game.Players.Keys]);
        var playerInfos = players.Select(p => new PublicUserInfo(id: p.Id!, email: p.Email)).ToList();
        return playerInfos;
    }

    [HttpPost("JoinGame")]
    public async Task<ActionResult<GameJoinResult>> JoinGame([FromBody] GameJoinDto gameParams)
    {
        var userId = User.FindFirst("user_id")?.Value;
        if (userId == null) return Unauthorized();

        var gameId = gameParams.GameId;

        var gameGrain = _grainFactory.GetGrain<IGameGrain>(gameId);
        var oldGame = await gameGrain.GetGame();

        if (oldGame.Players.Keys.Contains(userId))
        {
            return new GameJoinResult(
            game: oldGame,
            players: await getPlayerInfos(oldGame),
            time: oldGame.StartTime ?? DateTime.Now.ToString("o")
        );
        }

        var player = _grainFactory.GetGrain<IPlayerGrain>(userId);

        var time = DateTime.Now.ToString("o");

        await player.JoinGame(gameId, time);
        var newGame = await gameGrain.GetGame();

        var joinRes = new GameJoinResult(
            game: newGame,
            players: await getPlayerInfos(newGame),
            time: time
        );

        foreach (var id in newGame.Players.Keys)
        {
            var notifierGrain = _grainFactory.GetGrain<IPushNotifierGrain>(id);
            await notifierGrain.SendMessage(new SignalRMessage(type: SignalRMessageType.gameJoin, data: joinRes), gameId, toMe: true);
        }

        return Ok(joinRes);
    }

    [HttpGet("AvailableGames")]
    public async Task<ActionResult<AvailableGamesResult>> AvailableGames()
    {
        var playerPool = _grainFactory.GetGrain<IPlayerPoolGrain>(0);
        var players = await playerPool.GetActivePlayers();
        var gamesIds = (await Task.WhenAll(players.Select(async p => await _grainFactory.GetGrain<IPlayerGrain>(p).GetAvailableGames()))).SelectMany(x => x) ?? [];

        var games = await Task.WhenAll(gamesIds.Select(i => _grainFactory.GetGrain<IGameGrain>(i).GetGame()));

        return Ok(new AvailableGamesResult(games: [.. (games ?? [])]));
    }
}