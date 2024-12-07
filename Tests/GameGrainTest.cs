using System.Security.Cryptography;
using BadukServer;
using BadukServer.Orleans.Grains;
using BadukServer.Services;
using Castle.DynamicProxy;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Orleans.Runtime.Hosting;
using Orleans.Serialization;
using Orleans.TestingHost;

namespace Tests;

[TestClass]
public class GameGrainTests
{
    static public Mock<IDateTimeService> dateTimeMock = new Mock<IDateTimeService>();
    static public Mock<IUsersService> userServicesMock = new Mock<IUsersService>();
    static public Mock<IUserRatingService> userRatingsServicesMock = new Mock<IUserRatingService>();
    static public Mock<IGameService> gameService = new Mock<IGameService>();
    static public Mock<IPlayerInfoService> publicUserInfoGrain = new Mock<IPlayerInfoService>();
    static public Mock<ISignalRHubService> hubContextMock = new();
    static public Mock<IRatingEngine> ratingEngineMock = new();

    // static List<User> users = new List<User>
    // {
    //     new User("p1@p1.com", false, "ph") {Id = "p1" },
    //     new User("p2@p2.com", false, "ph") {Id = "p2" },
    // };

    static List<PlayerRatings> userRatings = [
        new PlayerRatings("p1", UserRatingService.GetInitialRatings()),
        new PlayerRatings("p2", UserRatingService.GetInitialRatings())
    ];



    [TestMethod]
    public async Task GameGrainTest()
    {
        // var builder = new TestClusterBuilder();
        // var cluster = builder
        //     .AddSiloBuilderConfigurator<TestSiloConfigurations>()
        //     .Build();
        // cluster.Deploy();


        // var curTime = _1980Jan1_1_30PM;

        // var p1Id = "p1";
        // var p2Id = "p2";
        // var p1ConId = "p1Con";
        // var p2ConId = "p2Con";

        // var p1 = cluster.GrainFactory.GetGrain<IPlayerGrain>(p1Id);
        // await p1.InitializePlayer(p1ConId);
        // var p2 = cluster.GrainFactory.GetGrain<IPlayerGrain>(p2Id);
        // await p2.InitializePlayer(p2ConId);

        // DateTime curTimeGetter() => curTime;

        // List<List<string>> userId = [];

        // userServicesMock.Setup(x => x.GetByIds(It.IsAny<List<string>>())).Returns(() => Task.FromResult(users));
        // userRatingsServicesMock.Setup(x => x.GetUserRatings(It.IsAny<string>())).Returns(() => Task.FromResult(userRatings.First()));
        // publicUserInfoGrain.Setup(x => x.GetPublicUserInfoForNormalUser(It.IsAny<string>())).Returns(() => Task.FromResult(new PlayerInfo(
        //     users.First().Id!,
        //     users.First().Email,
        //     userRatings.First()
        // )));

        // dateTimeMock.Setup(x => x.Now()).Returns(curTimeGetter);

        // dateTimeMock.Setup(x => x.NowFormatted()).Returns(() => dateTimeMock.Object.Now().ToString("o"));

        // hubContextMock.Setup(x => x.SendToClient(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
        //     .Returns(new ValueTask());

        // hubContextMock.Setup(x => x.SendToGroup(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
        //     .Returns(new ValueTask());

        // hubContextMock.Setup(x => x.AddToGroup(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
        //     .Returns(new ValueTask());

        // hubContextMock.Setup(x => x.SendToAll(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
        //     .Returns(new ValueTask());

        // var rows = 9;
        // var cols = 9;
        // var timeControl = new TimeControlDto(
        //     mainTimeSeconds: 10,
        //     incrementSeconds: null,
        //     byoYomiTime: new ByoYomiTime(3, 3)
        // );

        // var gameId = await p1.CreateGame(rows, cols, timeControl, BadukServer.StoneSelectionType.Black, dateTimeMock.Object.NowFormatted());

        // var gameGrain = cluster.GrainFactory.GetGrain<IGameGrain>(gameId);


        // gameService.Setup(x => x.SaveGame(It.IsAny<Game>())).Returns(async () =>
        // {
        //     return await gameGrain.GetGame();
        // });
        // gameService.Setup(x => x.GetGame(It.IsAny<string>())).Returns(async () => await gameGrain.GetGame());

        // ratingEngineMock.Setup(x => x.CalculateRatingAndPerfsAsync(
        //     It.IsAny<string>(),
        //     It.IsAny<BoardSize>(),
        //     It.IsAny<TimeStandard>(),
        //     It.IsAny<Dictionary<string, StoneType>>(),
        //     It.IsAny<List<UserRating>>(),
        //     It.IsAny<DateTime>()
        // )).Returns((
        //     new List<int>(),
        //     new List<PlayerRatingData>(),
        //     new List<PlayerRatingData>(),
        //     new List<UserRating>()
        // ));

        // ratingEngineMock.Setup(x => x.PreviewDeviation(
        //     It.IsAny<PlayerRatingData>(),
        //     It.IsAny<DateTime>(),
        //     It.IsAny<bool>()
        // )).Returns(120);


        // var game = await gameGrain.GetGame();


        // Assert.IsTrue(!game.Players.ContainsKey(p1Id)); // Players should not be in the game yet
        // Assert.IsNotNull(game.GameCreator);
        // Assert.AreEqual(GameState.WaitingForStart, game.GameState);
        // Assert.AreEqual(StoneSelectionType.Black, game.StoneSelectionType);

        // curTime = curTime.AddSeconds(1);

        // var res = await p2.JoinGame(gameId, dateTimeMock.Object.NowFormatted());
        // game = await gameGrain.GetGame();

        // // Now both players should be in the game
        // Assert.IsTrue(game.Players.ContainsKey(p1Id));
        // Assert.IsTrue(game.Players.ContainsKey(p2Id));
        // Assert.AreEqual(StoneType.White, game.Players[p2Id]);
        // Assert.AreEqual(StoneType.Black, game.Players[p1Id]);

        // Assert.AreEqual(GameState.Playing, game.GameState);
        // Assert.AreEqual(dateTimeMock.Object.NowFormatted(), game.StartTime);

        // curTime = curTime.AddSeconds(8);

        // var moveRes = await gameGrain.MakeMove(new MovePosition(1, 1), p1Id);

        // game = await gameGrain.GetGame();

        // Assert.IsTrue(moveRes.moveSuccess);
        // Assert.AreEqual(10 * 1000, game.PlayerTimeSnapshots[1].MainTimeMilliseconds);
        // Assert.AreEqual(2 * 1000, game.PlayerTimeSnapshots[0].MainTimeMilliseconds);

        // curTime = curTime.AddSeconds(2);

        // var moveRes2 = await gameGrain.MakeMove(new MovePosition(2, 2), p2Id);


        // game = await gameGrain.GetGame();

        // Assert.IsTrue(moveRes.moveSuccess);
        // Assert.AreEqual(8 * 1000, game.PlayerTimeSnapshots[1].MainTimeMilliseconds);
        // Assert.AreEqual(2 * 1000, game.PlayerTimeSnapshots[0].MainTimeMilliseconds);

        // curTime = curTime.AddSeconds(2);
        // await Task.Delay((2 * 1000) + 200);

        // gameGrain = cluster.GrainFactory.GetGrain<IGameGrain>(gameId);

        // game = await gameGrain.GetGame();

        // Assert.AreEqual(8 * 1000, game.PlayerTimeSnapshots[1].MainTimeMilliseconds);
        // Assert.AreEqual(3 * 1000 /* byoYomi Time */ , game.PlayerTimeSnapshots[0].MainTimeMilliseconds);
        // Assert.AreEqual(true, game.PlayerTimeSnapshots[0].ByoYomiActive);
        // Assert.AreEqual(3, game.PlayerTimeSnapshots[0].ByoYomisLeft);

        // curTime = curTime.AddSeconds(3);
        // await Task.Delay(3 * 1000 + 200);

        // game = await gameGrain.GetGame();

        // Assert.AreEqual(8 * 1000, game.PlayerTimeSnapshots[1].MainTimeMilliseconds);
        // Assert.AreEqual(3 * 1000 /* byoYomi Time */ , game.PlayerTimeSnapshots[0].MainTimeMilliseconds);
        // Assert.AreEqual(true, game.PlayerTimeSnapshots[0].ByoYomiActive);
        // Assert.AreEqual(2, game.PlayerTimeSnapshots[0].ByoYomisLeft);

        // curTime = curTime.AddSeconds(3);
        // await Task.Delay(3 * 1000 + 200);

        // game = await gameGrain.GetGame();

        // Assert.AreEqual(8 * 1000, game.PlayerTimeSnapshots[1].MainTimeMilliseconds);
        // Assert.AreEqual(3 * 1000 /* byoYomi Time */ , game.PlayerTimeSnapshots[0].MainTimeMilliseconds);
        // Assert.AreEqual(true, game.PlayerTimeSnapshots[0].ByoYomiActive);
        // Assert.AreEqual(1, game.PlayerTimeSnapshots[0].ByoYomisLeft);


        // curTime = curTime.AddSeconds(3);
        // await Task.Delay(3 * 1000 + 200);

        // game = await gameGrain.GetGame();

        // Assert.AreEqual(8 * 1000, game.PlayerTimeSnapshots[1].MainTimeMilliseconds);
        // Assert.AreEqual(0 /* all byo yomi gone */, game.PlayerTimeSnapshots[0].MainTimeMilliseconds);
        // Assert.AreEqual(true, game.PlayerTimeSnapshots[0].ByoYomiActive);
        // Assert.AreEqual(0, game.PlayerTimeSnapshots[0].ByoYomisLeft);





        // Assert.AreEqual(GameState.Ended, game.GameState);
        // Assert.AreEqual(GameOverMethod.Timeout, game.GameOverMethod);
        // Assert.AreEqual(p2Id, game.WinnerId);



        // cluster.StopAllSilos();
    }



    sealed class TestSiloConfigurations : ISiloConfigurator
    {
        public void Configure(ISiloBuilder siloBuilder)
        {
            siloBuilder.ConfigureServices(static services =>
            {
                services.AddSingleton((a) => dateTimeMock.Object);
                services.AddSingleton((a) => hubContextMock.Object);
                services.AddSingleton((a) => userServicesMock.Object);
                services.AddSingleton((a) => userRatingsServicesMock.Object);
                services.AddSingleton((a) => gameService.Object);
                services.AddSingleton((a) => ratingEngineMock.Object);
                services.AddSingleton((a) => publicUserInfoGrain.Object);
            });

            siloBuilder.Services.AddSerializer();
        }
    }
}