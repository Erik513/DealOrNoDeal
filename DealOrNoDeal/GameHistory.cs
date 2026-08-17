using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace DealOrNoDeal
{
    /// <summary>
    /// Persists the last 20 finished games (when, how much) for the home
    /// screen's history table. Newest first, both in memory and on disk -
    /// simplest way to keep the two in sync without re-sorting on load.
    /// </summary>
    internal static class GameHistory
    {
        private const int MaxEntries = 20;

        private static readonly string FilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DealOrNoDeal",
            "history.txt");

        public static List<(DateTime Date, decimal Amount)> Entries { get; } = new List<(DateTime, decimal)>();

        public static void Load()
        {
            Entries.Clear();

            try
            {
                if (!File.Exists(FilePath))
                    return;

                foreach (string line in File.ReadAllLines(FilePath))
                {
                    string[] parts = line.Split('|');
                    if (parts.Length != 2)
                        continue;

                    if (DateTime.TryParse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime date) &&
                        decimal.TryParse(parts[1], NumberStyles.Number, CultureInfo.InvariantCulture, out decimal amount))
                    {
                        Entries.Add((date, amount));
                    }
                }
            }
            catch
            {
                // Fall back to an empty history rather than crash startup
                // over a corrupted or inaccessible file.
            }
        }

        public static void Record(decimal amount)
        {
            Entries.Insert(0, (DateTime.Now, amount));

            if (Entries.Count > MaxEntries)
                Entries.RemoveRange(MaxEntries, Entries.Count - MaxEntries);

            Save();
        }

        private static void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));

                IEnumerable<string> lines = Entries.Select(entry =>
                    entry.Date.ToString("o", CultureInfo.InvariantCulture) + "|" + entry.Amount.ToString(CultureInfo.InvariantCulture));

                File.WriteAllLines(FilePath, lines);
            }
            catch
            {
                // Best-effort - if saving fails (e.g. no write access), this
                // round just won't show up in history on the next launch.
            }
        }
    }
}
