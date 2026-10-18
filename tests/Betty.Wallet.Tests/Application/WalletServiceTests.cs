using Betty.Wallet.Core.Abstractions;
using Betty.Wallet.Core.Application;
using Betty.Wallet.Core.Domain;
using Betty.Wallet.Tests.Fakes;
using DomainWallet = Betty.Wallet.Core.Domain.Wallet;

namespace Betty.Wallet.Tests.Application;

public class WalletServiceTests
{
    public static TheoryData<int, int, int, int, decimal, decimal, decimal> WinningRounds => new()
    {
        { 50, 1, 201, 40, 0.40m, 2m, 17m },
        { 90, 201, 1001, 707, 7.07m, 35.35m, 50.35m }
    };

    public static TheoryData<decimal, decimal> InvalidBets => new()
    {
        { 20m, 0m },
        { 20m, -1m },
        { 20m, 0.99m },
        { 20m, 10.01m },
        { 0m, -1m },
        { 0m, 10.01m },
        { 20m, decimal.MinValue },
        { 20m, decimal.MaxValue }
    };

    public static TheoryData<decimal, decimal> UncoveredBets => new()
    {
        { 0m, 1m },
        { 4.99m, 5m },
        { 9.99m, 10m }
    };

    [Fact]
    public void Constructor_NullWallet_ThrowsArgumentNullException()
    {
        var game = new SlotGame(new FakeRandomSource());

        var exception = Assert.Throws<ArgumentNullException>(() => new WalletService(null!, game));

        Assert.Equal("wallet", exception.ParamName);
    }

    [Fact]
    public void Constructor_NullSlotGame_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => new WalletService(new DomainWallet(), null!));

        Assert.Equal("slotGame", exception.ParamName);
    }

    [Fact]
    public void Balance_ReflectsChangesToSuppliedWallet()
    {
        var wallet = new DomainWallet();
        var service = new WalletService(wallet, new SlotGame(new FakeRandomSource()));
        Assert.Equal(0m, service.Balance);

        wallet.Deposit(20m);

        Assert.Equal(20m, service.Balance);
    }

    [Fact]
    public void Deposit_UpdatesSuppliedWallet()
    {
        var wallet = new DomainWallet();
        var service = new WalletService(wallet, new SlotGame(new FakeRandomSource()));

        service.Deposit(10.25m);

        Assert.Equal(10.25m, wallet.Balance);
        Assert.Equal(10.25m, service.Balance);
    }

    [Fact]
    public void Withdraw_UpdatesSuppliedWallet()
    {
        var wallet = new DomainWallet();
        wallet.Deposit(20m);
        var service = new WalletService(wallet, new SlotGame(new FakeRandomSource()));

        service.Withdraw(5m);

        Assert.Equal(15m, wallet.Balance);
        Assert.Equal(15m, service.Balance);
    }

    [Fact]
    public void PlaceBet_LosingRound_DeductsOnlyTheBet()
    {
        var wallet = new DomainWallet();
        wallet.Deposit(20m);
        var random = new FakeRandomSource((0, 100, 0));
        var service = new WalletService(wallet, new SlotGame(random));

        var result = service.PlaceBet(5m);

        Assert.Equal(5m, result.BetAmount);
        Assert.Equal(0m, result.Multiplier);
        Assert.Equal(0m, result.Payout);
        Assert.Equal(15m, wallet.Balance);
        Assert.Equal(15m, service.Balance);
        random.VerifyAllCallsMade();
    }

    [Theory]
    [MemberData(nameof(WinningRounds))]
    public void PlaceBet_WinningRound_AppliesTotalPayoutOnceAndReturnsResult(
        int category, int minInclusive, int maxExclusive, int multiplierDraw,
        decimal expectedMultiplier, decimal expectedPayout, decimal expectedBalance)
    {
        var wallet = new DomainWallet();
        wallet.Deposit(20m);
        var random = new FakeRandomSource((0, 100, category), (minInclusive, maxExclusive, multiplierDraw));
        var service = new WalletService(wallet, new SlotGame(random));

        var result = service.PlaceBet(5m);

        Assert.Equal(5m, result.BetAmount);
        Assert.Equal(expectedMultiplier, result.Multiplier);
        Assert.Equal(expectedPayout, result.Payout);
        Assert.Equal(expectedBalance, wallet.Balance);
        Assert.Equal(expectedBalance, service.Balance);
        random.VerifyAllCallsMade();
    }

    [Fact]
    public void PlaceBet_EntireBalance_IsAllowed()
    {
        var wallet = new DomainWallet();
        wallet.Deposit(5m);
        var random = new FakeRandomSource((0, 100, 90), (201, 1001, 707));
        var service = new WalletService(wallet, new SlotGame(random));

        var result = service.PlaceBet(5m);

        Assert.Equal(5m, result.BetAmount);
        Assert.Equal(35.35m, result.Payout);
        Assert.Equal(35.35m, wallet.Balance);
        Assert.Equal(35.35m, service.Balance);
        random.VerifyAllCallsMade();
    }

    [Theory]
    [MemberData(nameof(InvalidBets))]
    public void PlaceBet_InvalidBet_ThrowsBeforeRandomDrawAndPreservesBalance(decimal initialBalance, decimal betAmount)
    {
        var wallet = new DomainWallet();
        if (initialBalance > 0m)
        {
            wallet.Deposit(initialBalance);
        }
        var random = new FakeRandomSource();
        var service = new WalletService(wallet, new SlotGame(random));

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => service.PlaceBet(betAmount));

        Assert.Equal("betAmount", exception.ParamName);
        Assert.Equal(0, random.CallCount);
        Assert.Equal(initialBalance, wallet.Balance);
        Assert.Equal(initialBalance, service.Balance);
    }

    [Theory]
    [MemberData(nameof(UncoveredBets))]
    public void PlaceBet_UncoveredBet_ThrowsBeforeRandomDrawAndPreservesBalance(decimal initialBalance, decimal betAmount)
    {
        var wallet = new DomainWallet();
        if (initialBalance > 0m)
        {
            wallet.Deposit(initialBalance);
        }
        var random = new FakeRandomSource();
        var service = new WalletService(wallet, new SlotGame(random));

        Assert.Throws<InvalidOperationException>(() => service.PlaceBet(betAmount));

        Assert.Equal(0, random.CallCount);
        Assert.Equal(initialBalance, wallet.Balance);
        Assert.Equal(initialBalance, service.Balance);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void PlaceBet_RandomSourceFailure_PreservesBalance(int throwOnCall)
    {
        var wallet = new DomainWallet();
        wallet.Deposit(20m);
        var random = new ThrowingRandomSource(throwOnCall);
        var service = new WalletService(wallet, new SlotGame(random));

        var exception = Assert.Throws<InvalidOperationException>(() => service.PlaceBet(5m));

        Assert.Same(random.Failure, exception);
        Assert.Equal(throwOnCall, random.CallCount);
        Assert.Equal(20m, wallet.Balance);
        Assert.Equal(20m, service.Balance);
    }

    [Fact]
    public void PlaceBet_OverflowApplyingRound_PreservesOriginalBalance()
    {
        var wallet = new DomainWallet();
        wallet.Deposit(decimal.MaxValue);
        var random = new FakeRandomSource((0, 100, 90), (201, 1001, 707));
        var service = new WalletService(wallet, new SlotGame(random));

        Assert.Throws<OverflowException>(() => service.PlaceBet(5m));

        Assert.Equal(decimal.MaxValue, wallet.Balance);
        Assert.Equal(decimal.MaxValue, service.Balance);
        random.VerifyAllCallsMade();
    }

    private sealed class ThrowingRandomSource : IRandomSource
    {
        private readonly int _throwOnCall;

        public ThrowingRandomSource(int throwOnCall)
        {
            _throwOnCall = throwOnCall;
        }

        public InvalidOperationException Failure { get; } = new("Random source failed.");

        public int CallCount { get; private set; }

        public int Next(int minInclusive, int maxExclusive)
        {
            CallCount++;
            if (CallCount == _throwOnCall)
            {
                throw Failure;
            }

            return 90;
        }
    }
}
