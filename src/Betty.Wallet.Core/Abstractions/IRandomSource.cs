namespace Betty.Wallet.Core.Abstractions;

public interface IRandomSource
{
    int Next(int minInclusive, int maxExclusive);
}
