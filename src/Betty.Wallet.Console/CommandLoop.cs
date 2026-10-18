using System.Globalization;
using Betty.Wallet.Core.Application;

namespace Betty.Wallet.Console;

public sealed class CommandLoop
{
    private readonly TextReader _input;
    private readonly TextWriter _output;
    private readonly WalletService _walletService;

    public CommandLoop(TextReader input, TextWriter output, WalletService walletService)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(walletService);
        _input = input;
        _output = output;
        _walletService = walletService;
    }

    public void Run()
    {
        _output.WriteLine("Betty Wallet");
        _output.WriteLine("Commands: deposit <amount>, withdraw <amount>, bet <amount>, exit.");
        _output.WriteLine("Use a decimal point and at most 2 decimal places. Bets: $1.00 to $10.00.");
        _output.WriteLine($"Balance: {FormatMoney(_walletService.Balance)}");

        while (true)
        {
            _output.Write("> ");
            _output.Flush();
            var input = _input.ReadLine();
            if (input is null)
            {
                break;
            }

            if (!CommandParser.TryParse(input, out var command))
            {
                WriteResult("Invalid command or amount. Use the listed commands and at most 2 decimal places.");
                continue;
            }

            if (command.Type == CommandType.Exit)
            {
                break;
            }

            try
            {
                Execute(command);
            }
            catch (ArgumentOutOfRangeException)
            {
                WriteResult(command.Type == CommandType.Bet
                    ? "Bet must be between $1.00 and $10.00."
                    : "Amount must be greater than $0.00.");
            }
            catch (InvalidOperationException)
            {
                WriteResult("Insufficient balance.");
            }
            catch (OverflowException)
            {
                WriteResult("Result is too large or requires unsupported decimal precision.");
            }
        }

        _output.WriteLine("Goodbye.");
    }

    private void Execute(ParsedCommand command)
    {
        switch (command.Type)
        {
            case CommandType.Deposit:
                _walletService.Deposit(command.Amount);
                WriteResult($"Deposited {FormatMoney(command.Amount)}.");
                break;
            case CommandType.Withdraw:
                _walletService.Withdraw(command.Amount);
                WriteResult($"Withdrew {FormatMoney(command.Amount)}.");
                break;
            case CommandType.Bet:
                var result = _walletService.PlaceBet(command.Amount);
                WriteResult($"Bet: {FormatMoney(result.BetAmount)}. Gross payout: {FormatMoney(result.Payout)}.");
                break;
        }
    }

    private void WriteResult(string message)
    {
        _output.WriteLine($"{message} Balance: {FormatMoney(_walletService.Balance)}");
    }

    private static string FormatMoney(decimal amount)
    {
        return "$" + amount.ToString("F2", CultureInfo.InvariantCulture);
    }
}
