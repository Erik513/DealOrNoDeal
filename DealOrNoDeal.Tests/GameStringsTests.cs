using System.Text.RegularExpressions;

namespace DealOrNoDeal.Tests;

public class GameStringsTests
{
    [Fact]
    public void EnglishAndGerman_HaveTheSameKeys()
    {
        Assert.Empty(GameStrings.English.Keys.Except(GameStrings.German.Keys));
        Assert.Empty(GameStrings.German.Keys.Except(GameStrings.English.Keys));
    }

    [Fact]
    public void EveryKey_HasTheSamePlaceholdersInBothLanguages()
    {
        foreach (string key in GameStrings.English.Keys)
        {
            Assert.True(
                Placeholders(GameStrings.English[key]).SequenceEqual(Placeholders(GameStrings.German[key])),
                "Placeholders differ for " + key);
        }
    }

    [Fact]
    public void NoText_IsEmpty()
    {
        Assert.All(
            GameStrings.English.Concat(GameStrings.German),
            pair => Assert.False(string.IsNullOrWhiteSpace(pair.Value), pair.Key));
    }

    [Fact]
    public void EverySingularText_HasAPluralCounterpart()
    {
        Assert.True(GameStrings.English.ContainsKey("BankerOffer.CasesUntilNext"));
        Assert.True(GameStrings.English.ContainsKey("BankerOffer.CaseUntilNextSingular"));
        Assert.True(GameStrings.English.ContainsKey("Game.OpenMoreCases"));
        Assert.True(GameStrings.English.ContainsKey("Game.OpenMoreCaseSingular"));
    }

    [Fact]
    public void TheEnglishSingular_UsesTheWordCase_NotCases()
    {
        Assert.Equal("(1 case until the next offer)", string.Format(GameStrings.English["BankerOffer.CaseUntilNextSingular"], 1));
        Assert.Equal("Open 1 more case until the banker's offer!", string.Format(GameStrings.English["Game.OpenMoreCaseSingular"], 1));
    }

    private static string[] Placeholders(string text) =>
        Regex.Matches(text, @"\{\d+\}").Select(m => m.Value).OrderBy(v => v).ToArray();
}
