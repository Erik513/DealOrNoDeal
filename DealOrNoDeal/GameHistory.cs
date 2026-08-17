using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DealOrNoDeal
{
    /// <summary>
    /// Persists the last 20 finished games (when, how much, and how the
    /// game ended - "Deal" for an accepted banker offer, or a case number
    /// for keeping/swapping to the end) for the home screen's history
    /// table. Newest first, both in memory and on disk - simplest way to
    /// keep the two in sync without re-sorting on load.
    /// </summary>
    internal static class GameHistory
    {
        private const int MaxEntries = 20;
        private const string FileName = "history.txt";

        public static List<(DateTime Date, decimal Amount, string ResultLabel)> Entries { get; } =
            new List<(DateTime, decimal, string)>();

        public static void Load()
        {
            Entries.Clear();

            foreach (string line in AppDataStore.ReadLines(FileName))
            {
                string[] parts = line.Split('|');
                // Entries written before the result-label column existed
                // only have 2 parts - treat those as blank instead of
                // dropping them, so old history isn't lost.
                if (parts.Length != 2 && parts.Length != 3)
                    continue;

                if (DateTime.TryParse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime date) &&
                    decimal.TryParse(parts[1], NumberStyles.Number, CultureInfo.InvariantCulture, out decimal amount))
                {
                    string resultLabel = parts.Length == 3 ? parts[2] : "";
                    Entries.Add((date, amount, resultLabel));
                }
            }
        }

        public static void Record(decimal amount, string resultLabel)
        {
            Entries.Insert(0, (DateTime.Now, amount, resultLabel ?? ""));

            if (Entries.Count > MaxEntries)
                Entries.RemoveRange(MaxEntries, Entries.Count - MaxEntries);

            Save();
        }

        private static void Save()
        {
            IEnumerable<string> lines = Entries.Select(entry =>
                entry.Date.ToString("o", CultureInfo.InvariantCulture) + "|" +
                entry.Amount.ToString(CultureInfo.InvariantCulture) + "|" +
                entry.ResultLabel);

            AppDataStore.WriteLines(FileName, lines);
        }
    }
}
