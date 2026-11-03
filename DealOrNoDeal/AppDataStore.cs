using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        /// <summary>
        /// The folder the files live in - settable so that tests can point
        /// it at a temporary folder instead of the player's real data.
        /// </summary>
        internal static string AppFolder { get; set; } = Path.Combine(
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
        /// caller's data just won't persist to the next launch; the failure
        /// is traced and returned as false, not thrown. The text goes to a
        /// temporary file first and replaces the real one only once it is
        /// complete, so a crash or a full disk in the middle of writing
        /// cannot leave the player's save half empty.
        /// </summary>
        public static bool WriteLines(string fileName, IEnumerable<string> lines)
        {
            string filePath = Path.Combine(AppFolder, fileName);
            string temporaryPath = filePath + ".tmp";

            try
            {
                Directory.CreateDirectory(AppFolder);
                File.WriteAllLines(temporaryPath, lines);

                if (File.Exists(filePath))
                    File.Replace(temporaryPath, filePath, null);
                else
                    File.Move(temporaryPath, filePath);

                return true;
            }
            catch (Exception exception)
            {
                Trace.TraceError("Saving " + filePath + " failed: " + exception.Message);

                try
                {
                    File.Delete(temporaryPath);
                }
                catch (Exception)
                {
                }

                return false;
            }
        }
    }
}
