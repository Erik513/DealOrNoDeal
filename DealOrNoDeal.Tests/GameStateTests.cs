namespace DealOrNoDeal.Tests;

public class GameStateTests
{
    private static GameState NewGame(int seed = 1) => new GameState(new Random(seed));

    /// <summary>Picks case 1 as the own case, then opens 2, 3, ... until the given click count.</summary>
    private static void PlayUntilClick(GameState game, int clicks)
    {
        for (int number = 1; game.CasesClicked < clicks; number++)
            game.Pick(number);
    }

    [Fact]
    public void NewGame_HoldsEveryAmountExactlyOnce()
    {
        GameState game = NewGame();

        decimal[] amounts = Enumerable.Range(1, GameState.CaseCount).Select(game.AmountOf).OrderBy(a => a).ToArray();

        Assert.Equal(GameState.CaseAmountValues.OrderBy(a => a), amounts);
    }

    [Fact]
    public void SameSeed_GivesTheSameLayout_OtherSeedsDiffer()
    {
        decimal[] Layout(GameState game) => Enumerable.Range(1, GameState.CaseCount).Select(game.AmountOf).ToArray();

        Assert.Equal(Layout(NewGame(5)), Layout(NewGame(5)));
        Assert.NotEqual(Layout(NewGame(5)), Layout(NewGame(6)));
    }

    [Fact]
    public void FirstPickIsTheOwnCase_TheNextOnesAreOpened()
    {
        GameState game = NewGame();

        Assert.Equal(CasePick.OwnCase, game.Pick(7));
        Assert.Equal(7, game.OwnCase);
        Assert.Equal(CasePick.Opened, game.Pick(1));
        Assert.Equal(2, game.CasesClicked);
    }

    [Fact]
    public void PickingTheOwnOrAnOpenedCaseAgain_Throws()
    {
        GameState game = NewGame();
        game.Pick(3);
        game.Pick(4);

        Assert.Throws<InvalidOperationException>(() => game.Pick(3));
        Assert.Throws<InvalidOperationException>(() => game.Pick(4));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(31)]
    public void AnInvalidCaseNumber_Throws(int number)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => NewGame().Pick(number));
    }

    [Fact]
    public void NoOffer_BeforeTheFirstRound()
    {
        GameState game = NewGame();
        PlayUntilClick(game, 7);

        Assert.Null(game.TryMakeOffer());
    }

    [Fact]
    public void AnOfferComesExactlyOnTheOfferRounds()
    {
        GameState game = NewGame(3);
        var offerClicks = new List<int>();

        for (int number = 1; game.CasesClicked < BankerOfferCalculator.LastRound; number++)
        {
            game.Pick(number);
            decimal? offer = game.TryMakeOffer();

            if (offer.HasValue)
            {
                offerClicks.Add(game.CasesClicked);
                Assert.True(offer.Value >= 0m);
            }
        }

        Assert.Equal(BankerOfferCalculator.Rounds, offerClicks);
    }

    [Fact]
    public void TheOfferIsBuiltFromTheAmountsThatAreStillInPlay()
    {
        GameState game = NewGame(9);
        game.Pick(1);

        // Open the cases holding the 22 smallest of the other amounts: what
        // is left is large, so the offer in round 23 must be far above what
        // a pool of small amounts would give.
        foreach (int number in Enumerable.Range(2, 29).OrderBy(game.AmountOf).Take(22))
            game.Pick(number);

        Assert.Equal(23, game.CasesClicked);
        decimal offer = game.TryMakeOffer()!.Value;
        Assert.True(offer > 10000m);
    }

    [Fact]
    public void CasesToOpen_FollowTheSchedule()
    {
        GameState game = NewGame();
        var shown = new Dictionary<int, int>();

        for (int number = 1; game.CasesClicked < 29; number++)
        {
            game.Pick(number);
            shown[game.CasesClicked] = game.NextCasesToOpen();
        }

        Assert.Equal(7, shown[1]);
        Assert.Equal(6, shown[2]);
        Assert.Equal(1, shown[7]);
        Assert.Equal(6, shown[8]);
        Assert.Equal(1, shown[13]);
        Assert.Equal(5, shown[14]);
        Assert.Equal(2, shown[23]);
        Assert.Equal(1, shown[24]);
        Assert.Equal(2, shown[25]);
        Assert.Equal(0, shown[29]);
    }

    [Fact]
    public void AtTheLastOfferRound_OneCaseRemains_AndItIsNotTheOwnCase()
    {
        GameState game = NewGame();
        PlayUntilClick(game, 29);

        Assert.True(game.IsLastOfferRound);
        Assert.NotNull(game.LastRemainingCase);
        Assert.NotEqual(game.OwnCase, game.LastRemainingCase);
    }

    [Fact]
    public void LastRemainingCase_IsNullWhileMoreThanOneIsLeft()
    {
        GameState game = NewGame();
        PlayUntilClick(game, 10);

        Assert.Null(game.LastRemainingCase);
    }

    [Fact]
    public void TheGameCanOnlyBeFinishedOnce_AndOnlyWithAnOwnCase()
    {
        GameState game = NewGame();

        Assert.False(game.TryFinish());

        game.Pick(1);

        Assert.True(game.TryFinish());
        Assert.True(game.IsOver);
        Assert.False(game.TryFinish());
        Assert.Throws<InvalidOperationException>(() => game.Pick(2));
    }

    [Fact]
    public void Reset_StartsOverInTheMiddleOfAGame()
    {
        GameState game = NewGame();
        PlayUntilClick(game, 12);
        game.TryFinish();

        game.Reset();

        Assert.Equal(0, game.CasesClicked);
        Assert.Null(game.OwnCase);
        Assert.False(game.IsOver);
        Assert.Equal(CasePick.OwnCase, game.Pick(1));
        Assert.Equal(7, game.NextCasesToOpen());
    }
}
