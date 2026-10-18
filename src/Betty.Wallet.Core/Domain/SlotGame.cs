using Betty.Wallet.Core.Abstractions;

namespace Betty.Wallet.Core.Domain;

public sealed class SlotGame
{
    private readonly IRandomSource _randomSource;

    public SlotGame(IRandomSource randomSource)
    {
        ArgumentNullException.ThrowIfNull(randomSource);
        _randomSource = randomSource;
    }

    public RoundResult Play(decimal betAmount)
    {
        if (betAmount < 1m || betAmount > 10m)
        {
            throw new ArgumentOutOfRangeException(nameof(betAmount), betAmount, "Bet amount must be between 1 and 10 inclusive.");
        }

        var category = _randomSource.Next(0, 100);
        var multiplier = category switch
        {
            < 50 => 0m,
            < 90 => _randomSource.Next(1, 201) / 100m,
            _ => _randomSource.Next(201, 1001) / 100m
        };

        var payout = decimal.Round(betAmount * multiplier, 2, MidpointRounding.AwayFromZero);

        return new RoundResult(betAmount, multiplier, payout);
    }
}
