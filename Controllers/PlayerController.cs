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
    private readonly ILogger<PlayerController> _logger;
    private readonly IUsersService _usersService;
    private readonly IPlayerInfoService _playerInfoService;
    private readonly IUserRatingService _userRatingService;
    private readonly IGameService _gameService;
    private readonly IGrainFactory _grainFactory;

    [ActivatorUtilitiesConstructor]
    public PlayerController(ILogger<PlayerController> logger, IUsersService usersService, IGrainFactory grainFactory, IGameService gameService, IUserRatingService userRatingService, IPlayerInfoService playerInfoService)
    {
        _logger = logger;
        _usersService = usersService;
        _grainFactory = grainFactory;
        _gameService = gameService;
        _userRatingService = userRatingService;
        _playerInfoService = playerInfoService;
    }



    [HttpPost("RegisterPlayer")]
    public async Task<ActionResult<RegisterPlayerResult>> RegisterPlayer([FromBody] RegisterPlayerDto data)
    {
        var userId = User.FindFirst("user_id")?.Value;
        if (userId == null) return Unauthorized();

        var userType = User.FindFirst("user_type")?.Value;
        if (userType == null) return Unauthorized();

        var playerType = PlayerTypeExt.FromString(userType);

        var playerGrain = _grainFactory.GetGrain<IPlayerGrain>(userId);

        // var publicData = await _playerInfoService.GetPublicUserInfoForPlayer(userId, playerType);

        await playerGrain.InitializePlayer(data.ConnectionId, playerType);

        var playerPoolGrain = _grainFactory.GetGrain<IPlayerPoolGrain>(0);
        await playerPoolGrain.AddActivePlayer(userId);

        // return Ok(new RegisterPlayerResult(publicData));
        return Ok(new RegisterPlayerResult());
    }

    [HttpPost("CreateGame")]
    public async Task<ActionResult<Game>> CreateGame([FromBody] GameCreationDto gameParams)
    {
        var userId = User.FindFirst("user_id")?.Value;
        if (userId == null) return Unauthorized();

        var userType = User.FindFirst("user_type")?.Value;
        if (userType == null) return Unauthorized();

        var playerType = PlayerTypeExt.FromString(userType);

        if (gameParams.Rows == 0) return BadRequest("Rows can't be 0");
        if (gameParams.Columns == 0) return BadRequest("Columns can't be 0");
        if (gameParams.TimeControl.MainTimeSeconds == 0) return BadRequest("main time can't be 0");
        var time = DateTime.Now.ToString("o");


        var player = _grainFactory.GetGrain<IPlayerGrain>(userId);

        var gameId = await player.CreateGame(gameParams, time);

        var gameGrain = _grainFactory.GetGrain<IGameGrain>(gameId);

        var game = await gameGrain.GetGame();

        var creatorPublicData = await _playerInfoService.GetPublicUserInfoForPlayer(userId, playerType) ?? throw new UserNotFoundException(userId);

        var newGameMessage = new NewGameCreatedMessage(new AvailableGameData(game: game, creatorInfo: creatorPublicData));

        var notifierGrain = _grainFactory.GetGrain<IPushNotifierGrain>(await player.GetConnectionId());
        await notifierGrain.SendMessageToSameType(new SignalRMessage(type: SignalRMessageType.newGame, data: newGameMessage));

        return Ok(game);
    }

    private async Task<PlayerInfo?> GetOtherPlayerData(Game game, string myId, PlayerType myType)
    {
        var otherPlayer = game.Players.Keys.FirstOrDefault(p => p != myId);
        if (otherPlayer != null)
        {
            return await _playerInfoService.GetPublicUserInfoForPlayer(otherPlayer, myType);
        }
        return null;
    }

    [HttpPost("JoinGame")]
    public async Task<ActionResult<GameJoinResult>> JoinGame([FromBody] GameJoinDto gameParams)
    {
        var userId = User.FindFirst("user_id")?.Value;
        if (userId == null) return Unauthorized();

        var userType = User.FindFirst("user_type")?.Value;
        if (userType == null) return Unauthorized();

        var gameId = gameParams.GameId;

        var player = _grainFactory.GetGrain<IPlayerGrain>(userId);

        var time = DateTime.Now.ToString("o");

        var res = await player.JoinGame(gameId, time);

        var joinRes = new GameJoinResult(
            game: res.game,
            otherPlayerData: res.otherPlayerData,
            time: time
        );

        return Ok(joinRes);
    }

    [HttpGet("AvailableGames")]
    public async Task<ActionResult<AvailableGamesResult>> AvailableGames()
    {
        var userId = User.FindFirst("user_id")?.Value;
        if (userId == null) return Unauthorized();

        var userType = User.FindFirst("user_type")?.Value;
        if (userType == null) return Unauthorized();

        var myType = PlayerTypeExt.FromString(userType);

        var playerPool = _grainFactory.GetGrain<IPlayerPoolGrain>(0);
        var players = await playerPool.GetActivePlayers();

        players.RemoveWhere(a => a == userId);

        var gamesIds = (await Task.WhenAll(players.Select(async p => await _grainFactory.GetGrain<IPlayerGrain>(p).GetCreatedGames()))).SelectMany(x => x) ?? [];

        var games = await Task.WhenAll(gamesIds.Select(i => _grainFactory.GetGrain<IGameGrain>(i).GetGame()));

        var availableGames = games.Where(a => !a.DidStart());
        var allowedGames = availableGames.Where(a => a.GameType.IsAllowedPlayerType(myType));

        var result = (await Task.WhenAll(
allowedGames.Select(async g =>
        {
            // REVIEW: Getting game creator info using myType, i'm assuming that the creator is the same type as me
            var creatorData = await _playerInfoService.GetPublicUserInfoForPlayer(g.GameCreator!, myType);
            if (creatorData == null) return null;
            return new AvailableGameData(game: g, creatorInfo: creatorData);
        })
        )).Where(a => a != null).Select(a => a!).ToList();

        return Ok(new AvailableGamesResult(games: [.. (result ?? [])]));
    }

    [HttpGet("MyGames")]
    public async Task<ActionResult<MyGamesResult>> MyGames()
    {
        var userId = User.FindFirst("user_id")?.Value;
        if (userId == null) return Unauthorized();

        var userType = User.FindFirst("user_type")?.Value;
        if (userType == null) return Unauthorized();

        var myType = PlayerTypeExt.FromString(userType);

        var playerPool = _grainFactory.GetGrain<IPlayerPoolGrain>(0);
        var players = await playerPool.GetActivePlayers();

        var gamesIds = (await Task.WhenAll(players.Select(async p => await _grainFactory.GetGrain<IPlayerGrain>(p).GetCreatedGames()))).SelectMany(x => x) ?? [];

        var games = await Task.WhenAll(gamesIds.Select(i => _grainFactory.GetGrain<IGameGrain>(i).GetGame()));

        var myGames = games.Where(a => a.Players.ContainsKey(userId) || a.GameCreator == userId);

        var result = (await Task.WhenAll(
        myGames.Select(async g =>
        {
            PlayerInfo? otherPlayerPublicData = null;
            if (g.DidStart())
            {
                otherPlayerPublicData = await GetOtherPlayerData(g, userId, myType);
            }
            return new MyGameData(game: g, opposingPlayer: otherPlayerPublicData);
        })
        )).ToList();

        return Ok(new MyGamesResult(games: [.. (result ?? [])]));
    }

}