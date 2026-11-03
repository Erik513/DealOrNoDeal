using System.Globalization;

namespace DealOrNoDeal.Tests;

/// <summary>
/// GameHistory and GameSettings are static and write to a folder: one test
/// class (so xUnit runs it sequentially), pointed at a temporary folder so
/// the real save data is never touched.
/// </summary>
public sealed class PersistenceTests : IDisposable
{
    private readonly string folder = Path.Combine(Path.GetTempPath(), "DealOrNoDeal.Tests." + Guid.NewGuid().ToString("N"));
    private readonly string previousFolder = AppDataStore.AppFolder;

    public PersistenceTests()
    {
        AppDataStore.AppFolder = folder;
        GameHistory.Entries.Clear();
        GameSettings.HighestAmount = null;
        GameSettings.HighestAmountDate = null;
        GameSettings.HighestAmountResultLabel = null;
    }

    public void Dispose()
    {
        AppDataStore.AppFolder = previousFolder;

        if (Directory.Exists(folder))
            Directory.Delete(folder, true);
    }

    private string HistoryFile => Path.Combine(folder, "history.txt");

    [Fact]
    public void MissingFiles_GiveEmptyData()
    {
        GameHistory.Load();

        Assert.Empty(GameHistory.Entries);
        Assert.Empty(AppDataStore.ReadLines("nothing.txt"));
    }

    [Fact]
    public void History_PutsTheNewestFirst_AndSurvivesALoad()
    {
        GameHistory.Record(100m, "7");
        GameHistory.Record(2500.5m, "Deal");

        GameHistory.Load();

        Assert.Equal(2, GameHistory.Entries.Count);
        Assert.Equal(("Deal", 2500.5m), (GameHistory.Entries[0].ResultLabel, GameHistory.Entries[0].Amount));
        Assert.Equal(("7", 100m), (GameHistory.Entries[1].ResultLabel, GameHistory.Entries[1].Amount));
    }

    [Fact]
    public void History_KeepsOnlyTheLastTwentyGames()
    {
        for (int i = 1; i <= 25; i++)
            GameHistory.Record(i, i.ToString(CultureInfo.InvariantCulture));

        GameHistory.Load();

        Assert.Equal(20, GameHistory.Entries.Count);
        Assert.Equal(25m, GameHistory.Entries[0].Amount);
        Assert.Equal(6m, GameHistory.Entries[19].Amount);
    }

    [Fact]
    public void History_ReadsOldTwoPartLines_AndSkipsBrokenOnes()
    {
        Directory.CreateDirectory(folder);
        File.WriteAllLines(HistoryFile, new[]
        {
            "2026-08-17T10:00:00.0000000+02:00|750",
            "garbage",
            "2026-08-17T11:00:00.0000000+02:00|abc|Deal",
            "not a date|5|3",
            "2026-08-17T12:00:00.0000000+02:00|1000000|12",
        });

        GameHistory.Load();

        Assert.Equal(2, GameHistory.Entries.Count);
        Assert.Equal(("", 750m), (GameHistory.Entries[0].ResultLabel, GameHistory.Entries[0].Amount));
        Assert.Equal(("12", 1000000m), (GameHistory.Entries[1].ResultLabel, GameHistory.Entries[1].Amount));
    }

    [Fact]
    public void Settings_RoundTripTheAllTimeBest()
    {
        DateTime when = new DateTime(2026, 8, 15, 14, 34, 22, DateTimeKind.Local);
        GameSettings.HighestAmount = 500000m;
        GameSettings.HighestAmountDate = when;
        GameSettings.HighestAmountResultLabel = "19";
        GameSettings.Save();

        GameSettings.HighestAmount = null;
        GameSettings.HighestAmountDate = null;
        GameSettings.HighestAmountResultLabel = null;
        GameSettings.Load();

        Assert.Equal(500000m, GameSettings.HighestAmount);
        Assert.Equal(when, GameSettings.HighestAmountDate);
        Assert.Equal("19", GameSettings.HighestAmountResultLabel);
    }

    [Fact]
    public void Settings_LeaveTheBestOut_UntilAGameWasWon()
    {
        GameSettings.Save();

        string text = File.ReadAllText(Path.Combine(folder, "settings.txt"));

        Assert.DoesNotContain("HighestAmount", text);
    }

    [Fact]
    public void Settings_IgnoreUnparsableValues()
    {
        Directory.CreateDirectory(folder);
        File.WriteAllLines(Path.Combine(folder, "settings.txt"), new[]
        {
            "HighestAmount=lots",
            "HighestAmountDate=yesterday",
            "no equals sign here",
        });

        GameSettings.Load();

        Assert.Null(GameSettings.HighestAmount);
        Assert.Null(GameSettings.HighestAmountDate);
    }
}
