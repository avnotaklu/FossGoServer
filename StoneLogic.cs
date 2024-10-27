using System.Diagnostics;
using ZstdSharp.Unsafe;

using HighLevelBoardRepresentation = System.Collections.Generic.Dictionary<string, BadukServer.StoneType>;

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
    public class Position
    {
        public int x;
        public int y;

        public Position(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public Position(string repr)
        {
            var x = repr.Split(" ")[0];
            var y = repr.Split(" ")[1];

            this.x = int.Parse(x);
            this.y = int.Parse(y);
        }


        public override bool Equals(object? obj)
        {
            if (obj is Position pos)
            {
                return x == pos.x && y == pos.y;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return x ^ y;
        }

        public override string ToString()
        {
            return $"{x}, {y}";
        }

        public string ToHighLevelRepr()
        {
            return $"{x} {y}";
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
            return pos.x > -1 && pos.x < rows && pos.y < cols && pos.y > -1;
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

        static void DoActionOnNeighbors(Position thisCell,
            Action<Position, Position> doAction)
        {
            var rowPlusOne = new Position(thisCell.x + 1, thisCell.y);
            doAction(thisCell, rowPlusOne);
            var rowMinusOne = new Position(thisCell.x - 1, thisCell.y);
            doAction(thisCell, rowMinusOne);
            var colPlusOne = new Position(thisCell.x, thisCell.y + 1);
            doAction(thisCell, colPlusOne);
            var colMinusOne = new Position(thisCell.x, thisCell.y - 1);
            doAction(thisCell, colMinusOne);
        }

        // Scoring

        // Extend outward by checking all neighbors approach
    }

    // class Area
    // {
    //     Set<Position?> spaces = { };
    //     // int value;
    //     int get value => spaces.length;
    //   int? owner;
    //     bool isDame;
    //     Area.from(this.isDame, this.owner);
    //     Area()
    //       : isDame = false,
    //         owner = null;
    // }

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
        public BoardState BoardStateFromHighLevelBoardRepresentation(Dictionary<string, StoneType> map)
        {
            var simpleB = SimpleBoardRepresentation(map);
            var clusters = GetClusters(simpleB);
            var stones = GetStones(clusters);
            var board = ConstructBoard(rows, cols, stones);
            return board;
        }
        public int[,] SimpleBoardRepresentation(Dictionary<string, StoneType> map)
        {
            var board = new int[rows, cols];

            foreach (var item in map)
            {
                var position = new Position(item.Key);
                board[position.x, position.y] = (int)item.Value + 1;
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
                            if (board[n.x, n.y] == 0)
                            {
                                clusters[curpos].FreedomPositions.Add(n);
                                clusters[curpos].freedoms = clusters[curpos].FreedomPositions.Count;
                            }

                            if (board[i, j] == board[n.x, n.y] && clusters.ContainsKey(n))
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

        public BoardState ConstructBoard(int rows, int cols, List<Stone> stones)
        {
            return new BoardState(
                rows: rows,
                cols: cols,
                null,
                playgroundMap: stones.ToDictionary(e => e.position, e => e),
                prisoners: [0, 0]
                );
        }

        public HighLevelBoardRepresentation MakeHighLevelBoardRepresentationFromBoardState(BoardState boardState)
        {
            return boardState.playgroundMap.ToDictionary(e => e.Key.ToHighLevelRepr(), e => (StoneType)e.Value.player);
        }

        bool checkIfInsideBounds(Position pos)
        {
            return pos.x > -1 && pos.x < rows && pos.y < cols && pos.y > -1;
        }

    }

}
// 

