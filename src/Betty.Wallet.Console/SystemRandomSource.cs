using Betty.Wallet.Core.Abstractions;

namespace Betty.Wallet.Console;

public sealed class SystemRandomSource : IRandomSource
{
    public int Next(int minInclusive, int maxExclusive) => Random.Shared.Next(minInclusive, maxExclusive);
}
