using System;
using System.Collections.Generic;
using System.Linq;

namespace DealOrNoDeal
{
    /// <summary>
    /// Pure banker-offer math: which round (click count) triggers an offer,
    /// how much it's worth, how long the reveal takes to build suspense,
    /// and how many cases remain until the next one. No UI/game-state
    /// dependencies - DealOrNoDeal.cs supplies casesClicked and the
    /// still-unrevealed amounts and just reads back the results.
    /// </summary>
    internal static class BankerOfferCalculator
    {
        // Percentage of the average of still-unrevealed amounts the banker
        // offers in each round, keyed by the click count that triggers that
        // round's offer. Modeled on the real show: the offer starts well
        // below the fair average and climbs toward it as fewer cases (and
        // more information) remain, but stays just under 100% - the bank
        // always keeps a small edge. The last round is the last offer of
        // all, made right before the final keep-or-swap decision between
        // the player's own case and the one remaining case.
        private static readonly Dictionary<int, decimal> PercentageByRound = new Dictionary<int, decimal>
        {
            { 8, 0.10m },
            { 14, 0.15m },
            { 19, 0.25m },
            { 23, 0.40m },
            { 25, 0.55m },
            { 27, 0.75m },
            { 28, 0.90m },
            { 29, 0.95m }
        };

        // Suspense delay (same order as Rounds below) before the
        // accept/decline choice becomes usable - short for the first offer,
        // longer for later ones, since by then there's more at stake and
        // the moment deserves to breathe more. Skippable by clicking (see
        // ucBankerOffer.BeginRevealDelay), so this is a ceiling on the
        // wait, not a forced one.
        private static readonly int[] RevealDelaysMs = { 1500, 2250, 3000, 3750, 4500, 5250, 6000, 6000 };

        // Sorted click counts at which the banker makes an offer - single
        // source of truth for "which round is this" and "which round is
        // the last one", instead of retyping the same thresholds elsewhere.
        public static readonly IReadOnlyList<int> Rounds = PercentageByRound.Keys.OrderBy(round => round).ToList();

        public static int LastRound => Rounds[Rounds.Count - 1];

        public static bool TryGetOfferPercentage(int casesClicked, out decimal percentage)
        {
            return PercentageByRound.TryGetValue(casesClicked, out percentage);
        }

        public static int CalculateRevealDelayMs(int casesClicked)
        {
            int roundIndex = Rounds.ToList().IndexOf(casesClicked);
            return RevealDelaysMs[Math.Max(0, roundIndex)];
        }

        /// <summary>
        /// How many more cases need to be opened before the next banker
        /// offer - null on the very last offer, where declining leads
        /// straight to the final keep-or-swap decision instead of another
        /// round of case-opening.
        /// </summary>
        public static int? CalculateCasesUntilNextOffer(int casesClicked)
        {
            int nextRoundThreshold = Rounds
                .Where(round => round > casesClicked)
                .DefaultIfEmpty(-1)
                .Min();

            return nextRoundThreshold == -1 ? (int?)null : nextRoundThreshold - casesClicked;
        }

        /// <summary>
        /// The banker's offer for this round: the round's base percentage
        /// of the average remaining amount, dampened when those amounts are
        /// spread far apart (e.g. a very small and a very large one both
        /// still in play). A flat percentage-of-average would otherwise be
        /// unrealistically generous whenever one huge amount happens to
        /// still be live and everyone would just take the deal - in the
        /// real show the banker holds back more the riskier/more spread
        /// out the remaining pool is, and only approaches fair value once
        /// the remaining amounts are close together.
        /// </summary>
        public static decimal CalculateOffer(decimal baseOfferPercentage, IReadOnlyCollection<decimal> remainingValues)
        {
            if (remainingValues.Count == 0)
                return 0;

            decimal average = remainingValues.Average();

            if (average == 0)
                return 0;

            decimal variance = remainingValues.Average(value => (value - average) * (value - average));
            decimal coefficientOfVariation = (decimal)Math.Sqrt((double)variance) / average;

            // Normalize the spread into [0, 1] (capped at a CV of 2, which
            // is already an extreme spread) and dampen the base percentage
            // by at most half of it - so a high-risk round pays noticeably
            // less than the round's usual percentage, but never turns into
            // an obvious "always decline" trap either.
            decimal riskFactor = Math.Min(coefficientOfVariation, 2m) / 2m;
            decimal offerPercentage = baseOfferPercentage * (1m - riskFactor * 0.5m);
            decimal offer = average * offerPercentage;

            // Hard rule, enforced regardless of how the percentage table
            // above is tuned: the banker never offers more than the fair
            // average of what's still in play.
            return Math.Min(offer, average);
        }
    }
}
