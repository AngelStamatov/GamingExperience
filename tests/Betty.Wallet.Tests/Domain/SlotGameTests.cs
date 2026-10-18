using Betty.Wallet.Core.Domain;
using Betty.Wallet.Tests.Fakes;

namespace Betty.Wallet.Tests.Domain;

public class SlotGameTests
{
    public static TheoryData<decimal> ValidBoundaryBets => new()
    {
        1m,
        10m
    };

    public static TheoryData<decimal> InvalidBets => new()
    {
        decimal.MinValue,
        -1m,
        0m,
        0.99m,
        10.01m,
        decimal.MaxValue
    };

    public static TheoryData<int, int, int, int, decimal, decimal> WinningMultiplierBoundaries => new()
    {
        { 50, 1, 201, 1, 0.01m, 0.05m },
        { 89, 1, 201, 200, 2.00m, 10.00m },
        { 90, 201, 1001, 201, 2.01m, 10.05m },
        { 99, 201, 1001, 1000, 10.00m, 50.00m }
    };

    public static TheoryData<decimal, int, decimal> RoundedPayouts => new()
    {
        { 1.50m, 101, 1.52m },
        { 1.50m, 103, 1.55m },
        { 1.234m, 101, 1.25m },
        { 1.234m, 100, 1.23m },
        { 1.245m, 100, 1.25m },
        { 9.99m, 200, 19.98m }
    };

    [Fact]
    public void Constructor_NullRandomSource_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => new SlotGame(null!));

        Assert.Equal("randomSource", exception.ParamName);
    }

    [Theory]
    [MemberData(nameof(ValidBoundaryBets))]
    public void Play_BoundaryBet_ReturnsTotalPayoutIncludingStake(decimal betAmount)
    {
        var random = new FakeRandomSource((0, 100, 50), (1, 201, 100));
        var game = new SlotGame(random);

        var result = game.Play(betAmount);

        Assert.Equal(betAmount, result.BetAmount);
        Assert.Equal(1m, result.Multiplier);
        Assert.Equal(betAmount, result.Payout);
        random.VerifyAllCallsMade();
    }

    [Theory]
    [MemberData(nameof(InvalidBets))]
    public void Play_InvalidBet_ThrowsBeforeUsingRandomSource(decimal betAmount)
    {
        var random = new FakeRandomSource();
        var game = new SlotGame(random);

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => game.Play(betAmount));

        Assert.Equal("betAmount", exception.ParamName);
        Assert.Equal(0, random.CallCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(49)]
    public void Play_LosingCategoryBoundary_ReturnsZeroWithoutDrawingMultiplier(int category)
    {
        var random = new FakeRandomSource((0, 100, category));
        var game = new SlotGame(random);

        var result = game.Play(5m);

        Assert.Equal(5m, result.BetAmount);
        Assert.Equal(0m, result.Multiplier);
        Assert.Equal(0m, result.Payout);
        Assert.Equal(1, random.CallCount);
        random.VerifyAllCallsMade();
    }

    [Theory]
    [InlineData(50)]
    [InlineData(89)]
    public void Play_SmallWinCategoryBoundary_UsesSmallMultiplierRange(int category)
    {
        var random = new FakeRandomSource((0, 100, category), (1, 201, 150));
        var game = new SlotGame(random);

        var result = game.Play(5m);

        Assert.Equal(5m, result.BetAmount);
        Assert.Equal(1.50m, result.Multiplier);
        Assert.Equal(7.50m, result.Payout);
        random.VerifyAllCallsMade();
    }

    [Theory]
    [InlineData(90)]
    [InlineData(99)]
    public void Play_LargeWinCategoryBoundary_UsesLargeMultiplierRange(int category)
    {
        var random = new FakeRandomSource((0, 100, category), (201, 1001, 707));
        var game = new SlotGame(random);

        var result = game.Play(5m);

        Assert.Equal(5m, result.BetAmount);
        Assert.Equal(7.07m, result.Multiplier);
        Assert.Equal(35.35m, result.Payout);
        random.VerifyAllCallsMade();
    }

    [Theory]
    [MemberData(nameof(WinningMultiplierBoundaries))]
    public void Play_WinningMultiplierBoundary_ReturnsExpectedMultiplierAndPayout(
        int category, int minInclusive, int maxExclusive, int multiplierDraw,
        decimal expectedMultiplier, decimal expectedPayout)
    {
        var random = new FakeRandomSource((0, 100, category), (minInclusive, maxExclusive, multiplierDraw));
        var game = new SlotGame(random);

        var result = game.Play(5m);

        Assert.Equal(5m, result.BetAmount);
        Assert.Equal(expectedMultiplier, result.Multiplier);
        Assert.Equal(expectedPayout, result.Payout);
        random.VerifyAllCallsMade();
    }

    [Theory]
    [MemberData(nameof(RoundedPayouts))]
    public void Play_FractionalPayout_RoundsToTwoPlacesAwayFromZero(
        decimal betAmount, int multiplierDraw, decimal expectedPayout)
    {
        var random = new FakeRandomSource((0, 100, 50), (1, 201, multiplierDraw));
        var game = new SlotGame(random);

        var result = game.Play(betAmount);

        Assert.Equal(betAmount, result.BetAmount);
        Assert.Equal(expectedPayout, result.Payout);
        random.VerifyAllCallsMade();
    }

    [Fact]
    public void Play_ConsecutiveRounds_UsesFreshDrawsAndPreservesEarlierResult()
    {
        var random = new FakeRandomSource((0, 100, 90), (201, 1001, 707), (0, 100, 0));
        var game = new SlotGame(random);

        var firstResult = game.Play(5m);
        var secondResult = game.Play(1m);

        Assert.Equal(5m, firstResult.BetAmount);
        Assert.Equal(7.07m, firstResult.Multiplier);
        Assert.Equal(35.35m, firstResult.Payout);
        Assert.Equal(1m, secondResult.BetAmount);
        Assert.Equal(0m, secondResult.Multiplier);
        Assert.Equal(0m, secondResult.Payout);
        random.VerifyAllCallsMade();
    }
}
