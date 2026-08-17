using System;
using System.Collections.Generic;
using System.IO;

namespace DealOrNoDeal
{
    /// <summary>
    /// Shared "read/write a plain text file under this app's AppData
    /// folder" skeleton - GameHistory and GameSettings both persist as
    /// simple line-based text files, differing only in their own line
    /// format and parsing, not in how the file itself gets found/read/
    /// written/protected against a missing or inaccessible disk.
    /// </summary>
    internal static class AppDataStore
    {
        private static readonly string AppFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DealOrNoDeal");

        /// <summary>
        /// Empty array if the file doesn't exist or can't be read (e.g.
        /// no access) - callers fall back to their own defaults rather
        /// than crash startup over a missing/corrupted file.
        /// </summary>
        public static string[] ReadLines(string fileName)
        {
            try
            {
                string filePath = Path.Combine(AppFolder, fileName);

                if (!File.Exists(filePath))
                    return Array.Empty<string>();

                return File.ReadAllLines(filePath);
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// Best-effort - if saving fails (e.g. no write access), the
        /// caller's data just won't persist to the next launch.
        /// </summary>
        public static void WriteLines(string fileName, IEnumerable<string> lines)
        {
            try
            {
                Directory.CreateDirectory(AppFolder);
                File.WriteAllLines(Path.Combine(AppFolder, fileName), lines);
            }
            catch
            {
            }
        }
    }
}
