using System.Globalization;

namespace Betty.Wallet.Console;

public enum CommandType
{
    Deposit,
    Withdraw,
    Bet,
    Exit
}

public readonly record struct ParsedCommand(CommandType Type, decimal Amount = 0m);

public static class CommandParser
{
    public static bool TryParse(string? input, out ParsedCommand command)
    {
        command = default;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var parts = input.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1 && parts[0].Equals("exit", StringComparison.OrdinalIgnoreCase))
        {
            command = new ParsedCommand(CommandType.Exit);
            return true;
        }

        if (parts.Length != 2)
        {
            return false;
        }

        CommandType? type = parts[0].ToLowerInvariant() switch
        {
            "deposit" => CommandType.Deposit,
            "withdraw" => CommandType.Withdraw,
            "bet" => CommandType.Bet,
            _ => null
        };

        if (type is null || !TryParseAmount(parts[1], out var amount))
        {
            return false;
        }

        command = new ParsedCommand(type.Value, amount);
        return true;
    }

    private static bool TryParseAmount(string text, out decimal amount)
    {
        amount = 0m;
        var decimalPoint = text.IndexOf('.');
        var fractionalDigits = decimalPoint >= 0 ? text[(decimalPoint + 1)..] : string.Empty;
        if (fractionalDigits.Length > 2 || text.Any(c => !char.IsAsciiDigit(c) && c is not '.' and not '+' and not '-'))
        {
            return false;
        }

        const NumberStyles styles = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;
        if (!decimal.TryParse(text, styles, CultureInfo.InvariantCulture, out amount))
        {
            return false;
        }

        // Decimal parsing can round large values even when they have only two fractional digits.
        var parsedCents = (decimal.Abs(amount % 1m) * 100m).ToString("00", CultureInfo.InvariantCulture);
        return parsedCents == fractionalDigits.PadRight(2, '0');
    }
}
