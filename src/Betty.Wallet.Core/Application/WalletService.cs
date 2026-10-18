using Betty.Wallet.Core.Domain;
using DomainWallet = Betty.Wallet.Core.Domain.Wallet;

namespace Betty.Wallet.Core.Application;

public sealed class WalletService
{
    private readonly DomainWallet _wallet;
    private readonly SlotGame _slotGame;

    public WalletService(DomainWallet wallet, SlotGame slotGame)
    {
        ArgumentNullException.ThrowIfNull(wallet);
        ArgumentNullException.ThrowIfNull(slotGame);
        _wallet = wallet;
        _slotGame = slotGame;
    }

    public decimal Balance => _wallet.Balance;

    public void Deposit(decimal amount) => _wallet.Deposit(amount);

    public void Withdraw(decimal amount) => _wallet.Withdraw(amount);

    public RoundResult PlaceBet(decimal betAmount)
    {
        if (betAmount < 1m || betAmount > 10m)
        {
            throw new ArgumentOutOfRangeException(nameof(betAmount), betAmount, "Bet amount must be between 1 and 10 inclusive.");
        }

        if (betAmount > _wallet.Balance)
        {
            throw new InvalidOperationException("Insufficient balance.");
        }

        var result = _slotGame.Play(betAmount);
        _wallet.ApplyRound(result.BetAmount, result.Payout);

        return result;
    }
}
