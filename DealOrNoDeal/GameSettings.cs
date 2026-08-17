using System;
using System.Collections.Generic;
using System.Globalization;
using CustomWFUI;
using CustomWFUI.Styles;

namespace DealOrNoDeal
{
    /// <summary>
    /// Persists the chosen language/currency between launches as a plain
    /// key=value text file - no JSON/serialization library needed for two
    /// enum values, and it stays human-readable/editable.
    /// </summary>
    internal static class GameSettings
    {
        private const string FileName = "settings.txt";

        // The biggest amount ever won by any means (accepting an offer,
        // keeping, or swapping) - shown on the home screen as the
        // all-time-best history entry. Nullable, null until a first real
        // game finishes - 0 is itself a perfectly plausible result, so it
        // can't double as "nothing recorded yet" the way null can.
        public static decimal? HighestAmount { get; set; }

        // When HighestAmount was achieved - shown on the home screen
        // instead of a generic label, so the all-time-best row reads like
        // just another history entry.
        public static DateTime? HighestAmountDate { get; set; }

        // How that game ended - "Deal" for an accepted banker offer, or a
        // case number for keeping/swapping to the end. Same value that
        // would appear in that game's history row, kept alongside the
        // record since history itself only holds the last 20 games.
        public static string HighestAmountResultLabel { get; set; }

        /// <summary>
        /// Loads saved settings into AppLocalization/AppCurrencyFormatter.
        /// Call once at startup, before building any UI. Missing or
        /// corrupted settings just fall back to the defaults (English/Euro)
        /// instead of failing startup.
        /// </summary>
        public static void Load()
        {
            foreach (string line in AppDataStore.ReadLines(FileName))
            {
                string[] parts = line.Split(new[] { '=' }, 2);
                if (parts.Length != 2)
                    continue;

                string key = parts[0].Trim();
                string value = parts[1].Trim();

                AppLanguage language;
                AppCurrency currency;
                decimal amount;
                DateTime date;

                if (key == "Language" && Enum.TryParse(value, out language))
                {
                    AppLocalization.Language = language;
                    // Also drives CustomWFUI's own built-in text (e.g.
                    // the title bar's minimize/maximize/close
                    // tooltips), which is a separate setting from
                    // AppLocalization.
                    UIStyles.Language = language == AppLanguage.German
                        ? UILanguage.German
                        : UILanguage.English;
                }
                else if (key == "Currency" && Enum.TryParse(value, out currency))
                    AppCurrencyFormatter.Currency = currency;
                else if (key == "HighestAmount" && decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out amount))
                    HighestAmount = amount;
                else if (key == "HighestAmountDate" && DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out date))
                    HighestAmountDate = date;
                else if (key == "HighestAmountResultLabel")
                    HighestAmountResultLabel = value;
            }
        }

        public static void Save()
        {
            var lines = new List<string>
            {
                "Language=" + AppLocalization.Language,
                "Currency=" + AppCurrencyFormatter.Currency
            };

            // Only written once a real value exists - its absence is
            // exactly what Load() reads back as "nothing recorded yet".
            AddIfPresent(lines, "HighestAmount", HighestAmount);
            AddIfPresent(lines, "HighestAmountDate", HighestAmountDate);
            AddIfPresent(lines, "HighestAmountResultLabel", HighestAmountResultLabel);

            AppDataStore.WriteLines(FileName, lines);
        }

        private static void AddIfPresent(List<string> lines, string key, decimal? value)
        {
            if (value.HasValue)
                lines.Add(key + "=" + value.Value.ToString(CultureInfo.InvariantCulture));
        }

        private static void AddIfPresent(List<string> lines, string key, DateTime? value)
        {
            if (value.HasValue)
                lines.Add(key + "=" + value.Value.ToString("o", CultureInfo.InvariantCulture));
        }

        private static void AddIfPresent(List<string> lines, string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
                lines.Add(key + "=" + value);
        }
    }
}
