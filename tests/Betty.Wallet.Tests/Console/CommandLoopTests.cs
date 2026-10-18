using System.Globalization;
using Betty.Wallet.Console;
using Betty.Wallet.Core.Application;
using Betty.Wallet.Core.Domain;
using Betty.Wallet.Tests.Fakes;
using DomainWallet = Betty.Wallet.Core.Domain.Wallet;

namespace Betty.Wallet.Tests.Console;

public class CommandLoopTests
{
    [Fact]
    public void Run_DepositBetWithdrawExit_ShowsExactResultsAndBalances()
    {
        var random = new FakeRandomSource((0, 100, 50), (1, 201, 101));

        var (output, balance) = RunSession("deposit 20\nbet 1.50\nwithdraw 5\nexit\n", random);

        var expectedLines = new[]
        {
            "Betty Wallet",
            "Commands: deposit <amount>, withdraw <amount>, bet <amount>, exit.",
            "Use a decimal point and at most 2 decimal places. Bets: $1.00 to $10.00.",
            "Balance: $0.00",
            "> Deposited $20.00. Balance: $20.00",
            "> Bet: $1.50. Gross payout: $1.52. Balance: $20.02",
            "> Withdrew $5.00. Balance: $15.02",
            "> Goodbye."
        };
        Assert.Equal(string.Join(Environment.NewLine, expectedLines) + Environment.NewLine, output);
        Assert.Equal(15.02m, balance);
    }

    [Fact]
    public void Run_InvalidCommandAndInsufficientBalance_ReportsCurrentBalanceAndContinues()
    {
        var (output, balance) = RunSession(
            "unknown\ndeposit 20\nwithdraw 21\nwithdraw 5\nexit\n", new FakeRandomSource());

        Assert.Contains("Invalid command or amount.", output);
        Assert.Contains("Insufficient balance. Balance: $20.00", output);
        Assert.Contains("Withdrew $5.00. Balance: $15.00", output);
        Assert.Equal(15m, balance);
    }

    [Theory]
    [InlineData("deposit 0", "Amount must be greater than $0.00.")]
    [InlineData("deposit -1", "Amount must be greater than $0.00.")]
    [InlineData("withdraw 0", "Amount must be greater than $0.00.")]
    [InlineData("withdraw -1", "Amount must be greater than $0.00.")]
    [InlineData("bet 0.99", "Bet must be between $1.00 and $10.00.")]
    [InlineData("bet 10.01", "Bet must be between $1.00 and $10.00.")]
    [InlineData("bet -1", "Bet must be between $1.00 and $10.00.")]
    public void Run_InvalidOperation_ReportsErrorAndContinues(string command, string expectedError)
    {
        var (output, balance) = RunSession(command + "\ndeposit 1\nexit\n", new FakeRandomSource(), 20m);

        Assert.Contains(expectedError + " Balance: $20.00", output);
        Assert.Contains("Deposited $1.00. Balance: $21.00", output);
        Assert.Equal(21m, balance);
    }

    [Theory]
    [InlineData("deposit 1.005")]
    [InlineData("withdraw 1.005")]
    [InlineData("bet 1.505")]
    [InlineData("deposit 1,50")]
    [InlineData("deposit 1,000.00")]
    [InlineData("deposit 79228162514264337593543950336")]
    public void Run_InvalidAmount_IsRejectedWithoutRoundingOrChangingBalance(string command)
    {
        var (output, balance) = RunSession(command + "\nexit\n", new FakeRandomSource(), 20m);

        Assert.Contains("Invalid command or amount.", output);
        Assert.Contains("Balance: $20.00", output);
        Assert.DoesNotContain("Deposited ", output);
        Assert.DoesNotContain("Withdrew ", output);
        Assert.DoesNotContain("Gross payout:", output);
        Assert.Equal(20m, balance);
    }

    [Fact]
    public void Run_UncoveredBet_DoesNotUseRandomSourceAndContinues()
    {
        var random = new FakeRandomSource();

        var (output, balance) = RunSession("bet 1\ndeposit 20\nexit\n", random);

        Assert.Contains("Insufficient balance. Balance: $0.00", output);
        Assert.Contains("Deposited $20.00. Balance: $20.00", output);
        Assert.Equal(0, random.CallCount);
        Assert.Equal(20m, balance);
    }

    [Fact]
    public void Run_DepositOverflow_PreservesBalanceAndContinues()
    {
        var (output, balance) = RunSession("deposit 1\nwithdraw 1\nexit\n", new FakeRandomSource(), decimal.MaxValue);

        Assert.Contains("Result is too large or requires unsupported decimal precision. Balance: $79228162514264337593543950335.00", output);
        Assert.Contains("Withdrew $1.00. Balance: $79228162514264337593543950334.00", output);
        Assert.Equal(79228162514264337593543950334m, balance);
    }

    [Fact]
    public void Run_RoundOverflow_PreservesBalanceAndContinues()
    {
        var random = new FakeRandomSource((0, 100, 90), (201, 1001, 707));

        var (output, balance) = RunSession("bet 5\nwithdraw 1\nexit\n", random, decimal.MaxValue);

        Assert.Contains("Result is too large or requires unsupported decimal precision. Balance: $79228162514264337593543950335.00", output);
        Assert.Contains("Withdrew $1.00. Balance: $79228162514264337593543950334.00", output);
        Assert.Equal(79228162514264337593543950334m, balance);
    }

    [Fact]
    public void Run_InexactWithdrawal_ReportsUnsupportedPrecisionAndContinues()
    {
        var (output, balance) = RunSession("withdraw 0.01\nwithdraw 1\nexit\n", new FakeRandomSource(), decimal.MaxValue);

        Assert.Contains("Result is too large or requires unsupported decimal precision. Balance: $79228162514264337593543950335.00", output);
        Assert.DoesNotContain("Withdrew $0.01.", output);
        Assert.Contains("Withdrew $1.00. Balance: $79228162514264337593543950334.00", output);
        Assert.Equal(79228162514264337593543950334m, balance);
    }

    [Theory]
    [InlineData("bg-BG")]
    [InlineData("fr-FR")]
    [InlineData("tr-TR")]
    public void Run_FormatsDollarsIndependentlyOfCulture(string cultureName)
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);

            var (output, balance) = RunSession("DEPOSIT 1234.50\nWITHDRAW 0.15\nEXIT\n", new FakeRandomSource());

            Assert.Contains("Balance: $0.00", output);
            Assert.Contains("Deposited $1234.50. Balance: $1234.50", output);
            Assert.Contains("Withdrew $0.15. Balance: $1234.35", output);
            Assert.Equal(1234.35m, balance);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Fact]
    public void Run_Exit_StopsBeforeReadingFollowingCommands()
    {
        var (output, balance) = RunSession("exit\ndeposit 20\n", new FakeRandomSource());

        Assert.EndsWith("Goodbye." + Environment.NewLine, output);
        Assert.DoesNotContain("Deposited", output);
        Assert.Equal(0m, balance);
    }

    [Theory]
    [InlineData("")]
    [InlineData("deposit 20")]
    public void Run_EndOfInput_CompletesNormally(string input)
    {
        var (output, balance) = RunSession(input, new FakeRandomSource());

        Assert.EndsWith("Goodbye." + Environment.NewLine, output);
        Assert.Equal(input.Length == 0 ? 0m : 20m, balance);
    }

    [Theory]
    [InlineData("input")]
    [InlineData("output")]
    [InlineData("walletService")]
    public void Constructor_NullDependency_ThrowsArgumentNullException(string parameterName)
    {
        using var input = new StringReader("");
        using var output = new StringWriter();
        var service = new WalletService(new DomainWallet(), new SlotGame(new FakeRandomSource()));

        var exception = Assert.Throws<ArgumentNullException>(() => new CommandLoop(
            parameterName == "input" ? null! : input,
            parameterName == "output" ? null! : output,
            parameterName == "walletService" ? null! : service));

        Assert.Equal(parameterName, exception.ParamName);
    }

    private static (string Output, decimal Balance) RunSession(
        string commands, FakeRandomSource random, decimal initialBalance = 0m)
    {
        var wallet = new DomainWallet();
        if (initialBalance > 0m)
        {
            wallet.Deposit(initialBalance);
        }
        var service = new WalletService(wallet, new SlotGame(random));
        using var input = new StringReader(commands);
        using var output = new StringWriter(CultureInfo.CurrentCulture);

        new CommandLoop(input, output, service).Run();

        random.VerifyAllCallsMade();
        return (output.ToString(), service.Balance);
    }
}
