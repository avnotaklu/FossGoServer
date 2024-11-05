using System.Diagnostics;
using ZstdSharp.Unsafe;

using HighLevelBoardRepresentation = System.Collections.Generic.Dictionary<BadukServer.Position, BadukServer.StoneType>;

namespace BadukServer
{
    public class Cluster
    {
        public HashSet<Position> data;
        public HashSet<Position> FreedomPositions;
        public int freedoms;
        public int player;

        public Cluster(HashSet<Position> data, HashSet<Position> freedomPositions, int freedoms, int player)
        {
            this.data = data;
            this.freedoms = freedoms;
            this.player = player;
            this.FreedomPositions = freedomPositions;
        }
    }
    public class Stone
    {
        public Position position;
        public int player;
        public Cluster cluster;

        public Stone(Position position, int player, Cluster cluster)
        {
            this.position = position;
            this.player = player;
            this.cluster = cluster;
        }

    }

    [Immutable, GenerateSerializer]
    public class Position
    {
        public int X {get; set;}
        public int Y {get; set;}

        public Position(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        public Position(string repr)
        {
            var x = repr.Split(" ")[0];
            var y = repr.Split(" ")[1];

            this.X = int.Parse(x);
            this.Y = int.Parse(y);
        }

        public override bool Equals(object? obj)
        {
            if (obj is Position pos)
            {
                return X == pos.X && Y == pos.Y;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return X ^ Y;
        }

        public override string ToString()
        {
            return $"{X}, {Y}";
        }

        public string ToHighLevelRepr()
        {
            return $"{X} {Y}";
        }
    }
    public class BoardState
    {
        public int rows;
        public int cols;
        // koDelete assumes that this position was deleted in the last move by the opposing player
        public Position? koDelete;
        public List<int> prisoners = [0, 0];
        public Dictionary<Position, Stone> playgroundMap;

        public BoardState(int rows, int cols, Position? koDelete, Dictionary<Position, Stone> playgroundMap, List<int> prisoners)
        {
            this.rows = rows;
            this.cols = cols;
            this.koDelete = koDelete;
            this.prisoners = prisoners;
            this.playgroundMap = playgroundMap;
        }
    }

    public class StoneLogic
    {
        public BoardState board;

        public StoneLogic(BoardState board)
        {
            this.board = board;
        }

        Stone? stoneAt(Position pos)
        {
            if (!board.playgroundMap.ContainsKey(pos))
            {
                return null;
            }
            return board.playgroundMap[pos];
        }

        Cluster? getClusterFromPosition(Position pos)
        {
            return stoneAt(pos)?.cluster;
        }

        /// returns true if not out of bounds
        bool checkIfInsideBounds(Position pos)
        {
            var rows = board.rows;
            var cols = board.cols;
            return pos.X > -1 && pos.X < rows && pos.Y < cols && pos.Y > -1;
        }

        /* Main Stone Logic functionality */

        // --- finally update freedoms for newly inserted stone
        // Update freedom by calculating only current stones neighbor( We can increment and decrement for neighbors in this way)
        void UpdateFreedomsFromNewlyInsertedStone(Position position, Dictionary<Position, List<Cluster>> traversed)
        {
            DoActionOnNeighbors(position, (Position curpos, Position neighbor) =>
            {
                var neighborCluster = getClusterFromPosition(neighbor);
                var curPosCluster = getClusterFromPosition(curpos);
                if (neighborCluster != null && curPosCluster != null && (!traversed.ContainsKey(curpos) || !traversed[curpos].Contains(neighborCluster)))
                {
                    // if( _playgroundMap[neighbor] == null && checkOutofBounds(neighbor))
                    // {
                    //   _playgroundMap[curpos]?.cluster.freedoms += 1;
                    // }
                    if (stoneAt(neighbor)!.player !=
                        stoneAt(curpos)!.player)
                    {
                        neighborCluster.freedoms -= 1;

                        if (!traversed.ContainsKey(curpos))
                        {
                            traversed[curpos] = [neighborCluster];
                        }
                        else
                        {
                            traversed[curpos]!.Add(neighborCluster);
                        }
                    }
                }
            });
        }

        // Hack
        bool checkInsertable(Position position)
        {
            if (board.koDelete?.Equals(position) ?? false)
            {
                return false;
            }
            bool insertable = false;
            DoActionOnNeighbors(
                position,
                (curpos, neighbor) =>
                {
                    if (stoneAt(neighbor) != null)
                    {
                        if (!insertable)
                        {
                            if (stoneAt(neighbor)!.player == stoneAt(curpos)!.player)
                            {
                                insertable = !(getClusterFromPosition(neighbor)?.freedoms == 1);
                            }
                            else
                            {
                                insertable = getClusterFromPosition(neighbor)?.freedoms == 1;
                            }

                        }
                    }
                    else if (checkIfInsideBounds(neighbor))
                    {
                        insertable = true;
                    }
                });
            return insertable;
        }

        // Update Freedom by going through all stone in cluster and counting freedom for every stone

        /* From here */
        void CalculateFreedomForPosition(Position position, Dictionary<Position, List<Cluster>> traversed)
        {
            DoActionOnNeighbors(position, (Position curpos, Position neighbor) =>
            {
                // if( _playgroundMap[neighbor] == null && checkOutofBounds(neighbor) && alreadyAdded.contains(neighbor) == false)//  && (traversed[neighbor]?.contains(getClusterFromPosition(curpos)) == false) )
                if (stoneAt(neighbor) == null &&
                    checkIfInsideBounds(neighbor) &&
                    stoneAt(curpos) != null &&
                    (!traversed.ContainsKey(neighbor) || !traversed[neighbor].Contains(getClusterFromPosition(curpos)!)))
                //  && (traversed[neighbor]?.contains(getClusterFromPosition(curpos)) == false) )
                // neighbor are the possible free position here unlike recalculateFreedomsForNeighborsOfDeleted where deletedStonePosition is the free position and neighbors are possible clusters for which we will increment freedoms
                {
                    stoneAt(curpos)!.cluster.freedoms += 1;
                }
                traversed[neighbor] = [getClusterFromPosition(curpos)];
            });
        }

        void CalculateFreedomForCluster(Cluster cluster, Dictionary<Position, List<Cluster>> traversed)
        {
            foreach (var i in cluster.data)
            {
                CalculateFreedomForPosition(i, traversed);
            }
        }
        /* to here */

        // --- Step 1
        void addMatchingNeighborsToCluster(
            Position curpos, Position? neighbor) // done on all neighbors
        {
            if (neighbor != null &&
                stoneAt(neighbor)?.player == stoneAt(curpos)?.player)
            {
                // If neighbor isn't null and both neighbor and curpos both have same color
                foreach (var i in getClusterFromPosition(neighbor)!.data)
                {
                    // add all of neighbors Position to cluster of curpos
                    stoneAt(curpos)?.cluster.data.Add(i);
                }
            }
        }

        void UpdateAllInTheClusterWithCorrectCluster(Cluster correctCluster)
        {
            foreach (var i in correctCluster.data)
            {
                var stone = stoneAt(i);

                if (stone != null)
                {
                    stone.cluster = correctCluster;
                }
            }
        }
        // Step 1 ---

        // --- Deletion
        // Traversed key gives the empty freedom point position and value is the list of cluster that has recieved freedom from that point
        void deleteStonesInDeletableCluster(Position curpos, Position neighbor, Dictionary<Position, List<Cluster>> traversed)
        {
            if (stoneAt(neighbor)?.player != stoneAt(curpos)?.player &&
                stoneAt(neighbor)?.cluster.freedoms == 1)
            {
                foreach (var i in getClusterFromPosition(neighbor)!.data)
                {
                    // This supposedly works because a
                    // position where delete occurs in such a way that ko is possible
                    // the cluster at that position can only have one member because
                    // all the neighboring ones have to opposite ones for ko to be possible

                    // how do we solve the behaviour when neighboring cells will be null

                    // we store in koDelete The position that was deleted
                    // we check that against newly entered stone and stone can only be deleted when neighboring cells will be opposite
                    //

                    if (getClusterFromPosition(i)!.data.Count == 1)
                    {
                        Console.WriteLine("Setting ko delete" + neighbor);
                        board.koDelete = neighbor;
                    }

                    board.prisoners[1 - stoneAt(i)!.player] += 1;
                    board.playgroundMap.Remove(i);
                    // _playgroundMap.remove(i);

                    recalculateFreedomsForNeighborsOfDeleted(i, traversed);
                }
            }
        }

        void recalculateFreedomsForNeighborsOfDeleted(Position deletedStonePosition, Dictionary<Position, List<Cluster>> traversed)
        {
            // If a deleted position( free position ) has already contributed to the freedoms of a cluster it should not contribute again as that will result in duplication
            // A list of clusters is stored to keep track of what cluster has recieved freedoms points one free position can't give two freedoms to one cluster
            // but it can give freedom to different cluster
            DoActionOnNeighbors(deletedStonePosition,
                (Position curpos, Position neighbor) =>
                {
                    if (traversed.ContainsKey(curpos) == false)
                    {
                        traversed[curpos] = [];
                        // :assert(traversed[curpos]!.contains(getClusterFromPosition(neighbor)));
                    }
                    var neighborCluster = getClusterFromPosition(neighbor);
                    if (neighborCluster == null)
                    {
                        return;
                    }
                    if (traversed[curpos]!.Contains(neighborCluster) == false)
                    {
                        // var neighborCluster = getClusterFromPosition(neighbor);
                        if (neighborCluster != null)
                        {
                            neighborCluster.freedoms += 1;
                            traversed[curpos]?.Add(neighborCluster);
                            Debug.Assert(traversed[curpos]!.Contains(neighborCluster));
                        }
                    }
                });
        }

        // Deletion ---

        public (bool result, BoardState board) HandleStoneUpdate(Position? position, int player)
        {
            if (position == null)
            {
                return (true, board);
            }
            Position? thisCurrentCell = position;

            var current_cluster = new Cluster([position], [], 0, player);

            board.playgroundMap[thisCurrentCell] = new Stone(
              position: position,
              player: player,
              cluster: current_cluster
            );

            // StoneWidget(gameStateBloc?.getPlayerWithTurn.mColor, position);

            if (checkInsertable(position))
            {
                Dictionary<Position, List<Cluster>> traversed = [];
                // if stone can be inserted at this position
                board.koDelete = null;
                DoActionOnNeighbors(position, addMatchingNeighborsToCluster);
                UpdateAllInTheClusterWithCorrectCluster(current_cluster);
                DoActionOnNeighbors(position, (a, b) => deleteStonesInDeletableCluster(a, b, traversed));
                CalculateFreedomForCluster(current_cluster, traversed);
                UpdateFreedomsFromNewlyInsertedStone(position, traversed);
                return (true, board);
            }

            board.playgroundMap.Remove(thisCurrentCell);
            return (false, board);
        }

        public static void DoActionOnNeighbors(Position thisCell,
            Action<Position, Position> doAction)
        {
            var rowPlusOne = new Position(thisCell.X + 1, thisCell.Y);
            doAction(thisCell, rowPlusOne);
            var rowMinusOne = new Position(thisCell.X - 1, thisCell.Y);
            doAction(thisCell, rowMinusOne);
            var colPlusOne = new Position(thisCell.X, thisCell.Y + 1);
            doAction(thisCell, colPlusOne);
            var colMinusOne = new Position(thisCell.X, thisCell.Y - 1);
            doAction(thisCell, colMinusOne);
        }

        // Scoring

        // Extend outward by checking all neighbors approach
    }


    public class ScoreCalculation
    {
        public Dictionary<Position, Area> AreaMap = [];
        public List<Cluster> clusterEncountered = [];
        List<int> _territoryScores = [];
        Dictionary<Position, Stone> VirtualPlaygroundMap = [];
        HashSet<Cluster> DeadClusters = [];
        public Game Game;
        public string BlackPlayerId;
        public string WhitePlayerId;
        public int komi;

        StoneType GetWinner(Game game)
        {
            var blackScore = _territoryScores[0] + game.Prisoners[BlackPlayerId];
            var whiteScore = _territoryScores[1] + game.Prisoners[WhitePlayerId] + komi;
            var winner = (blackScore > whiteScore) ? StoneType.Black : StoneType.White;
            return winner;
        }

        // GETTERS
        // List<int> scores()
        // {
        //     if (_territoryScores.isNotEmpty) return _territoryScores;
        //     calculateScore();
        //     return _territoryScores;
        // }

        public ScoreCalculation(
    Game game,
    string blackPlayerId,
    string whitePlayerId,
Dictionary<Position, Stone> playground,
HashSet<Cluster> deadClusters
    )
        {
            Game = game;
            BlackPlayerId = blackPlayerId;
            WhitePlayerId = whitePlayerId;
            VirtualPlaygroundMap = playground;
            DeadClusters = deadClusters;
        }

        public void CalculateScore()
        {
            clusterEncountered.Clear();
            var rows = Game.Rows;
            var cols = Game.Columns;

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
                    if (AreaMap[new Position(i, j)] == null &&
                        VirtualPlaygroundMap[new Position(i, j)] == null)
                    {
                        var pos = new Position(i, j);
                        ForEachEmptyPosition(pos, [pos], null, false);
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
            var rows = Game.Rows;
            var cols = Game.Columns;
            return pos.X > -1 && pos.X < rows && pos.Y < cols && pos.Y > -1;
        }


        void ForEachEmptyPosition(Position startPos, HashSet<Position> positionsSeenSoFar, int? owner, bool isDame)
        {
            if (checkIfInsideBounds(startPos))
            {
                if (VirtualPlaygroundMap[startPos] != null)
                {
                    if (!clusterEncountered
                        .Contains(VirtualPlaygroundMap[startPos]!.cluster))
                    {
                        // TODO: idk if it is possible to visit a stone at curpos without having it in cluster
                        // so maybe this can be removed only cases i can think of is the first stone in which it maybe doesn't matter if we include it's cluster
                        clusterEncountered.Add(VirtualPlaygroundMap[startPos]!.cluster);
                        return;
                    }
                }
                StoneLogic.DoActionOnNeighbors(startPos, (curPos, neighbor) =>
                {
                    if (checkIfInsideBounds(neighbor))
                    {
                        if (VirtualPlaygroundMap[neighbor] == null)
                        {
                            if (!positionsSeenSoFar.Contains(neighbor))
                            {
                                ForEachEmptyPosition(neighbor, [.. positionsSeenSoFar, neighbor], owner, isDame);
                            }
                        }
                        if (VirtualPlaygroundMap[neighbor]?.player != null)
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
                        }
                    }
                });
            }
            foreach (var pos in positionsSeenSoFar)
            {
                AreaMap[pos] = new Area(owner, positionsSeenSoFar);
            }
        }
    }

    public class Area
    {
        public HashSet<Position> Spaces;
        // int value;
        public int Value => Spaces.Count;
        public int? Owner;
        public bool IsDame => Owner == null;

        public Area(int? owner, HashSet<Position> spaces)
        {
            Owner = owner;
            Spaces = spaces;
        }
        // public Area()
        // {
        //     IsDame = false;
        //     Owner = null;
        // }
    }

    public class BoardStateUtilities(int rows, int cols)
    {

        // public int[,] GetFreedomsFromBoard(int[,] board)
        // {
        //     int[,] freedoms = new int[board.GetLength(0), board.GetLength(1)];
        //     for (int i = 0; i < board.GetLength(1); i++)
        //     {
        //         for (int j = 0; j < board.GetLength(0); j++)
        //         {
        //             if (board[i, j] != 0)
        //             {
        //                 List<Position> neighbors = [
        //                     new Position(i - 1, j),
        //                 new Position(i , j - 1),
        //                 new Position(i , j + 1),
        //                 new Position(i + 1, j),
        //             ];
        //                 int freedomsForMe = 0;

        //                 foreach (var neighbor in neighbors)
        //                 {
        //                     if (checkIfInsideBounds(neighbor) && board[neighbor.x, neighbor.y] == 0)
        //                     {
        //                         freedomsForMe += 1;
        //                     }
        //                 }
        //                 freedoms[i, j] = freedomsForMe;
        //             }
        //         }
        //     }
        //     return freedoms;
        // }
        public BoardState BoardStateFromGame(Game game)
        {
            var map = game.PlaygroundMap;

            var simpleB = SimpleBoardRepresentation(map);
            var clusters = GetClusters(simpleB);

            var stones = GetStones(clusters);
            var board = ConstructBoard(rows, cols, stones, game.KoPositionInLastMove == null ? null : new Position(game.KoPositionInLastMove!));

            return board;
        }
        public int[,] SimpleBoardRepresentation(Dictionary<string, StoneType> map)
        {
            var board = new int[rows, cols];

            foreach (var item in map)
            {
                var position = new Position(item.Key);
                board[position.X, position.Y] = (int)item.Value + 1;
            }

            return board;
        }

        public List<Cluster> GetClusters(int[,] board)
        {
            Dictionary<Position, Cluster> clusters = [];

            void MergeClusters(Cluster a, Cluster b)
            {
                a.data.UnionWith(b.data);
                a.FreedomPositions.UnionWith(b.FreedomPositions);
                a.freedoms = a.FreedomPositions.Count;
                // b.data = a.data;
                foreach (var pos in a.data)
                {
                    clusters[pos] = a;
                }
            }
            for (int i = 0; i < board.GetLength(1); i++)
            {
                for (int j = 0; j < board.GetLength(0); j++)
                {
                    var curpos = new Position(i, j);

                    List<Position> neighbors = [
                        new Position(i - 1, j),
                        new Position(i , j - 1),
                        new Position(i , j + 1),
                        new Position(i + 1, j),
                    ];

                    neighbors.RemoveAll(p => !checkIfInsideBounds(p));

                    if (board[i, j] != 0)
                    {
                        foreach (var n in neighbors)
                        {
                            if (!clusters.ContainsKey(curpos))
                            {
                                clusters[curpos] = new Cluster([new Position(i, j)], [], 0, board[i, j] - 1);
                            }
                            if (board[n.X, n.Y] == 0)
                            {
                                clusters[curpos].FreedomPositions.Add(n);
                                clusters[curpos].freedoms = clusters[curpos].FreedomPositions.Count;
                            }

                            if (board[i, j] == board[n.X, n.Y] && clusters.ContainsKey(n))
                            {
                                MergeClusters(clusters[curpos], clusters[n]);
                            }

                        }
                    }
                }
            }

            List<Cluster> clustersList = [];
            List<Position> traversed = [];

            foreach (var position in clusters.Keys)
            {
                var cluster = clusters[position];
                if (!traversed.Contains(position))
                {
                    traversed.AddRange(cluster.data);
                    clustersList.Add(cluster);
                }
            }


            // foreach (var cluster in clustersList)
            // {
            //     HashSet<Position> freedomPositions = [];
            //     foreach (var pos in cluster.data)
            //     {
            //         if ()


            //     }
            // }
            return clustersList;
        }

        public List<Stone> GetStones(List<Cluster> clusters)
        {
            List<Stone> stones = [];
            foreach (var cluster in clusters)
            {
                foreach (var position in cluster.data)
                {
                    stones.Add(new Stone(position, cluster.player, cluster));
                }
            }
            return stones;
        }

        public BoardState ConstructBoard(int rows, int cols, List<Stone> stones, Position? koDelete = null)
        {
            return new BoardState(
                rows: rows,
                cols: cols,
                koDelete,
                playgroundMap: stones.ToDictionary(e => e.position, e => e),
                prisoners: [0, 0]
                );
        }

        public HighLevelBoardRepresentation MakeHighLevelBoardRepresentationFromBoardState(BoardState boardState)
        {
            return boardState.playgroundMap.ToDictionary(e => e.Key, e => (StoneType)e.Value.player);
        }

        bool checkIfInsideBounds(Position pos)
        {
            return pos.X > -1 && pos.X < rows && pos.Y < cols && pos.Y > -1;
        }

    }

}
// 

