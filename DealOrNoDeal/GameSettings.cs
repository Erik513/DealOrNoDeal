using System;
using System.IO;
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
        private static readonly string FilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DealOrNoDeal",
            "settings.txt");

        /// <summary>
        /// Loads saved settings into AppLocalization/AppCurrencyFormatter.
        /// Call once at startup, before building any UI. Missing or
        /// corrupted settings just fall back to the defaults (English/Euro)
        /// instead of failing startup.
        /// </summary>
        public static void Load()
        {
            try
            {
                if (!File.Exists(FilePath))
                    return;

                foreach (string line in File.ReadAllLines(FilePath))
                {
                    string[] parts = line.Split(new[] { '=' }, 2);
                    if (parts.Length != 2)
                        continue;

                    string key = parts[0].Trim();
                    string value = parts[1].Trim();

                    AppLanguage language;
                    AppCurrency currency;

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
                }
            }
            catch
            {
                // Fall back to defaults rather than crash startup over a
                // corrupted or inaccessible settings file.
            }
        }

        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                File.WriteAllLines(FilePath, new[]
                {
                    "Language=" + AppLocalization.Language,
                    "Currency=" + AppCurrencyFormatter.Currency
                });
            }
            catch
            {
                // Best-effort - if saving fails (e.g. no write access), the
                // chosen options just won't persist to the next launch.
            }
        }
    }
}
