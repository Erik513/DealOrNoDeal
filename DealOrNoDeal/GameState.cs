using System;
using System.Collections.Generic;
using System.Linq;

namespace DealOrNoDeal
{
    /// <summary>
    /// How a finished game ended: what was won (a raw value, so it can be
    /// re-formatted when the currency changes), the format it is shown
    /// with, the history label ("Deal" or a case number) and whether the
    /// player kept their own case.
    /// </summary>
    internal sealed class GameResult
    {
        public GameResult(decimal value, string format, string label, bool keptOwnCase)
        {
            Value = value;
            Format = format;
            Label = label;
            KeptOwnCase = keptOwnCase;
        }

        public decimal Value { get; }

        /// <summary>Case amounts use "#,0", banker offers (computed percentages) "#,0.00".</summary>
        public string Format { get; }

        public string Label { get; }

        /// <summary>
        /// True only when the game ended by keeping the original case - its
        /// content IS the winnings then, so the game-over screen must not
        /// repeat it as "your own case contained...".
        /// </summary>
        public bool KeptOwnCase { get; }
    }

    internal enum CasePick
    {
        /// <summary>The first click: the player's own case.</summary>
        OwnCase,

        /// <summary>Any later click: a case that gets opened.</summary>
        Opened
    }

    /// <summary>
    /// The rules and the state of one game, without any UI: which case
    /// holds which amount, which cases are open, how many clicks were
    /// made, when the banker offers and what he offers. Cases are numbered
    /// 1-30, as printed on the case artwork.
    /// </summary>
    internal sealed class GameState
    {
        public const int CaseCount = 30;

        // Money amounts for the 30 cases, in case-1..30 order. Plain
        // numbers, not pre-formatted strings - display text (with the
        // currently selected currency's symbol/format) is generated on
        // demand via AppCurrencyFormatter, so the currency can be switched
        // without touching this list.
        public static readonly decimal[] CaseAmountValues =
        {
            0, 1, 5, 10, 20, 50, 75, 100, 200, 300,
            400, 500, 750, 1000, 2500, 5000, 7500, 10000, 15000, 20000,
            25000, 50000, 75000, 100000, 200000, 300000, 400000, 500000, 750000, 1000000
        };

        // How many cases are left to open once the info text is next shown,
        // keyed by the click count it applies from. Round thresholds match
        // BankerOfferCalculator's offer rounds (plus click 1, the very
        // first case pick, which has no banker offer of its own) - but the
        // remaining-count values themselves are a separate, hand-tuned
        // case-opening schedule with no mathematical link to the offer
        // percentages, so they can't be derived from that table.
        private static readonly Dictionary<int, int> CasesRemainingByRound = new Dictionary<int, int>
        {
            { 1, 7 },
            { 8, 6 },
            { 14, 5 },
            { 19, 4 },
            { 23, 2 },
            { 25, 2 },
            { 27, 1 },
            { 28, 1 },
            { 29, 0 }
        };

        private readonly Random random;

        // Single source of truth for which case holds which amount (index
        // into CaseAmountValues), by case number - 1.
        private readonly int[] amountIndexByCase = new int[CaseCount];

        // Indices of the amounts that haven't been revealed yet - basis for
        // the banker's offer average calculation.
        private readonly HashSet<int> remainingAmountIndices = new HashSet<int>();

        private readonly HashSet<int> openedCases = new HashSet<int>();

        private int casesRemaining;

        public GameState(Random random)
        {
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            Reset();
        }

        /// <summary>Clicks so far, the pick of the own case included.</summary>
        public int CasesClicked { get; private set; }

        /// <summary>Number of the player's own case, null until it was picked.</summary>
        public int? OwnCase { get; private set; }

        public bool IsOver { get; private set; }

        public bool IsLastOfferRound => CasesClicked == BankerOfferCalculator.LastRound;

        /// <summary>
        /// Starts a new game: forgets everything and distributes the 30
        /// amounts randomly across the 30 cases - the only place that rolls
        /// the dice.
        /// </summary>
        public void Reset()
        {
            CasesClicked = 0;
            casesRemaining = 0;
            OwnCase = null;
            IsOver = false;

            openedCases.Clear();
            remainingAmountIndices.Clear();

            List<int> availableIndices = Enumerable.Range(0, CaseAmountValues.Length).ToList();

            for (int i = 0; i < CaseCount; i++)
            {
                int randomPosition = random.Next(availableIndices.Count);
                int amountIndex = availableIndices[randomPosition];
                availableIndices.RemoveAt(randomPosition);

                amountIndexByCase[i] = amountIndex;
                remainingAmountIndices.Add(amountIndex);
            }
        }

        /// <summary>Index into CaseAmountValues of the amount in the given case.</summary>
        public int AmountIndexOf(int caseNumber)
        {
            return amountIndexByCase[ValidIndex(caseNumber)];
        }

        public decimal AmountOf(int caseNumber)
        {
            return CaseAmountValues[AmountIndexOf(caseNumber)];
        }

        /// <summary>
        /// A click on a case: the first one picks the player's own case,
        /// every later one opens that case and takes its amount out of the
        /// game.
        /// </summary>
        public CasePick Pick(int caseNumber)
        {
            int index = ValidIndex(caseNumber);

            if (IsOver)
                throw new InvalidOperationException("The game is over.");

            if (caseNumber == OwnCase || openedCases.Contains(caseNumber))
                throw new InvalidOperationException("Case " + caseNumber + " is not available.");

            CasesClicked++;

            if (OwnCase == null)
            {
                OwnCase = caseNumber;
                return CasePick.OwnCase;
            }

            openedCases.Add(caseNumber);
            remainingAmountIndices.Remove(amountIndexByCase[index]);
            return CasePick.Opened;
        }

        /// <summary>
        /// The "open N more cases until the banker's offer" number for the
        /// current click count. Remembers the countdown between clicks that
        /// have no entry of their own.
        /// </summary>
        public int NextCasesToOpen()
        {
            if (CasesRemainingByRound.TryGetValue(CasesClicked, out int scheduled))
                casesRemaining = scheduled;

            return casesRemaining--;
        }

        public int? CasesUntilNextOffer => BankerOfferCalculator.CalculateCasesUntilNextOffer(CasesClicked);

        public int RevealDelayMs => BankerOfferCalculator.CalculateRevealDelayMs(CasesClicked);

        /// <summary>
        /// The banker's offer after this click, or null when this click
        /// does not trigger one.
        /// </summary>
        public decimal? TryMakeOffer()
        {
            if (!BankerOfferCalculator.TryGetOfferPercentage(CasesClicked, out decimal baseOfferPercentage))
                return null;

            decimal[] remainingValues = remainingAmountIndices.Select(index => CaseAmountValues[index]).ToArray();
            return BankerOfferCalculator.CalculateOffer(baseOfferPercentage, remainingValues, random);
        }

        /// <summary>
        /// The one case that is neither the player's own nor opened yet -
        /// what "swap" gives; null while more than one is left.
        /// </summary>
        public int? LastRemainingCase
        {
            get
            {
                if (OwnCase == null)
                    return null;

                int[] left = Enumerable.Range(1, CaseCount)
                    .Where(number => number != OwnCase && !openedCases.Contains(number))
                    .ToArray();

                return left.Length == 1 ? left[0] : (int?)null;
            }
        }

        /// <summary>Ends the game; false if it was already over.</summary>
        public bool TryFinish()
        {
            if (IsOver || OwnCase == null)
                return false;

            IsOver = true;
            return true;
        }

        private static int ValidIndex(int caseNumber)
        {
            if (caseNumber < 1 || caseNumber > CaseCount)
                throw new ArgumentOutOfRangeException(nameof(caseNumber));

            return caseNumber - 1;
        }
    }
}
