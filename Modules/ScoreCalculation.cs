
using System.Collections.ObjectModel;
using BadukServer;

public class ScoreCalculation
{
    public Dictionary<Position, Area> AreaMap = [];
    List<int> _territoryScores = [];
    List<int> _score = [];
    public ReadOnlyCollection<int> Score => _score.AsReadOnly();
    Dictionary<Position, Stone> VirtualPlaygroundMap = [];
    HashSet<Cluster> DeadClusters = [];
    // public Game Game;
    public List<int> Prisoners;
    public List<Position> DeadStones;
    public int Rows;
    public int Cols;
    public float Komi;
    private GameResult _finalResult;
    public GameResult finalResult { get => _finalResult; }

    private void _CalculateResult()
    {
        var blackStonesCount = VirtualPlaygroundMap.Values.Where(a => a.player == (int)StoneType.Black).Count();
        var blackScore = _territoryScores[0] + blackStonesCount;

        var whiteStonesCount = VirtualPlaygroundMap.Values.Where(a => a.player == (int)StoneType.White).Count();
        var whiteScore = _territoryScores[1] + Komi + whiteStonesCount;

        _score = [blackScore, (int)MathF.Round((whiteScore - Komi))];

        if (blackScore == whiteScore) _finalResult = GameResult.Draw;

        _finalResult = (blackScore > whiteScore) ? GameResult.BlackWon : GameResult.WhiteWon;
    }

    public ScoreCalculation(
        int rows,
        int cols,
        float komi,
        List<int> prisoners,
        List<Position> deadStones,
Dictionary<Position, Stone> playground
)
    {
        VirtualPlaygroundMap = playground;
        DeadClusters = [];

        Rows = rows;
        Cols = cols;
        Komi = komi;
        Prisoners = prisoners;
        DeadStones = deadStones;

        foreach (var pos in DeadStones)
        {
            DeadClusters.Add(playground[pos]!.cluster);
        }

        _CalculateScore();
        _CalculateResult();
    }

    private void _CalculateScore()
    {
        var rows = Rows;
        var cols = Cols;

        foreach (Cluster cluster in DeadClusters)
        {
            foreach (var pos in cluster.data)
            {
                VirtualPlaygroundMap.Remove(pos);
            }
        }

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                if (!AreaMap.ContainsKey(new Position(i, j)) &&
                    !VirtualPlaygroundMap.ContainsKey(new Position(i, j)))
                {
                    var pos = new Position(i, j);
                    ForEachEmptyPosition(pos, [pos], null, false, []);
                }
            }
        }
        _territoryScores = [0, 0];

        foreach (var area in AreaMap.Values)
        {
            if (area.Owner != null)
            {
                // _territoryScores[Constants.playerColors
                //     .indexWhere((element) => element == value.value?.owner)] += 1;

                _territoryScores[(int)area.Owner] += 1;
            }
        }
    }
    bool checkIfInsideBounds(Position pos)
    {
        var rows = Rows;
        var cols = Cols;
        return pos.X > -1 && pos.X < rows && pos.Y < cols && pos.Y > -1;
    }


    (HashSet<Position> positionsSeenSoFar, int? owner, bool isDame, List<Cluster> clusterEncountered) ForEachEmptyPosition(Position startPos, HashSet<Position> positionsSeenSoFar, int? owner, bool isDame, List<Cluster> clusterEncountered)
    {
        if (checkIfInsideBounds(startPos))
        {
            if (VirtualPlaygroundMap.ContainsKey(startPos))
            {
                if (!clusterEncountered
                    .Contains(VirtualPlaygroundMap[startPos]!.cluster))
                {
                    // so maybe this can be removed only cases i can think of is the first stone in which it maybe doesn't matter if we include it's cluster
                    clusterEncountered.Add(VirtualPlaygroundMap[startPos]!.cluster);
                    return (positionsSeenSoFar, owner, isDame, clusterEncountered);
                }
            }
            StoneLogic.DoActionOnNeighbors(startPos, (curPos, neighbor) =>
            {
                if (checkIfInsideBounds(neighbor))
                {
                    if (!VirtualPlaygroundMap.ContainsKey(neighbor))
                    {
                        if (!positionsSeenSoFar.Contains(neighbor))
                        {
                            var res = ForEachEmptyPosition(neighbor, [.. positionsSeenSoFar, neighbor], owner, isDame, clusterEncountered);

                            positionsSeenSoFar = res.positionsSeenSoFar;
                            owner = res.owner;
                            isDame = res.isDame;
                            clusterEncountered = res.clusterEncountered;

                            return;
                        }
                    }
                    if (VirtualPlaygroundMap.ContainsKey(neighbor))
                    {
                        if (!clusterEncountered.Contains(VirtualPlaygroundMap[neighbor]!.cluster))
                        {
                            clusterEncountered.Add(VirtualPlaygroundMap[neighbor]!.cluster);
                        }

                        if (owner == null && !isDame)
                        {
                            owner = VirtualPlaygroundMap[neighbor]?.player;
                        }
                        else if (owner != null &&
                            VirtualPlaygroundMap[neighbor]!.player != owner)
                        {
                            owner = null;
                            isDame = true;
                        }
                        return;
                    }
                }
            });


            foreach (var pos in positionsSeenSoFar)
            {
                AreaMap[pos] = new Area(owner, positionsSeenSoFar);
            }

        }
        return (positionsSeenSoFar, owner, isDame, clusterEncountered);
    }
}