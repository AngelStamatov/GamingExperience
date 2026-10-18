using DomainWallet = Betty.Wallet.Core.Domain.Wallet;

namespace Betty.Wallet.Tests.Domain;

public class WalletRoundTests
{
    public static TheoryData<decimal, decimal, decimal, decimal> ValidRounds => new()
    {
        { 20m, 5m, 0m, 15m },
        { 20m, 5m, 2m, 17m },
        { 20m, 5m, 35.35m, 50.35m },
        { 5m, 5m, 0m, 0m },
        { 20m, 20m, 2m, 2m },
        { 20m, 15m, 0m, 5m },
        { decimal.MaxValue, 1m, 1m, decimal.MaxValue }
    };

    public static TheoryData<decimal> NonPositiveBets => new()
    {
        0m,
        -0.01m,
        -1m,
        decimal.MinValue
    };

    public static TheoryData<decimal> NegativePayouts => new()
    {
        -0.01m,
        -1m,
        decimal.MinValue
    };

    public static TheoryData<decimal> UncoveredBets => new()
    {
        20.01m,
        decimal.MaxValue
    };

    [Theory]
    [MemberData(nameof(ValidRounds))]
    public void ApplyRound_ValidRound_SubtractsBetThenAddsPayout(
        decimal initialBalance, decimal betAmount, decimal payout, decimal expectedBalance)
    {
        var wallet = new DomainWallet();
        wallet.Deposit(initialBalance);

        wallet.ApplyRound(betAmount, payout);

        Assert.Equal(expectedBalance, wallet.Balance);
    }

    [Theory]
    [MemberData(nameof(NonPositiveBets))]
    public void ApplyRound_NonPositiveBet_ThrowsAndPreservesBalance(decimal betAmount)
    {
        var wallet = new DomainWallet();
        wallet.Deposit(20m);

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => wallet.ApplyRound(betAmount, 2m));

        Assert.Equal("betAmount", exception.ParamName);
        Assert.Equal(20m, wallet.Balance);
    }

    [Theory]
    [MemberData(nameof(NegativePayouts))]
    public void ApplyRound_NegativePayout_ThrowsAndPreservesBalance(decimal payout)
    {
        var wallet = new DomainWallet();
        wallet.Deposit(20m);

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => wallet.ApplyRound(5m, payout));

        Assert.Equal("payout", exception.ParamName);
        Assert.Equal(20m, wallet.Balance);
    }

    [Theory]
    [MemberData(nameof(UncoveredBets))]
    public void ApplyRound_UncoveredBet_ThrowsAndPreservesBalance(decimal betAmount)
    {
        var wallet = new DomainWallet();
        wallet.Deposit(20m);

        Assert.Throws<InvalidOperationException>(() => wallet.ApplyRound(betAmount, 0m));

        Assert.Equal(20m, wallet.Balance);
    }

    [Fact]
    public void ApplyRound_Overflow_ThrowsAndPreservesOriginalBalance()
    {
        var wallet = new DomainWallet();
        wallet.Deposit(20m);

        Assert.Throws<OverflowException>(() => wallet.ApplyRound(5m, decimal.MaxValue));

        Assert.Equal(20m, wallet.Balance);
    }
}
