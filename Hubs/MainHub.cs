using System.Configuration;
using System.Diagnostics;
using BadukServer.Orleans.Grains;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

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
        var userType = Context.User?.FindFirst("user_type")?.Value ?? throw new Exception("User not found");

        var playerId = Context.User?.FindFirst("user_id")?.Value ?? throw new Exception("User not found");

        _logger.LogInformation("User connected : {id} of type {type}", Context.ConnectionId, userType);

        await Groups.AddToGroupAsync(Context.ConnectionId, "Users");

        var playerGrain = _grainFactory.GetGrain<IPlayerGrain>(playerId);

        var playerType = PlayerTypeExt.FromString(userType);

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

            var playerId = Context.User?.FindFirst("user_id")?.Value ?? throw new Exception("User not found");
            var userType = Context.User?.FindFirst("user_type")?.Value ?? throw new Exception("User not found");

            var playerType = PlayerTypeExt.FromString(userType);

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
            var playerId = Context.User?.FindFirst("user_id")?.Value ?? throw new Exception("User not found");
            var userType = Context.User?.FindFirst("user_type")?.Value ?? throw new Exception("User not found");

            var playerType = PlayerTypeExt.FromString(userType);
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
}
