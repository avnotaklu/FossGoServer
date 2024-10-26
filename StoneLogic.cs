using System.Diagnostics;
using Tests;

namespace BadukServer
{
    public class Cluster
    {
        public HashSet<Position> data;
        public int freedoms;
        public int player;

        public Cluster(HashSet<Position> data, int freedoms, int player)
        {
            this.data = data;
            this.freedoms = freedoms;
            this.player = player;
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
    }
    public class BoardState
    {
        public int rows;
        public int cols;
        public Position? koDelete;
        public List<int> prisoners = [0, 0];
        public Dictionary<Position, Stone?> playgroundMap;

        public BoardState(int rows, int cols, Position? koDelete, Dictionary<Position, Stone?> playgroundMap, List<int> prisoners)
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
            // if (pos == null)
            // {
            //     return null;
            // }
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
                if (neighborCluster != null && curPosCluster != null && (traversed[curpos]?.Contains(neighborCluster) ?? false) == false)
                {
                    // if( _playgroundMap[neighbor] == null && checkOutofBounds(neighbor))
                    // {
                    //   _playgroundMap[curpos]?.cluster.freedoms += 1;
                    // }
                    if (stoneAt(neighbor)!.player !=
                        stoneAt(curpos)!.player)
                    {
                        neighborCluster.freedoms -= 1;

                        if (traversed[curpos] == null)
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
            if (board.koDelete == position)
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
                            if (stoneAt(neighbor)?.player ==
                            stoneAt(curpos)?.player)
                                insertable =
                                    !(getClusterFromPosition(neighbor)?.freedoms == 1);
                            else
                                insertable =
                                    getClusterFromPosition(neighbor)?.freedoms == 1;
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
                    ((traversed[neighbor]?.Contains(getClusterFromPosition(curpos)!) ??
                            false) ==
                        false))
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

        bool handleStoneUpdate(Position? position, int player)
        {
            if (position == null)
            {
                return true;
            }
            Position? thisCurrentCell = position;

            var current_cluster = new Cluster([position], 0, player);

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
                return true;
            }

            board.playgroundMap.Remove(thisCurrentCell);
            return false;
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

}
