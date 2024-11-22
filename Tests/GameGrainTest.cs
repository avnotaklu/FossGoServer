using BadukServer;
using BadukServer.Orleans.Grains;
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
    static public Mock<ISignalRGameHubService> hubContextMock = new();
    static public Mock<IPushNotifierGrain> journal = new Mock<IPushNotifierGrain>();

    static public DateTime _1980Jan1_1_30PM = new DateTime(
        year: 1980,
        month: 1,
        day: 1,
        hour: 13,
        minute: 30,
        second: 0
    );

    [TestMethod]
    public async Task GameGrainTest()
    {
        var builder = new TestClusterBuilder();
        var cluster = builder
            .AddSiloBuilderConfigurator<TestSiloConfigurations>()
            .Build();
        cluster.Deploy();


        var curTime = _1980Jan1_1_30PM;

        var p1Id = "p1";
        var p2Id = "p2";
        var p1ConId = "p1Con";
        var p2ConId = "p2Con";

        var p1 = cluster.GrainFactory.GetGrain<IPlayerGrain>(p1Id);
        await p1.InitializePlayer(p1ConId);
        var p2 = cluster.GrainFactory.GetGrain<IPlayerGrain>(p2Id);
        await p2.InitializePlayer(p2ConId);

        DateTime curTimeGetter() => curTime;

        dateTimeMock.Setup(x => x.Now()).Returns(curTimeGetter);
        dateTimeMock.Setup(x => x.NowFormatted()).Returns(() => dateTimeMock.Object.Now().ToString("o"));

        hubContextMock.Setup(x => x.SendToClient(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask());

        hubContextMock.Setup(x => x.SendToAll(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask());

        var rows = 9;
        var cols = 9;
        var timeControl = new TimeControl(
            mainTimeSeconds: 10,
            incrementSeconds: null,
            byoYomiTime: new ByoYomiTime(3, 3)
        );

        var gameId = await p1.CreateGame(rows, cols, timeControl, BadukServer.StoneSelectionType.Black, dateTimeMock.Object.NowFormatted());

        var gameGrain = cluster.GrainFactory.GetGrain<IGameGrain>(gameId);
        var stoneType = await gameGrain.GetStoneFromPlayerId(p1Id);

        var game = await gameGrain.GetGame();


        Assert.IsTrue(game.Players.ContainsKey(p1Id));
        Assert.AreEqual(GameState.WaitingForStart, game.GameState);
        Assert.AreEqual(StoneType.Black, game.Players[p1Id]);

        curTime = curTime.AddSeconds(1);

        var res = await p2.JoinGame(gameId, dateTimeMock.Object.NowFormatted());
        game = await gameGrain.GetGame();

        Assert.IsTrue(game.Players.ContainsKey(p2Id));
        Assert.AreEqual(StoneType.White, game.Players[p2Id]);
        Assert.AreEqual(GameState.Playing, game.GameState);
        Assert.AreEqual(dateTimeMock.Object.NowFormatted(), game.StartTime);

        curTime = curTime.AddSeconds(8);

        var moveRes = await gameGrain.MakeMove(new MovePosition(1, 1), p1Id);

        game = await gameGrain.GetGame();

        Assert.IsTrue(moveRes.moveSuccess);
        Assert.AreEqual(10 * 1000, game.PlayerTimeSnapshots[1].MainTimeMilliseconds);
        Assert.AreEqual(2 * 1000, game.PlayerTimeSnapshots[0].MainTimeMilliseconds);

        curTime = curTime.AddSeconds(2);

        var moveRes2 = await gameGrain.MakeMove(new MovePosition(2, 2), p2Id);


        game = await gameGrain.GetGame();

        Assert.IsTrue(moveRes.moveSuccess);
        Assert.AreEqual(8 * 1000, game.PlayerTimeSnapshots[1].MainTimeMilliseconds);
        Assert.AreEqual(2 * 1000, game.PlayerTimeSnapshots[0].MainTimeMilliseconds);

        curTime = curTime.AddSeconds(2);
        await Task.Delay((2 * 1000) + 200);

        gameGrain = cluster.GrainFactory.GetGrain<IGameGrain>(gameId);

        game = await gameGrain.GetGame();

        Assert.AreEqual(8 * 1000, game.PlayerTimeSnapshots[1].MainTimeMilliseconds);
        Assert.AreEqual(3 * 1000 /* byoYomi Time */ , game.PlayerTimeSnapshots[0].MainTimeMilliseconds);
        Assert.AreEqual(true, game.PlayerTimeSnapshots[0].ByoYomiActive);
        Assert.AreEqual(3, game.PlayerTimeSnapshots[0].ByoYomisLeft);

        curTime = curTime.AddSeconds(3);
        await Task.Delay(3 * 1000 + 200);

        game = await gameGrain.GetGame();

        Assert.AreEqual(8 * 1000, game.PlayerTimeSnapshots[1].MainTimeMilliseconds);
        Assert.AreEqual(3 * 1000 /* byoYomi Time */ , game.PlayerTimeSnapshots[0].MainTimeMilliseconds);
        Assert.AreEqual(true, game.PlayerTimeSnapshots[0].ByoYomiActive);
        Assert.AreEqual(2, game.PlayerTimeSnapshots[0].ByoYomisLeft);

        curTime = curTime.AddSeconds(3);
        await Task.Delay(3 * 1000 + 200);

        game = await gameGrain.GetGame();

        Assert.AreEqual(8 * 1000, game.PlayerTimeSnapshots[1].MainTimeMilliseconds);
        Assert.AreEqual(3 * 1000 /* byoYomi Time */ , game.PlayerTimeSnapshots[0].MainTimeMilliseconds);
        Assert.AreEqual(true, game.PlayerTimeSnapshots[0].ByoYomiActive);
        Assert.AreEqual(1, game.PlayerTimeSnapshots[0].ByoYomisLeft);


        curTime = curTime.AddSeconds(3);
        await Task.Delay(3 * 1000 + 200);

        game = await gameGrain.GetGame();

        Assert.AreEqual(8 * 1000, game.PlayerTimeSnapshots[1].MainTimeMilliseconds);
        Assert.AreEqual(0 /* all byo yomi gone */, game.PlayerTimeSnapshots[0].MainTimeMilliseconds);
        Assert.AreEqual(true, game.PlayerTimeSnapshots[0].ByoYomiActive);
        Assert.AreEqual(0, game.PlayerTimeSnapshots[0].ByoYomisLeft);





        Assert.AreEqual(GameState.Ended, game.GameState);
        Assert.AreEqual(GameOverMethod.Timeout, game.GameOverMethod);
        Assert.AreEqual(p2Id, game.WinnerId);



        cluster.StopAllSilos();
    }



    sealed class TestSiloConfigurations : ISiloConfigurator
    {
        public void Configure(ISiloBuilder siloBuilder)
        {
            siloBuilder.ConfigureServices(static services =>
            {
                services.AddSingleton((a) => dateTimeMock.Object);
                services.AddSingleton((a) => hubContextMock.Object);
            });

            siloBuilder.Services.AddSerializer();
        }
    }
}