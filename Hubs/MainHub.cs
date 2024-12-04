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

    public override Task OnConnectedAsync()
    {
        var userType = Context.User?.FindFirst("user_type")?.Value ?? throw new Exception("User not found");

        _logger.LogInformation("User connected : {id} of type {type}", Context.ConnectionId, userType);
        Groups.AddToGroupAsync(Context.ConnectionId, "Users");
        return base.OnConnectedAsync();
    }

    // [Authorize(Policy = "PlayerOnly")]
    public async Task FindMatch(FindMatchDto findMatchDto)
    {
        try
        {
            _logger.LogInformation("Player {user} is looking for a match", Context.ConnectionId);

            Debug.Assert(findMatchDto.BoardSizes.Count > 0);
            Debug.Assert(findMatchDto.TimeStandards.Count > 0);

            var matchGrain = _grainFactory.GetGrain<IMatchMakingGrain>(0);
            // var pushGrain = _grainFactory.GetGrain<IPushNotifierGrain>(Context.ConnectionId);

            var playerId = Context.User?.FindFirst("user_id")?.Value ?? throw new Exception("User not found");
            var userType = Context.User?.FindFirst("user_type")?.Value ?? throw new Exception("User not found");

            var playerType = PlayerTypeExt.FromString(userType);

            var playerData = await _userInfoService.GetPublicUserInfoForPlayer(playerId, playerType);

            await matchGrain.FindMatch(
                playerData,
                findMatchDto.BoardSizes,
                findMatchDto.TimeStandards
            );
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error finding match");
        }
    }

}
