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
    private readonly IUserRatingService _userRatingService;
    private readonly IGameService _gameService;
    private readonly IGrainFactory _grainFactory;

    [ActivatorUtilitiesConstructor]
    public PlayerController(ILogger<PlayerController> logger, IUsersService usersService, IGrainFactory grainFactory, IGameService gameService,
        IUserRatingService userRatingService)
    {
        _logger = logger;
        _usersService = usersService;
        _grainFactory = grainFactory;
        _gameService = gameService;
        _userRatingService = userRatingService;
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



    private async Task<Game> SaveGame(string gameId)
    {
        var gameGrain = _grainFactory.GetGrain<IGameGrain>(gameId);
        var curGame = await gameGrain.GetGame();
        var res = await _gameService.SaveGame(curGame);
        if (res == null)
        {
            var oldGame = await _gameService.GetGame(gameId);
            await gameGrain.ResetGame(oldGame);
            throw new Exception("Failed to save game");
        }
        else
        {
            return res;
        }
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

        var creatorData = await _usersService.GetByIds([userId]);
        var creatorRating = await _userRatingService.GetUserRatings(userId);

        var creatorPublicData = new PublicUserInfo(id: creatorData[0].Id!, email: creatorData[0].Email, rating: creatorRating);

        var newGameMessage = new NewGameCreatedMessage(new AvailableGameData(game: game, creatorInfo: creatorPublicData));

        var notifierGrain = _grainFactory.GetGrain<IPushNotifierGrain>(await player.GetConnectionId());
        notifierGrain.SendMessageToAllUsers(new SignalRMessage(type: SignalRMessageType.newGame, data: newGameMessage));

        await SaveGame(gameId);
        return Ok(game);
    }

    private async Task<PublicUserInfo?> GetOtherPlayerData(Game game, string myId)
    {
        var otherPlayer = game.Players.Keys.FirstOrDefault(p => p != myId);
        if (otherPlayer != null)
        {
            var otherPlayerData = await _usersService.GetByIds([otherPlayer]);
            var otherRating = await _userRatingService.GetUserRatings(otherPlayer);
            return new PublicUserInfo(id: otherPlayerData[0].Id!, email: otherPlayerData[0].Email, rating: otherRating);
        }
        return null;
    }

    [HttpPost("JoinGame")]
    public async Task<ActionResult<GameJoinResult>> JoinGame([FromBody] GameJoinDto gameParams)
    {
        var userId = User.FindFirst("user_id")?.Value;
        if (userId == null) return Unauthorized();

        var gameId = gameParams.GameId;

        var gameGrain = _grainFactory.GetGrain<IGameGrain>(gameId);
        var oldGame = await gameGrain.GetGame();


        if (oldGame.GameCreator == userId)
        {
            return new GameJoinResult(
                game: oldGame,
                otherPlayerData: await GetOtherPlayerData(oldGame, userId),
                time: oldGame.StartTime ?? DateTime.Now.ToString("o")
            );
        }

        if (oldGame.Players.Keys.Contains(userId))
        {
            return new GameJoinResult(
                game: oldGame,
                otherPlayerData: await GetOtherPlayerData(oldGame, userId),
                time: oldGame.StartTime ?? DateTime.Now.ToString("o")
            );
        }

        var player = _grainFactory.GetGrain<IPlayerGrain>(userId);

        var time = DateTime.Now.ToString("o");

        var res = await player.JoinGame(gameId, time);

        var joinRes = new GameJoinResult(
            game: res.game,
            otherPlayerData: res.creatorData,
            time: time
        );

        await SaveGame(gameId);
        return Ok(joinRes);
    }

    [HttpGet("AvailableGames")]
    public async Task<ActionResult<AvailableGamesResult>> AvailableGames()
    {
        var userId = User.FindFirst("user_id")?.Value;
        if (userId == null) return Unauthorized();

        var playerPool = _grainFactory.GetGrain<IPlayerPoolGrain>(0);
        var players = await playerPool.GetActivePlayers();

        players.RemoveWhere(a => a == userId);

        var gamesIds = (await Task.WhenAll(players.Select(async p => await _grainFactory.GetGrain<IPlayerGrain>(p).GetCreatedGames()))).SelectMany(x => x) ?? [];

        var games = await Task.WhenAll(gamesIds.Select(i => _grainFactory.GetGrain<IGameGrain>(i).GetGame()));

        var availableGames = games.Where(a => !a.DidStart());

        var result = (await Task.WhenAll(
availableGames.Select(async g =>
        {
            var creatorData = await _usersService.GetByIds([g.GameCreator]);
            var creatorRating = await _userRatingService.GetUserRatings(g.GameCreator!);

            var createPublicData = new PublicUserInfo(id: creatorData[0].Id!, email: creatorData[0].Email, rating: creatorRating);
            return new AvailableGameData(game: g, creatorInfo: createPublicData);
        })
        )).ToList();

        return Ok(new AvailableGamesResult(games: [.. (result ?? [])]));
    }

    [HttpGet("MyGames")]
    public async Task<ActionResult<MyGamesResult>> MyGames()
    {
        var userId = User.FindFirst("user_id")?.Value;
        if (userId == null) return Unauthorized();

        var playerPool = _grainFactory.GetGrain<IPlayerPoolGrain>(0);
        var players = await playerPool.GetActivePlayers();

        var gamesIds = (await Task.WhenAll(players.Select(async p => await _grainFactory.GetGrain<IPlayerGrain>(p).GetCreatedGames()))).SelectMany(x => x) ?? [];

        var games = await Task.WhenAll(gamesIds.Select(i => _grainFactory.GetGrain<IGameGrain>(i).GetGame()));

        var myGames = games.Where(a => a.Players.ContainsKey(userId) || a.GameCreator == userId);

        var result = (await Task.WhenAll(
        myGames.Select(async g =>
        {
            PublicUserInfo? otherPlayerPublicData = null;
            if (g.DidStart())
            {
                otherPlayerPublicData = await GetOtherPlayerData(g, userId);
            }
            return new MyGameData(game: g, opposingPlayer: otherPlayerPublicData);
        })
        )).ToList();

        return Ok(new MyGamesResult(games: [.. (result ?? [])]));
    }

}