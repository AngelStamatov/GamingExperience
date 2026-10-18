using System.Globalization;
using Betty.Wallet.Console;

namespace Betty.Wallet.Tests.Console;

public class CommandParserTests
{
    public static TheoryData<string, CommandType, decimal> ValidCommands => new()
    {
        { "deposit 20", CommandType.Deposit, 20m },
        { "DEPOSIT 20.00", CommandType.Deposit, 20m },
        { "  DePoSiT   10.25  ", CommandType.Deposit, 10.25m },
        { "withdraw\t5", CommandType.Withdraw, 5m },
        { "WITHDRAW 0.15", CommandType.Withdraw, 0.15m },
        { "bet 1.50", CommandType.Bet, 1.50m },
        { "bet 10", CommandType.Bet, 10m },
        { "exit", CommandType.Exit, 0m },
        { "  eXiT  ", CommandType.Exit, 0m },
        { "deposit +10.25", CommandType.Deposit, 10.25m },
        { "deposit .50", CommandType.Deposit, 0.50m },
        { "deposit 1.", CommandType.Deposit, 1m },
        { "deposit 00020.50", CommandType.Deposit, 20.50m },
        { "deposit 0", CommandType.Deposit, 0m },
        { "withdraw -1", CommandType.Withdraw, -1m },
        { "deposit 79228162514264337593543950335", CommandType.Deposit, decimal.MaxValue }
    };

    [Theory]
    [MemberData(nameof(ValidCommands))]
    public void TryParse_ValidSyntax_ReturnsCommandAndExactAmount(
        string input, CommandType expectedType, decimal expectedAmount)
    {
        var success = CommandParser.TryParse(input, out var command);

        Assert.True(success);
        Assert.Equal(expectedType, command.Type);
        Assert.Equal(expectedAmount, command.Amount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("hello")]
    [InlineData("deposit")]
    [InlineData("withdraw")]
    [InlineData("bet")]
    [InlineData("exit 1")]
    [InlineData("deposit 20 extra")]
    [InlineData("deposit 1,50")]
    [InlineData("deposit 1,000.00")]
    [InlineData("deposit 1e2")]
    [InlineData("deposit $20")]
    [InlineData("deposit NaN")]
    [InlineData("deposit 1.001")]
    [InlineData("deposit 1.000")]
    [InlineData("withdraw 1.005")]
    [InlineData("bet 1.505")]
    [InlineData("deposit 79228162514264337593543950336")]
    [InlineData("deposit 79228162514264337593543950334.99")]
    [InlineData("deposit 79228162514264337593543950335.01")]
    [InlineData("deposit 1..0")]
    [InlineData("deposit +")]
    [InlineData("deposit .")]
    [InlineData("deposit --1")]
    public void TryParse_InvalidSyntaxOrInexactAmount_ReturnsFalse(string? input)
    {
        Assert.False(CommandParser.TryParse(input, out _));
    }

    [Theory]
    [InlineData("bg-BG")]
    [InlineData("fr-FR")]
    [InlineData("tr-TR")]
    public void TryParse_UsesInvariantCulture(string cultureName)
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);

            Assert.True(CommandParser.TryParse("DEPOSIT 12.50", out var command));
            Assert.Equal(CommandType.Deposit, command.Type);
            Assert.Equal(12.50m, command.Amount);
            Assert.False(CommandParser.TryParse("deposit 12,50", out _));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }
}
