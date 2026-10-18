namespace Betty.Wallet.Core.Domain;

public sealed class Wallet
{
    public decimal Balance { get; private set; }

    public void Deposit(decimal amount)
    {
        if (amount <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Deposit amount must be positive.");
        }

        Balance += amount;
    }

    public void Withdraw(decimal amount)
    {
        if (amount <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Withdrawal amount must be positive.");
        }

        if (amount > Balance)
        {
            throw new InvalidOperationException("Insufficient balance.");
        }

        Balance -= amount;
    }

    public void ApplyRound(decimal betAmount, decimal payout)
    {
        if (betAmount <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(betAmount), betAmount, "Bet amount must be positive.");
        }

        if (payout < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(payout), payout, "Payout must not be negative.");
        }

        if (betAmount > Balance)
        {
            throw new InvalidOperationException("Insufficient balance.");
        }

        var newBalance = Balance - betAmount + payout;
        Balance = newBalance;
    }
}
