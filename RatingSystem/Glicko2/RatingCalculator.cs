﻿using System;
using System.Collections.Generic;

namespace Glicko2
{
    public class RatingCalculator
    {
        private const double DefaultRating = 1500.0;
        private const double DefaultDeviation = 350;
        private const double DefaultVolatility = 0.06;
        private const double DefaultRatingPeriodsPerDay = 0.21436;
        private const double DefaultTau = 0.75;
        private const double Multiplier = 173.7178;
        private const double ConvergenceTolerance = 0.000001;

        private readonly double _tau; // constrains volatility over time
        private readonly double _defaultVolatility;
        private readonly double _ratingPeriodsPerDay;

        private readonly double DAYS_PER_MILLI = 1.0 / (1000 * 60 * 60 * 24);

        private double ratingPeriodsPerMilli => _ratingPeriodsPerDay * DAYS_PER_MILLI;

        /// <summary>
        /// Standard constructor, taking default values for volatility and tau.
        /// </summary>
        public RatingCalculator()
        {
            _tau = DefaultTau;
            _defaultVolatility = DefaultVolatility;
            _ratingPeriodsPerDay = DefaultRatingPeriodsPerDay;
        }

        /// <summary>
        /// Constructor allowing you to specify values for volatility and tau.
        /// </summary>
        /// <param name="initVolatility"></param>
        /// <param name="tau"></param>
        public RatingCalculator(double initVolatility, double tau, double ratingPeriodsPerDay)
        {
            _defaultVolatility = initVolatility;
            _tau = tau;
            _ratingPeriodsPerDay = ratingPeriodsPerDay;
        }


        /// <summary>
        /// Run through all players within a resultset and calculate their new ratings.
        /// 
        /// Players within the resultset who did not compete during the rating period
        /// will have see their deviation increase (in line with Prof Glickman's paper).
        /// 
        /// Note that this method will clear the results held in the association result set.
        /// </summary>
        /// <param name="results"></param>
        public void UpdateRatings(RatingPeriodResults results, bool skipDeviationIncrease)
        {
            int elapsedPeriods = skipDeviationIncrease ? 0 : 1;
            foreach (var player in results.GetParticipants())
            {
                if (results.GetResults(player).Count > 0)
                {
                    CalculateNewRating(player, results.GetResults(player), elapsedPeriods);
                }
                else
                {
                    // if a player does not compete during the rating period, then only Step 6 applies.
                    // the player's rating and volatility parameters remain the same but deviation increases
                    player.SetWorkingRating(player.GetGlicko2Rating());
                    player.SetWorkingRatingDeviation(CalculateNewRatingDeviation(player.GetGlicko2RatingDeviation(),
                        player.GetVolatility(), elapsedPeriods));
                    player.SetWorkingVolatility(player.GetVolatility());
                }
            }

            // now iterate through the participants and confirm their new ratings
            foreach (var player in results.GetParticipants())
            {
                player.FinaliseRating();
            }

            // lastly, clear the result set down in anticipation of the next rating period
            results.Clear();
        }

        /// <summary>
        /// This is the function processing described in step 5 of Glickman's paper.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="results"></param>
        public void CalculateNewRating(Rating player, IList<Result> results, int elapsedPeriods)
        {
            var phi = player.GetGlicko2RatingDeviation();
            var sigma = player.GetVolatility();

            // step 3 - estimate variance
            var v = V(player, results);

            // step 4 - estimate delta value
            var delta = Delta(player, results);

            // step 5.1 - set the initial values of the iterative algorithm
            var a = Math.Log(Math.Pow(sigma, 2));

            // step 5.2 - set the initial values of the iterative algorithm to come in step 5.4
            var A = a;
            double B;
            if (Math.Pow(delta, 2) > Math.Pow(phi, 2) + v)
            {
                B = Math.Log(Math.Pow(delta, 2) - Math.Pow(phi, 2) - v);
            }
            else
            {
                double k = 1;
                B = a - (k * Math.Abs(_tau));

                while (F(B, delta, phi, v, a, _tau) < 0)
                {
                    k++;
                    B = a - (k * Math.Abs(_tau));
                }
            }

            // step 5.3
            var fA = F(A, delta, phi, v, a, _tau);
            var fB = F(B, delta, phi, v, a, _tau);

            // step 5.4
            while (Math.Abs(B - A) > ConvergenceTolerance)
            {
                var C = A + (((A - B) * fA) / (fB - fA));
                var fC = F(C, delta, phi, v, a, _tau);

                if (fC * fB < 0)
                {
                    A = B;
                    fA = fB;
                }
                else
                {
                    fA = fA / 2.0;
                }

                B = C;
                fB = fC;
            }

            var newSigma = Math.Exp(A / 2.0);

            player.SetWorkingVolatility(newSigma);

            // Step 6
            var phiStar = CalculateNewRatingDeviation(phi, newSigma, elapsedPeriods);

            // Step 7
            var newPhi = 1.0 / Math.Sqrt((1.0 / Math.Pow(phiStar, 2)) + (1.0 / v));

            // note that the newly calculated rating values are stored in a "working" area in the Rating object
            // this avoids us attempting to calculate subsequent participants' ratings against a moving target
            player.SetWorkingRating(
                player.GetGlicko2Rating()
                + (Math.Pow(newPhi, 2) * OutcomeBasedRating(player, results)));
            player.SetWorkingRatingDeviation(newPhi);
            player.IncrementNumberOfResults(results.Count);
        }


        /// <summary>
        /// This is the formula defined in step 6. It is also used for players who have not competed during the
        /// rating period.
        /// </summary>
        /// <param name="player">The player's rating object.</param>
        /// <param name="ratingPeriodEndDate">The end date of the rating period.</param>
        /// <param name="reverse">Indicates whether to reverse the elapsed periods.</param>
        /// <returns>The new rating deviation.</returns>
        public double PreviewDeviation(Rating player, DateTime ratingPeriodEndDate, bool reverse)
        {
            double elapsedRatingPeriods = 0;

            DateTime? periodEnd = player.GetLastRatingPeriodEnd();

            if (periodEnd != null && ratingPeriodsPerMilli > 0)
            {
                var interval = ratingPeriodEndDate - (DateTime)periodEnd;
                elapsedRatingPeriods = interval.TotalMilliseconds * ratingPeriodsPerMilli;
            }

            if (reverse)
            {
                elapsedRatingPeriods = -elapsedRatingPeriods;
            }

            var newRD = CalculateNewRatingDeviation(player.GetGlicko2RatingDeviation(), player.GetVolatility(), elapsedRatingPeriods);
            return ConvertRatingDeviationToOriginalGlickoScale(newRD);
        }



        private static double F(double x, double delta, double phi, double v, double a, double tau)
        {
            return (Math.Exp(x) * (Math.Pow(delta, 2) - Math.Pow(phi, 2) - v - Math.Exp(x)) /
                    (2.0 * Math.Pow(Math.Pow(phi, 2) + v + Math.Exp(x), 2))) -
                   ((x - a) / Math.Pow(tau, 2));
        }

        /// <summary>
        /// This is the first sub-function of step 3 of Glickman's paper.
        /// </summary>
        /// <param name="deviation"></param>
        /// <returns></returns>
        private static double G(double deviation)
        {
            return 1.0 / (Math.Sqrt(1.0 + (3.0 * Math.Pow(deviation, 2) / Math.Pow(Math.PI, 2))));
        }

        /// <summary>
        /// This is the second sub-function of step 3 of Glickman's paper.
        /// </summary>
        /// <param name="playerRating"></param>
        /// <param name="opponentRating"></param>
        /// <param name="opponentDeviation"></param>
        /// <returns></returns>
        private static double E(double playerRating, double opponentRating, double opponentDeviation)
        {
            return 1.0 / (1.0 + Math.Exp(-1.0 * G(opponentDeviation) * (playerRating - opponentRating)));
        }

        /// <summary>
        /// This is the main function in step 3 of Glickman's paper.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="results"></param>
        /// <returns></returns>
        private static double V(Rating player, IEnumerable<Result> results)
        {
            var v = 0.0;

            foreach (var result in results)
            {
                v = v + (
                    (Math.Pow(G(result.GetOpponent(player).GetGlicko2RatingDeviation()), 2))
                    * E(player.GetGlicko2Rating(),
                        result.GetOpponent(player).GetGlicko2Rating(),
                        result.GetOpponent(player).GetGlicko2RatingDeviation())
                    * (1.0 - E(player.GetGlicko2Rating(),
                        result.GetOpponent(player).GetGlicko2Rating(),
                        result.GetOpponent(player).GetGlicko2RatingDeviation())
                        ));
            }

            return Math.Pow(v, -1);
        }

        /// <summary>
        /// This is a formula as per step 4 of Glickman's paper.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="results"></param>
        /// <returns></returns>
        private static double Delta(Rating player, IList<Result> results)
        {
            return V(player, results) * OutcomeBasedRating(player, results);
        }

        /// <summary>
        /// This is a formula as per step 4 of Glickman's paper.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="results"></param>
        /// <returns>Expected rating based on outcomes.</returns>
        private static double OutcomeBasedRating(Rating player, IEnumerable<Result> results)
        {
            double outcomeBasedRating = 0;

            foreach (var result in results)
            {
                outcomeBasedRating = outcomeBasedRating
                                     + (G(result.GetOpponent(player).GetGlicko2RatingDeviation())
                                        * (result.GetScore(player) - E(
                                            player.GetGlicko2Rating(),
                                            result.GetOpponent(player).GetGlicko2Rating(),
                                            result.GetOpponent(player).GetGlicko2RatingDeviation()))
                                         );
            }

            return outcomeBasedRating;
        }

        /// <summary>
        /// This is the formula defined in step 6. It is also used for players
	    /// who have not competed during the rating period.
        /// </summary>
        /// <param name="phi"></param>
        /// <param name="sigma"></param>
        /// <returns>New rating deviation.</returns>
        private static double CalculateNewRatingDeviation(double phi, double sigma, double elapsedPeriods)
        {
            return Math.Sqrt(Math.Pow(phi, 2) + elapsedPeriods * Math.Pow(sigma, 2));
        }

        /// <summary>
        /// Converts from the value used within the algorithm to a rating in
        /// the same range as traditional Elo et al
        /// </summary>
        /// <param name="rating">Rating in Glicko-2 scla.e</param>
        /// <returns>Rating in Glicko scale.</returns>
        public double ConvertRatingToOriginalGlickoScale(double rating)
        {
            return (rating * Multiplier) + DefaultRating;
        }

        /// <summary>
        /// Converts from a rating in the same range as traditional Elo
        /// et al to the value used within the algorithm.
        /// </summary>
        /// <param name="rating">Rating in Glicko scale.</param>
        /// <returns>Rating in Glicko-2 scale.</returns>
        public double ConvertRatingToGlicko2Scale(double rating)
        {
            return (rating - DefaultRating) / Multiplier;
        }

        /// <summary>
        /// Converts from the value used within the algorithm to a
        /// rating deviation in the same range as traditional Elo et al.
        /// </summary>
        /// <param name="ratingDeviation">Rating deviation </param>
        /// <returns></returns>
        public double ConvertRatingDeviationToOriginalGlickoScale(double ratingDeviation)
        {
            return ratingDeviation * Multiplier;
        }

        /// <summary>
        /// Converts from a rating deviation in the same range as traditional Elo et al
        /// to the value used within the algorithm.
        /// </summary>
        /// <param name="ratingDeviation">Rating deviation in Glicko scale.</param>
        /// <returns>Rating deviation in Glicko-2 scale.</returns>
        public double ConvertRatingDeviationToGlicko2Scale(double ratingDeviation)
        {
            return ratingDeviation / Multiplier;
        }

        public double GetDefaultRating()
        {
            return DefaultRating;
        }

        public double GetDefaultVolatility()
        {
            return _defaultVolatility;
        }

        public double GetDefaultRatingDeviation()
        {
            return DefaultDeviation;
        }
    }
}
