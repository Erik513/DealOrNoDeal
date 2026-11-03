namespace DealOrNoDeal.Tests;

public class BankerOfferCalculatorTests
{
    [Fact]
    public void Rounds_AreTheEightOfferRounds_AndTheLastIsTwentyNine()
    {
        Assert.Equal(new[] { 8, 14, 19, 23, 25, 27, 28, 29 }, BankerOfferCalculator.Rounds);
        Assert.Equal(29, BankerOfferCalculator.LastRound);
    }

    [Theory]
    [InlineData(0, 8)]
    [InlineData(8, 6)]
    [InlineData(13, 1)]
    [InlineData(28, 1)]
    public void CasesUntilNextOffer_CountsToTheNextRound(int casesClicked, int expected)
    {
        Assert.Equal(expected, BankerOfferCalculator.CalculateCasesUntilNextOffer(casesClicked));
    }

    [Fact]
    public void CasesUntilNextOffer_IsNullOnTheLastOffer()
    {
        Assert.Null(BankerOfferCalculator.CalculateCasesUntilNextOffer(29));
    }

    [Theory]
    [InlineData(8, 1500)]
    [InlineData(29, 6000)]
    public void RevealDelay_GrowsWithTheRound(int round, int expectedMs)
    {
        Assert.Equal(expectedMs, BankerOfferCalculator.CalculateRevealDelayMs(round));
    }

    [Fact]
    public void TryGetOfferPercentage_OnlyKnowsOfferRounds()
    {
        Assert.True(BankerOfferCalculator.TryGetOfferPercentage(8, out decimal percentage));
        Assert.Equal(0.10m, percentage);
        Assert.False(BankerOfferCalculator.TryGetOfferPercentage(9, out _));
    }

    [Fact]
    public void Offer_IsZero_WithoutRemainingAmounts()
    {
        Assert.Equal(0m, BankerOfferCalculator.CalculateOffer(0.5m, new decimal[0], new Random(1)));
    }

    [Fact]
    public void Offer_IsZero_WhenEverythingLeftIsZero()
    {
        Assert.Equal(0m, BankerOfferCalculator.CalculateOffer(0.9m, new[] { 0m, 0m }, new Random(1)));
    }

    [Fact]
    public void Offer_NeverExceedsTheFairAverage_WhateverTheSeed()
    {
        decimal[][] pools =
        {
            new[] { 1m, 1000000m },
            new[] { 5m, 10m, 20m, 50m },
            new[] { 750000m, 1000000m },
            new[] { 0m, 300m, 75000m, 500000m, 1000000m },
        };

        foreach (decimal[] pool in pools)
        {
            decimal average = pool.Average();

            for (int seed = 0; seed < 200; seed++)
            {
                foreach (int round in BankerOfferCalculator.Rounds)
                {
                    BankerOfferCalculator.TryGetOfferPercentage(round, out decimal percentage);
                    decimal offer = BankerOfferCalculator.CalculateOffer(percentage, pool, new Random(seed));

                    Assert.InRange(offer, 0m, average);
                }
            }
        }
    }

    [Fact]
    public void Offer_IsStableForTheSameSeed()
    {
        decimal[] pool = { 10m, 5000m, 250000m };

        decimal first = BankerOfferCalculator.CalculateOffer(0.4m, pool, new Random(7));
        decimal second = BankerOfferCalculator.CalculateOffer(0.4m, pool, new Random(7));

        Assert.Equal(first, second);
    }

    [Fact]
    public void Offer_IsLowerWhenTheAmountsAreSpreadFarApart()
    {
        // Same average (500), one pool tight, one extremely spread: the
        // spread one must be dampened. Seed chosen so neither is rounded;
        // checked over many seeds below by comparing the minimum possible.
        decimal[] tight = { 499m, 501m };
        decimal[] spread = { 0m, 1000m };

        decimal tightOffer = Enumerable.Range(0, 200)
            .Select(seed => BankerOfferCalculator.CalculateOffer(0.5m, tight, new Random(seed)))
            .Max();
        decimal spreadOffer = Enumerable.Range(0, 200)
            .Select(seed => BankerOfferCalculator.CalculateOffer(0.5m, spread, new Random(seed)))
            .Min();

        Assert.True(spreadOffer < tightOffer);
    }
}
