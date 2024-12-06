using System;

namespace Glicko2
{
    /// <summary>
    /// Represents the result of a match between two players.
    /// </summary>
    public class Result
    {
        private const double PointsForWin = 1.0;
        private const double PointsForLoss = 0.0;
        private const double PointsForDraw = 0.5;

        private readonly bool _isDraw;

        private readonly Rating _p1;
        private readonly Rating _p2;

        /// <summary>
        /// Record a new result from a match between two players.
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="isDraw"></param>
        public Result(Rating p1, Rating p2, bool isDraw)
        {
            if (!ValidPlayers(p1, p2))
            {
                throw new ArgumentException("Players winner and loser are the same player");
            }

            _p1 = p1;
            _p2 = p2;
            _isDraw = isDraw;
        }

        /// <summary>
        /// Check that we're not doing anything silly like recording a match with only one player.
        /// </summary>
        /// <param name="player1"></param>
        /// <param name="player2"></param>
        /// <returns></returns>
        private static bool ValidPlayers(Rating player1, Rating player2)
        {
            return player1 != player2;
        }

        /// <summary>
        /// Test whether a particular player participated in the match represented by this result.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public bool Participated(Rating player)
        {
            return player == _p1 || player == _p2;
        }

        /// <summary>
        /// Returns the "score" for a match.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public double GetScore(Rating player)
        {
            double score;

            if (_p1 == player)
            {
                score = PointsForWin;
            }
            else if (_p2 == player)
            {
                score = PointsForLoss;
            }
            else
            {
                throw new ArgumentException("Player did not participate in match", "player");
            }

            return score;
        }

        /// <summary>
        /// Given a particular player, returns the opponent.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public Rating GetOpponent(Rating player)
        {
            Rating opponent;

            if (_p1 == player)
            {
                opponent = _p2;
            }
            else if (_p2 == player)
            {
                opponent = _p1;
            }
            else
            {
                throw new ArgumentException("Player did not participate in match", "player");
            }

            return opponent;
        }

        public Rating GetPlayer1()
        {
            return _p1;
        }

        public Rating GetPlayer2()
        {
            return _p2;
        }
    }
}
