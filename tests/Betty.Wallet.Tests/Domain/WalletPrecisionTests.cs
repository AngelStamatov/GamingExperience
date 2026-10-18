using DomainWallet = Betty.Wallet.Core.Domain.Wallet;

namespace Betty.Wallet.Tests.Domain;

public class WalletPrecisionTests
{
    public static TheoryData<decimal, decimal, decimal> RepresentableDeposits => new()
    {
        { 1m, 0.0000000000000000000000000001m, 1.0000000000000000000000000001m },
        { 7.9000000000000000000000000000m, 0.1m, 8m },
        { 4294967296m, 18446744073709551616m, 18446744078004518912m },
        { 18446744078004518912m, 1m, 18446744078004518913m }
    };

    [Fact]
    public void Withdraw_InexactResultAtMaximumBalance_ThrowsAndPreservesBalance()
    {
        var wallet = new DomainWallet();
        wallet.Deposit(decimal.MaxValue);

        Assert.Throws<OverflowException>(() => wallet.Withdraw(0.01m));

        Assert.Equal(decimal.MaxValue, wallet.Balance);
    }

    [Fact]
    public void Deposit_InexactResultAtMaximumBalance_ThrowsAndPreservesBalance()
    {
        var wallet = new DomainWallet();
        wallet.Deposit(decimal.MaxValue);

        Assert.Throws<OverflowException>(() => wallet.Deposit(0.01m));

        Assert.Equal(decimal.MaxValue, wallet.Balance);
    }

    [Fact]
    public void ApplyRound_InexactFinalResultAtMaximumBalance_ThrowsAndPreservesBalance()
    {
        var wallet = new DomainWallet();
        wallet.Deposit(decimal.MaxValue);

        Assert.Throws<OverflowException>(() => wallet.ApplyRound(1.01m, 1m));

        Assert.Equal(decimal.MaxValue, wallet.Balance);
    }

    [Fact]
    public void ApplyRound_ExactFinalResultDespiteInexactIntermediate_Succeeds()
    {
        var wallet = new DomainWallet();
        wallet.Deposit(decimal.MaxValue);

        wallet.ApplyRound(1.01m, 1.01m);

        Assert.Equal(decimal.MaxValue, wallet.Balance);
    }

    [Theory]
    [MemberData(nameof(RepresentableDeposits))]
    public void Deposit_RepresentableResult_PreservesAllDigits(
        decimal initialBalance, decimal amount, decimal expectedBalance)
    {
        var wallet = new DomainWallet();
        wallet.Deposit(initialBalance);

        wallet.Deposit(amount);

        Assert.Equal(expectedBalance, wallet.Balance);
    }

    [Fact]
    public void Deposit_InexactTinyAdditionAtSmallBalance_ThrowsAndPreservesBalance()
    {
        var wallet = new DomainWallet();
        wallet.Deposit(8m);

        Assert.Throws<OverflowException>(() => wallet.Deposit(0.0000000000000000000000000001m));

        Assert.Equal(8m, wallet.Balance);
    }

    [Fact]
    public void Withdraw_InexactTinyDeductionAtSmallBalance_ThrowsAndPreservesBalance()
    {
        var wallet = new DomainWallet();
        wallet.Deposit(8m);

        Assert.Throws<OverflowException>(() => wallet.Withdraw(0.0000000000000000000000000001m));

        Assert.Equal(8m, wallet.Balance);
    }

    [Fact]
    public void Withdraw_RepresentableSmallestFraction_UsesExactResult()
    {
        var wallet = new DomainWallet();
        wallet.Deposit(1m);

        wallet.Withdraw(0.0000000000000000000000000001m);

        Assert.Equal(0.9999999999999999999999999999m, wallet.Balance);
    }

    [Fact]
    public void Withdraw_EntireMaximumBalance_Succeeds()
    {
        var wallet = new DomainWallet();
        wallet.Deposit(decimal.MaxValue);

        wallet.Withdraw(decimal.MaxValue);

        Assert.Equal(0m, wallet.Balance);
    }

    [Fact]
    public void ApplyRound_ExactFinalResultAfterFractionalCancellation_Succeeds()
    {
        var wallet = new DomainWallet();
        wallet.Deposit(decimal.MaxValue);

        wallet.ApplyRound(1.01m, 0.01m);

        Assert.Equal(79228162514264337593543950334m, wallet.Balance);
    }

    [Fact]
    public void ApplyRound_SmallestBetWithZeroPayout_ProducesZero()
    {
        var wallet = new DomainWallet();
        wallet.Deposit(0.0000000000000000000000000001m);

        wallet.ApplyRound(0.0000000000000000000000000001m, 0m);

        Assert.Equal(0m, wallet.Balance);
    }

    [Fact]
    public void Deposit_WholeUnitBeyondMaximum_ThrowsAndPreservesBalance()
    {
        var wallet = new DomainWallet();
        wallet.Deposit(decimal.MaxValue);

        Assert.Throws<OverflowException>(() => wallet.Deposit(1m));

        Assert.Equal(decimal.MaxValue, wallet.Balance);
    }
}
