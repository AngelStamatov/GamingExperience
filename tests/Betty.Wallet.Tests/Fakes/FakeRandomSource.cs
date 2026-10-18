using Betty.Wallet.Core.Abstractions;

namespace Betty.Wallet.Tests.Fakes;

internal sealed class FakeRandomSource : IRandomSource
{
    private readonly Queue<(int MinInclusive, int MaxExclusive, int Result)> _expectedCalls;

    public FakeRandomSource(params (int MinInclusive, int MaxExclusive, int Result)[] expectedCalls)
    {
        _expectedCalls = new(expectedCalls);
    }

    public int CallCount { get; private set; }

    public int Next(int minInclusive, int maxExclusive)
    {
        CallCount++;
        Assert.NotEmpty(_expectedCalls);
        var expected = _expectedCalls.Dequeue();
        Assert.Equal(expected.MinInclusive, minInclusive);
        Assert.Equal(expected.MaxExclusive, maxExclusive);

        return expected.Result;
    }

    public void VerifyAllCallsMade()
    {
        Assert.Empty(_expectedCalls);
    }
}
