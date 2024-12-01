using BadukServer;

namespace Tests;
[TestClass]
public class TimeCalculatorTest
{
    List<PlayerTimeSnapshot> _playerTimeSnapshots;
    TimeControl _timeControl;
    DateTime _curTime;
    int _turn;

    StoneType CurTurn() => (StoneType)(_turn % 2);
    string CurTime() => _curTime.SerializedDate();

    private void Setup()
    {
        _turn = 0;
        _curTime = _1980Jan1_1_30PM;
        _timeControl = new TimeControl(
            mainTimeSeconds: 10,
            incrementSeconds: null,
            byoYomiTime: new ByoYomiTime(3, 3),
            timeStandard: TimeStandard.Blitz
        );

        _playerTimeSnapshots = [
                    new PlayerTimeSnapshot(
                snapshotTimestamp: CurTime(),
                mainTimeMilliseconds: _timeControl.MainTimeSeconds * 1000,
                byoYomisLeft: _timeControl.ByoYomiTime?.ByoYomis,
                byoYomiActive: false,
                timeActive: true
            ),
            new PlayerTimeSnapshot(
                snapshotTimestamp: CurTime(),
                mainTimeMilliseconds: _timeControl.MainTimeSeconds * 1000,
                byoYomisLeft: _timeControl.ByoYomiTime?.ByoYomis,
                byoYomiActive: false,
                timeActive: false
            )
                ];
    }

    private void SetupIncrement()
    {
        _turn = 0;
        _curTime = _1980Jan1_1_30PM;
        _timeControl = new TimeControl(
            mainTimeSeconds: 10,
            incrementSeconds: 3,
            byoYomiTime: null,
            timeStandard: TimeStandard.Blitz
        );

        _playerTimeSnapshots = [
                    new PlayerTimeSnapshot(
                snapshotTimestamp: CurTime(),
                mainTimeMilliseconds: _timeControl.MainTimeSeconds * 1000,
                byoYomisLeft: _timeControl.ByoYomiTime?.ByoYomis,
                byoYomiActive: false,
                timeActive: true
            ),
            new PlayerTimeSnapshot(
                snapshotTimestamp: CurTime(),
                mainTimeMilliseconds: _timeControl.MainTimeSeconds * 1000,
                byoYomisLeft: _timeControl.ByoYomiTime?.ByoYomis,
                byoYomiActive: false,
                timeActive: false
            )
                ];
    }


    [TestMethod]
    public void TestByoYomi()
    {
        // Arrange
        var timeCalculator = new TimeCalculator();

        Setup();

        List<PlayerTimeSnapshot> ReCalc()
        {
            var res = timeCalculator.RecalculateTurnPlayerTimeSnapshots(CurTurn(), _playerTimeSnapshots, _timeControl, CurTime());
            _playerTimeSnapshots = res;
            return res;
        }

        // Make one move
        _curTime = _curTime.AddSeconds(8);
        _turn += 1;
        var result = ReCalc();

        // Test One move made after 8 seconds

        Assert.AreEqual(10 * 1000, result[1].MainTimeMilliseconds);
        Assert.AreEqual(2 * 1000, result[0].MainTimeMilliseconds);

        _curTime = _curTime.AddSeconds(2);
        _turn += 1;
        result = ReCalc();

        Assert.AreEqual(8 * 1000, result[1].MainTimeMilliseconds);
        Assert.AreEqual(2 * 1000, result[0].MainTimeMilliseconds);

        _curTime = _curTime.AddSeconds(2);
        result = ReCalc();

        Assert.AreEqual(8 * 1000, result[1].MainTimeMilliseconds);
        Assert.AreEqual(3 * 1000 /* byoYomi Time */ , result[0].MainTimeMilliseconds);
        Assert.AreEqual(true, result[0].ByoYomiActive);
        Assert.AreEqual(3, result[0].ByoYomisLeft);

        _curTime = _curTime.AddSeconds(3);
        result = ReCalc();

        Assert.AreEqual(8 * 1000, result[1].MainTimeMilliseconds);
        Assert.AreEqual(3 * 1000 /* byoYomi Time */ , result[0].MainTimeMilliseconds);
        Assert.AreEqual(true, result[0].ByoYomiActive);
        Assert.AreEqual(2, result[0].ByoYomisLeft);

        _curTime = _curTime.AddSeconds(3);
        result = ReCalc();

        Assert.AreEqual(8 * 1000, result[1].MainTimeMilliseconds);
        Assert.AreEqual(3 * 1000 /* byoYomi Time */ , result[0].MainTimeMilliseconds);
        Assert.AreEqual(true, result[0].ByoYomiActive);
        Assert.AreEqual(1, result[0].ByoYomisLeft);


        _curTime = _curTime.AddSeconds(3);
        result = ReCalc();

        Assert.AreEqual(8 * 1000, result[1].MainTimeMilliseconds);
        Assert.AreEqual(0 /* all byo yomi gone */, result[0].MainTimeMilliseconds);
        Assert.AreEqual(true, result[0].ByoYomiActive);
        Assert.AreEqual(0, result[0].ByoYomisLeft);
    }


    [TestMethod]
    public void TestIncrement()
    {
        // Arrange
        var timeCalculator = new TimeCalculator();

        SetupIncrement();

        List<PlayerTimeSnapshot> ReCalc()
        {
            var res = timeCalculator.RecalculateTurnPlayerTimeSnapshots(CurTurn(), _playerTimeSnapshots, _timeControl, CurTime());
            _playerTimeSnapshots = res;
            return res;
        }

        // Make one move
        _curTime = _curTime.AddSeconds(8);
        _turn += 1;
        var result = ReCalc();

        // Test One move made after 8 seconds

        Assert.AreEqual(10 * 1000, result[1].MainTimeMilliseconds);
        Assert.AreEqual(5 * 1000, result[0].MainTimeMilliseconds);

        _curTime = _curTime.AddSeconds(2);
        _turn += 1;
        result = ReCalc();

        Assert.AreEqual(11 * 1000, result[1].MainTimeMilliseconds);
        Assert.AreEqual(5 * 1000, result[0].MainTimeMilliseconds);

        _curTime = _curTime.AddSeconds(2);
        result = ReCalc();

        Assert.AreEqual(11 * 1000, result[1].MainTimeMilliseconds);
        Assert.AreEqual(3 * 1000, result[0].MainTimeMilliseconds);

        _curTime = _curTime.AddSeconds(3);
        result = ReCalc();

        Assert.AreEqual(11 * 1000, result[1].MainTimeMilliseconds);
        Assert.AreEqual(0 * 1000, result[0].MainTimeMilliseconds);

        // _curTime = _curTime.AddSeconds(3);
        // result = ReCalc();

        // Assert.AreEqual(8 * 1000, result[1].MainTimeMilliseconds);
        // Assert.AreEqual(0 /* all byo yomi gone */, result[0].MainTimeMilliseconds);
        // Assert.AreEqual(true, result[0].ByoYomiActive);
        // Assert.AreEqual(0, result[0].ByoYomisLeft);
    }

    static public DateTime _1980Jan1_1_30PM = new DateTime(
        year: 1980,
        month: 1,
        day: 1,
        hour: 13,
        minute: 30,
        second: 0
    );


}