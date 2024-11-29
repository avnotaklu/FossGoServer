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
    private readonly ILogger<AuthenticationController> _logger;
    private readonly IGrainFactory _grainFactory;
    private readonly IUserRatingService _userRatingService;

    [ActivatorUtilitiesConstructor]
    public MainHub(ILogger<AuthenticationController> logger, IGrainFactory grainFactory, IUserRatingService userRatingService) {
        _logger = logger;
        _grainFactory = grainFactory;
        _userRatingService = userRatingService;
    }
    public override Task OnConnectedAsync()
    {
        _logger.LogInformation("User connected : {id}", Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public async Task FindMatch(FindMatchDto findMatchDto)
    {
        _logger.LogInformation("User {user} is looking for a match", Context.ConnectionId);

        var matchGrain = _grainFactory.GetGrain<IMatchMakingGrain>(0);
        var pushGrain = _grainFactory.GetGrain<IPushNotifierGrain>(Context.ConnectionId);
        var playerId = await pushGrain.GetPlayerId();

        var playerRating = await _userRatingService.GetUserRatings(playerId);

        await matchGrain.FindMatch(
            playerId,
            playerRating,
            findMatchDto.BoardSizes,
            findMatchDto.TimeStandards
        );
    }
    
}
