using DomainWallet = Betty.Wallet.Core.Domain.Wallet;

namespace Betty.Wallet.Tests.Domain;

public class WalletTests
{
    public static TheoryData<decimal> PositiveAmounts => new()
    {
        10.25m,
        0.15m,
        0.0000000000000000000000000001m,
        decimal.MaxValue
    };

    public static TheoryData<decimal> NonPositiveAmounts => new()
    {
        0m,
        -0.01m,
        -1m,
        decimal.MinValue
    };

    public static TheoryData<decimal> AmountsExceedingBalance => new()
    {
        10.26m,
        11m,
        decimal.MaxValue
    };

    [Fact]
    public void NewWallet_HasZeroBalance()
    {
        var wallet = new DomainWallet();

        Assert.Equal(0m, wallet.Balance);
    }

    [Theory]
    [MemberData(nameof(PositiveAmounts))]
    public void Deposit_PositiveAmount_IncreasesBalance(decimal amount)
    {
        var wallet = new DomainWallet();

        wallet.Deposit(amount);

        Assert.Equal(amount, wallet.Balance);
    }

    [Fact]
    public void Deposit_ConsecutiveAmounts_AccumulatesBalance()
    {
        var wallet = new DomainWallet();

        wallet.Deposit(10m);
        wallet.Deposit(20m);
        wallet.Deposit(5m);

        Assert.Equal(35m, wallet.Balance);
    }

    [Theory]
    [MemberData(nameof(NonPositiveAmounts))]
    public void Deposit_NonPositiveAmount_ThrowsAndPreservesBalance(decimal amount)
    {
        var wallet = new DomainWallet();
        wallet.Deposit(10.25m);

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => wallet.Deposit(amount));

        Assert.Equal("amount", exception.ParamName);
        Assert.Equal(10.25m, wallet.Balance);
    }

    [Fact]
    public void Withdraw_AmountWithinBalance_DecreasesBalance()
    {
        var wallet = new DomainWallet();
        wallet.Deposit(10.25m);

        wallet.Withdraw(3.25m);

        Assert.Equal(7m, wallet.Balance);
    }

    [Fact]
    public void Withdraw_EntireBalance_LeavesZeroBalance()
    {
        var wallet = new DomainWallet();
        wallet.Deposit(10.25m);
        Assert.Equal(10.25m, wallet.Balance);

        wallet.Withdraw(10.25m);

        Assert.Equal(0m, wallet.Balance);
    }

    [Theory]
    [MemberData(nameof(NonPositiveAmounts))]
    public void Withdraw_NonPositiveAmount_ThrowsAndPreservesBalance(decimal amount)
    {
        var wallet = new DomainWallet();
        wallet.Deposit(10.25m);

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => wallet.Withdraw(amount));

        Assert.Equal("amount", exception.ParamName);
        Assert.Equal(10.25m, wallet.Balance);
    }

    [Theory]
    [MemberData(nameof(AmountsExceedingBalance))]
    public void Withdraw_AmountExceedingBalance_ThrowsAndPreservesBalance(decimal amount)
    {
        var wallet = new DomainWallet();
        wallet.Deposit(10.25m);

        Assert.Throws<InvalidOperationException>(() => wallet.Withdraw(amount));

        Assert.Equal(10.25m, wallet.Balance);
    }

    [Fact]
    public void Withdraw_FromEmptyWallet_ThrowsAndPreservesZeroBalance()
    {
        var wallet = new DomainWallet();

        Assert.Throws<InvalidOperationException>(() => wallet.Withdraw(0.15m));

        Assert.Equal(0m, wallet.Balance);
    }

    [Fact]
    public void Deposit_FractionalAmounts_AddsExactly()
    {
        var wallet = new DomainWallet();
        wallet.Deposit(10.25m);

        wallet.Deposit(0.15m);

        Assert.Equal(10.40m, wallet.Balance);
    }

    [Fact]
    public void Withdraw_FractionalAmount_SubtractsExactly()
    {
        var wallet = new DomainWallet();
        wallet.Deposit(10.25m);

        wallet.Withdraw(0.15m);

        Assert.Equal(10.10m, wallet.Balance);
    }
}
