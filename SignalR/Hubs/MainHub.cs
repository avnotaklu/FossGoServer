using System.Configuration;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using BadukServer.Orleans.Grains;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Driver.Linq;

namespace BadukServer.Hubs;

/// <summary>
/// The hub which Web clients connect to receive game updates.
/// </summary>
public sealed class MainHub : Hub
{
    private readonly ILogger<MainHub> _logger;
    private readonly IGrainFactory _grainFactory;
    private readonly IUserRatingService _userRatingService;
    private readonly IPlayerInfoService _userInfoService;

    [ActivatorUtilitiesConstructor]
    public MainHub(ILogger<MainHub> logger, IGrainFactory grainFactory, IUserRatingService userRatingService, IPlayerInfoService userInfoService)
    {
        _logger = logger;
        _grainFactory = grainFactory;
        _userRatingService = userRatingService;
        _userInfoService = userInfoService;
    }


    public async override Task OnConnectedAsync()
    {
        // var userType = Context.User?.FindFirst("user_type")?.Value ?? throw new Exception("User not found");

        // var playerId = Context.User?.FindFirst("user_id")?.Value ?? throw new Exception("User not found");

        var (playerId, playerType) = GetPlayerIdAndType(Context);

        _logger.LogInformation("User connected : {id} of type {type}", Context.ConnectionId, playerType.ToString());

        await Groups.AddToGroupAsync(Context.ConnectionId, "Users");

        var playerGrain = _grainFactory.GetGrain<IPlayerGrain>(playerId);

        await playerGrain.ConnectPlayer(Context.ConnectionId, playerType);

        var playerPoolGrain = _grainFactory.GetGrain<IPlayerPoolGrain>(0);
        await playerPoolGrain.AddActivePlayer(playerId);

        await base.OnConnectedAsync();
    }

    public ValueTask FindMatch(FindMatchDto findMatchDto)
    {
        try
        {
            _logger.LogInformation("User {user} is looking for a match", Context.ConnectionId);

            Debug.Assert(findMatchDto.BoardSizes.Count > 0);
            Debug.Assert(findMatchDto.TimeStandards.Count > 0);

            var matchGrain = _grainFactory.GetGrain<IMatchMakingGrain>(0);
            // var pushGrain = _grainFactory.GetGrain<IPushNotifierGrain>(Context.ConnectionId);

            // var playerId = Context.User?.FindFirst("user_id")?.Value ?? throw new Exception("User not found");
            // var userType = Context.User?.FindFirst("user_type")?.Value ?? throw new Exception("User not found");

            var (playerId, playerType) = GetPlayerIdAndType(Context);


            return new(Task.Run(
            async () =>
            {
                var playerData = await _userInfoService.GetPublicUserInfoForPlayer(playerId, playerType) ?? throw new UserNotFoundException(playerId);

                await matchGrain.FindMatch(
                    playerData,
                    findMatchDto.BoardSizes,
                    findMatchDto.TimeStandards
                );
            }
            ));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error finding match");
            return new();
        }
    }

    public async Task CancelFind()
    {
        try
        {
            // var playerId = Context.User?.FindFirst("user_id")?.Value ?? throw new Exception("User not found");
            // var userType = Context.User?.FindFirst("user_type")?.Value ?? throw new Exception("User not found");

            var (playerId, playerType) = GetPlayerIdAndType(Context);

            var matchGrain = _grainFactory.GetGrain<IMatchMakingGrain>(0);
            await matchGrain.CancelFind(playerId);
        }
        catch (System.Exception e)
        {
            _logger.LogError(e, "Error cancelling match");
            return;
        }
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("user disconnected {user}", Context.ConnectionId);
        _logger.LogDebug("user disconnected {exception}", exception);
        var pushG = _grainFactory.GetGrain<IPushNotifierGrain>(Context.ConnectionId);

        pushG.SetConnectionStrength(new ConnectionStrength(10_000));

        return base.OnDisconnectedAsync(exception);
    }

    public async Task Ping(int ping)
    {
        try
        {
            // var playerId = Context.User?.FindFirst("user_id")?.Value ?? throw new Exception("User not found");
            // var userType = Context.User?.FindFirst("user_type")?.Value ?? throw new Exception("User not found");

            var (playerId, playerType) = GetPlayerIdAndType(Context);

            var pushG = _grainFactory.GetGrain<IPushNotifierGrain>(Context.ConnectionId);
            await pushG.SetConnectionStrength(new ConnectionStrength(ping));
            await pushG.SendMessageToMe(
                new SignalRMessage(
                    SignalRMessageType.pong,
                    null
                )
            );
        }
        catch (System.Exception e)
        {
            _logger.LogError(e, "Error cancelling match");
            return;
        }
    }

    private (String UserId, PlayerType PlayerType) GetPlayerIdAndType(HubCallerContext context)
    {

        if ((!context.User?.Identity?.IsAuthenticated) ?? false)
        {
            var http = context.GetHttpContext();
            StringValues token = StringValues.Empty;
            if (http != null)
            {
                http.Request.Query.TryGetValue("token", out token);

                if (!token.IsNullOrEmpty())
                {
                    var rToken = token.First();
                    try
                    {
                        var sec = new JwtSecurityTokenHandler();
                        var res = sec.ReadJwtToken(rToken);
                        var _playerId = res.Claims.First(a => a.Type == "user_id")?.Value ?? throw new Exception("User not found");
                        var _userType = res.Claims.First(a => a.Type == "user_type")?.Value ?? throw new Exception("User not found");

                        return (_playerId, PlayerTypeExt.FromString(_userType));
                    }
                    catch
                    {
                        throw new Exception("User not found");
                    }
                }
            }

            throw new Exception("User not found");
        }

        var playerId = context.User?.FindFirst("user_id")?.Value ?? throw new Exception("User not found");
        var userType = context.User?.FindFirst("user_type")?.Value ?? throw new Exception("User not found");

        return (playerId, PlayerTypeExt.FromString(userType));
    }
}
